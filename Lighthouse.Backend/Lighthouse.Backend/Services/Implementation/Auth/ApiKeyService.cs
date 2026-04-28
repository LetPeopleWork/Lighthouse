using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class ApiKeyService(IApiKeyRepository repository) : IApiKeyService
    {
        private const int KeyByteLength = 32;
        private const int SaltByteLength = 16;
        private const int Pbkdf2Iterations = 100_000;

        public async Task<ApiKeyCreationResult> CreateApiKeyAsync(string name, string description, string createdByUser)
        {
            var plainTextKey = GenerateSecureKey();
            var salt = GenerateSalt();
            var hash = HashKey(plainTextKey, salt);

            var apiKey = new ApiKey
            {
                Name = name,
                Description = description,
                CreatedByUser = createdByUser,
                CreatedAt = DateTime.UtcNow,
                KeyHash = hash,
                Salt = salt,
            };

            repository.Add(apiKey);
            await repository.Save();

            return new ApiKeyCreationResult
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Description = apiKey.Description,
                CreatedByUser = apiKey.CreatedByUser,
                CreatedAt = apiKey.CreatedAt,
                PlainTextKey = plainTextKey,
            };
        }

        public IEnumerable<ApiKeyInfo> GetAllApiKeys()
        {
            return repository.GetAll().Select(k => new ApiKeyInfo
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                CreatedByUser = k.CreatedByUser,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt,
            });
        }

        public bool DeleteApiKey(int id)
        {
            if (!repository.Exists(id))
            {
                return false;
            }

            repository.Remove(id);
            repository.Save();
            return true;
        }

        public async Task<bool> ValidateApiKeyAsync(string plainTextKey)
        {
            if (string.IsNullOrEmpty(plainTextKey))
            {
                return false;
            }

            var keys = repository.GetAll();
            foreach (var key in keys)
            {
                var expectedHash = HashKey(plainTextKey, key.Salt);
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedHash),
                    Encoding.UTF8.GetBytes(key.KeyHash)))
                {
                    key.LastUsedAt = DateTime.UtcNow;
                    await repository.Save();
                    return true;
                }
            }

            return false;
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
