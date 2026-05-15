using Lighthouse.Backend.Services.Interfaces.OAuth;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthProviderRegistry : IOAuthProviderRegistry
    {
        private readonly Dictionary<string, IOAuthProvider> providersByKey;

        public OAuthProviderRegistry(IEnumerable<IOAuthProvider> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);

            var providerList = providers.ToList();
            var duplicateKey = providerList
                .GroupBy(p => p.ProviderKey, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateKey is not null)
            {
                throw new InvalidOperationException(
                    $"Duplicate IOAuthProvider registration for ProviderKey '{duplicateKey}'. " +
                    "Each OAuth provider must have a unique ProviderKey.");
            }

            providersByKey = providerList.ToDictionary(p => p.ProviderKey, p => p, StringComparer.Ordinal);
        }

        public IOAuthProvider GetByKey(string authenticationMethodKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationMethodKey);

            if (providersByKey.TryGetValue(authenticationMethodKey, out var provider))
            {
                return provider;
            }

            throw new OAuthProviderNotFoundException(
                $"No IOAuthProvider is registered for authentication method key '{authenticationMethodKey}'.");
        }
    }
}
