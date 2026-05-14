using Lighthouse.Backend.Services.Interfaces.OAuth;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthProviderRegistry : IOAuthProviderRegistry
    {
        private readonly Dictionary<string, IOAuthProvider> providersByKey;

        public OAuthProviderRegistry(IEnumerable<IOAuthProvider> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);

            providersByKey = new Dictionary<string, IOAuthProvider>(StringComparer.Ordinal);

            foreach (var provider in providers)
            {
                if (!providersByKey.TryAdd(provider.ProviderKey, provider))
                {
                    throw new InvalidOperationException(
                        $"Duplicate IOAuthProvider registration for ProviderKey '{provider.ProviderKey}'. " +
                        "Each OAuth provider must have a unique ProviderKey.");
                }
            }
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
