using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    public class ApiKeyOwnerReconciliationSeeder(
        IApiKeyRepository apiKeyRepository,
        IRepository<UserProfile> userProfileRepository,
        ILogger<ApiKeyOwnerReconciliationSeeder> logger) : ISeeder
    {
        public async Task Seed()
        {
            var unlinkedKeys = apiKeyRepository.GetAll()
                .Where(k => string.IsNullOrWhiteSpace(k.OwnerSubject) && !k.OwnerUserProfileId.HasValue)
                .ToList();

            if (unlinkedKeys.Count == 0)
            {
                return;
            }

            logger.LogInformation("Reconciling {Count} API key(s) with no owner link", unlinkedKeys.Count);

            var profiles = userProfileRepository.GetAll().ToList();
            var anyReconciled = false;

            foreach (var key in unlinkedKeys)
            {
                var matchedProfile = TryMatchProfile(key, profiles);

                if (matchedProfile is not null)
                {
                    key.OwnerSubject = matchedProfile.Subject;
                    key.OwnerUserProfileId = matchedProfile.Id;
                    anyReconciled = true;
                    logger.LogInformation(
                        "API key {KeyId} linked to owner subject {Subject} (profile {ProfileId}) during reconciliation",
                        key.Id,
                        matchedProfile.Subject,
                        matchedProfile.Id);
                }
                else
                {
                    logger.LogWarning(
                        "API key {KeyId} (CreatedByUser={CreatedByUser}) could not be deterministically linked to a UserProfile and will remain unlinked. It will use the authenticated-read-only baseline until reassigned or revoked",
                        key.Id,
                        key.CreatedByUser);
                }
            }

            if (anyReconciled)
            {
                await apiKeyRepository.Save();
            }
        }

        private static UserProfile? TryMatchProfile(ApiKey apiKey, IReadOnlyList<UserProfile> profiles)
        {
            if (string.IsNullOrWhiteSpace(apiKey.CreatedByUser))
            {
                return null;
            }

            var candidates = profiles
                .Where(p =>
                    string.Equals(p.DisplayName, apiKey.CreatedByUser, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Email, apiKey.CreatedByUser, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return candidates.Count == 1 ? candidates[0] : null;
        }
    }
}
