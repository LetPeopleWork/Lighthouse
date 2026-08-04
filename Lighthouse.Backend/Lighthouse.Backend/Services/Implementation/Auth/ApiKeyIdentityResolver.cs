using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class ApiKeyIdentityResolver(
        IApiKeyRepository apiKeyRepository,
        IRepository<UserProfile> userProfileRepository) : IApiKeyIdentityResolver
    {
        public ApiKeyValidationResult? ResolveByApiKeyId(int apiKeyId)
        {
            var apiKey = apiKeyRepository.GetById(apiKeyId);
            if (apiKey is null)
            {
                return null;
            }

            var ownerProfile = ResolveOwnerProfile(apiKey);
            if (ownerProfile is null)
            {
                return new ApiKeyValidationResult
                {
                    IsValid = true,
                    ApiKeyId = apiKey.Id,
                    OwnerResolutionState = ApiKeyOwnerResolutionState.Unlinked,
                };
            }

            return new ApiKeyValidationResult
            {
                IsValid = true,
                ApiKeyId = apiKey.Id,
                OwnerResolutionState = ApiKeyOwnerResolutionState.Resolved,
                OwnerSubject = ownerProfile.Subject,
                OwnerDisplayName = ownerProfile.DisplayName,
                OwnerEmail = ownerProfile.Email,
            };
        }

        private UserProfile? ResolveOwnerProfile(ApiKey apiKey)
        {
            if (apiKey.OwnerUserProfileId.HasValue)
            {
                var byId = userProfileRepository.GetById(apiKey.OwnerUserProfileId.Value);
                if (byId is not null)
                {
                    return byId;
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey.OwnerSubject))
            {
                return null;
            }

            return userProfileRepository
                .GetAll()
                .SingleOrDefault(profile => string.Equals(profile.Subject, apiKey.OwnerSubject, StringComparison.Ordinal));
        }
    }
}
