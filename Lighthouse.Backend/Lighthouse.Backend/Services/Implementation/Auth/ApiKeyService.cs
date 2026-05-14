using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class ApiKeyService(
        IApiKeyRepository repository,
        IRepository<UserProfile> userProfileRepository,
        IRepository<ApiKeyPermission> apiKeyPermissionRepository,
        ILogger<ApiKeyService> logger) : IApiKeyService
    {
        private const int KeyByteLength = 32;
        private const int SaltByteLength = 16;
        private const int Pbkdf2Iterations = 100_000;

        public Task<ApiKeyCreationResult> CreateApiKeyAsync(
            string name,
            string description,
            string createdByUser,
            string ownerSubject)
        {
            return CreateApiKeyAsync(name, description, createdByUser, ownerSubject, scope: null);
        }

        public async Task<ApiKeyCreationResult> CreateApiKeyAsync(
            string name,
            string description,
            string createdByUser,
            string ownerSubject,
            IReadOnlyList<ApiKeyScopeDto>? scope)
        {
            var plainTextKey = GenerateSecureKey();
            var salt = GenerateSalt();
            var hash = HashKey(plainTextKey, salt);
            var ownerProfile = ResolveOwnerProfileBySubject(ownerSubject, userProfileRepository.GetAll().ToList());

            var apiKey = new ApiKey
            {
                Name = name,
                Description = description,
                CreatedByUser = createdByUser,
                OwnerUserProfileId = ownerProfile?.Id,
                OwnerSubject = ownerSubject,
                CreatedAt = DateTime.UtcNow,
                KeyHash = hash,
                Salt = salt,
            };

            repository.Add(apiKey);
            await repository.Save();

            if (scope is not null && scope.Count > 0)
            {
                foreach (var entry in scope)
                {
                    apiKeyPermissionRepository.Add(new ApiKeyPermission
                    {
                        ApiKeyId = apiKey.Id,
                        Role = entry.Role,
                        ScopeType = entry.ScopeType,
                        ScopeId = entry.ScopeId,
                        GrantedAt = DateTime.UtcNow,
                    });
                }

                await apiKeyPermissionRepository.Save();
            }

            return new ApiKeyCreationResult
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Description = apiKey.Description,
                CreatedAt = apiKey.CreatedAt,
                PlainTextKey = plainTextKey,
            };
        }

        public IEnumerable<ApiKeyInfo> GetApiKeysByOwnerSubject(string ownerSubject)
        {
            var profiles = userProfileRepository.GetAll().ToList();
            var scopesByKeyId = BuildScopeIndex(apiKeyPermissionRepository.GetAll().ToList());

            return repository.GetAll()
                .Where(k => string.Equals(TryResolveOwnerSubject(k, profiles), ownerSubject, StringComparison.Ordinal))
                .Select(k => new ApiKeyInfo
                {
                    Id = k.Id,
                    Name = k.Name,
                    Description = k.Description,
                    CreatedAt = k.CreatedAt,
                    LastUsedAt = k.LastUsedAt,
                    Scopes = scopesByKeyId.TryGetValue(k.Id, out var scopes)
                        ? scopes
                        : Array.Empty<ApiKeyScopeDto>(),
                });
        }

        private static Dictionary<int, IReadOnlyList<ApiKeyScopeDto>> BuildScopeIndex(IReadOnlyList<ApiKeyPermission> permissions)
        {
            return permissions
                .GroupBy(permission => permission.ApiKeyId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ApiKeyScopeDto>)group
                        .Select(permission => new ApiKeyScopeDto
                        {
                            Role = permission.Role,
                            ScopeType = permission.ScopeType,
                            ScopeId = permission.ScopeId,
                        })
                        .ToList());
        }

        public async Task<bool> DeleteApiKey(int id, string ownerSubject)
        {
            if (!repository.Exists(id))
            {
                return false;
            }

            var key = repository.GetById(id);
            if (key is null)
            {
                return false;
            }

            var profiles = userProfileRepository.GetAll().ToList();
            var resolvedOwnerSubject = TryResolveOwnerSubject(key, profiles);

            if (!string.Equals(resolvedOwnerSubject, ownerSubject, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "API key {KeyId} deletion denied: requesting subject does not match the key owner",
                    id);
                return false;
            }

            repository.Remove(id);
            await repository.Save();
            return true;
        }

        public async Task<ApiKeyValidationResult> ValidateApiKeyWithOwnerAsync(string plainTextKey)
        {
            if (string.IsNullOrEmpty(plainTextKey))
            {
                return new ApiKeyValidationResult { IsValid = false };
            }

            var key = FindMatchingKey(plainTextKey);
            if (key is null)
            {
                return new ApiKeyValidationResult { IsValid = false };
            }

            var profiles = userProfileRepository.GetAll().ToList();
            var ownerProfile = TryResolveOwnerProfile(key, profiles);

            key.LastUsedAt = DateTime.UtcNow;
            if (ownerProfile is not null)
            {
                key.OwnerUserProfileId = ownerProfile.Id;
                key.OwnerSubject = ownerProfile.Subject;
            }

            await repository.Save();

            if (ownerProfile is null)
            {
                logger.LogWarning(
                    "API key {KeyId} authenticated but owner is unlinked. Requests will use authenticated-read-only baseline until the key is reassigned or revoked",
                    key.Id);

                return new ApiKeyValidationResult
                {
                    IsValid = true,
                    ApiKeyId = key.Id,
                    OwnerResolutionState = ApiKeyOwnerResolutionState.Unlinked,
                };
            }

            logger.LogDebug(
                "API key {KeyId} authenticated with resolved owner subject {OwnerSubject}",
                key.Id,
                ownerProfile.Subject);

            return new ApiKeyValidationResult
            {
                IsValid = true,
                ApiKeyId = key.Id,
                OwnerResolutionState = ApiKeyOwnerResolutionState.Resolved,
                OwnerSubject = ownerProfile.Subject,
                OwnerDisplayName = ownerProfile.DisplayName,
                OwnerEmail = ownerProfile.Email,
            };
        }

        public async Task<bool> ValidateApiKeyAsync(string plainTextKey)
        {
            var result = await ValidateApiKeyWithOwnerAsync(plainTextKey);
            return result.IsValid;
        }

        private ApiKey? FindMatchingKey(string plainTextKey)
        {
            foreach (var key in repository.GetAll())
            {
                var expectedHash = HashKey(plainTextKey, key.Salt);
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedHash),
                    Encoding.UTF8.GetBytes(key.KeyHash)))
                {
                    return key;
                }
            }

            return null;
        }

        private static string? TryResolveOwnerSubject(ApiKey apiKey, IReadOnlyList<UserProfile> profiles)
        {
            if (!string.IsNullOrWhiteSpace(apiKey.OwnerSubject))
            {
                return apiKey.OwnerSubject;
            }

            return TryResolveOwnerProfile(apiKey, profiles)?.Subject;
        }

        private static UserProfile? ResolveOwnerProfileBySubject(string ownerSubject, IReadOnlyList<UserProfile> profiles)
        {
            return profiles.SingleOrDefault(profile => string.Equals(profile.Subject, ownerSubject, StringComparison.Ordinal));
        }

        private static UserProfile? TryResolveOwnerProfile(ApiKey apiKey, IReadOnlyList<UserProfile> profiles)
        {
            if (apiKey.OwnerUserProfileId.HasValue)
            {
                var byId = profiles.SingleOrDefault(profile => profile.Id == apiKey.OwnerUserProfileId.Value);
                if (byId is not null)
                {
                    return byId;
                }
            }

            if (!string.IsNullOrWhiteSpace(apiKey.OwnerSubject))
            {
                var bySubject = profiles.SingleOrDefault(profile => string.Equals(profile.Subject, apiKey.OwnerSubject, StringComparison.Ordinal));
                if (bySubject is not null)
                {
                    return bySubject;
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey.CreatedByUser))
            {
                return null;
            }

            var candidates = profiles
                .Where(profile =>
                    string.Equals(profile.DisplayName, apiKey.CreatedByUser, StringComparison.Ordinal)
                    || string.Equals(profile.Email, apiKey.CreatedByUser, StringComparison.OrdinalIgnoreCase))
                .DistinctBy(profile => profile.Id)
                .ToList();

            return candidates.Count == 1 ? candidates[0] : null;
        }

        private static string GenerateSecureKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(KeyByteLength);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static string GenerateSalt()
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltByteLength);
            return Convert.ToBase64String(saltBytes);
        }

        private static string HashKey(string plainTextKey, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var keyBytes = Encoding.UTF8.GetBytes(plainTextKey);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                keyBytes,
                saltBytes,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                32);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
