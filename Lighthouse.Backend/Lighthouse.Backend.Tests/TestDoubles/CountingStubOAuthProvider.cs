using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth.Providers;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    internal sealed class CountingStubOAuthProvider : IOAuthProvider
    {
        private readonly StubOAuthProvider inner;
        private readonly TimeSpan refreshDelay;
        private readonly OAuthTokens refreshedTokens;
        private int refreshInvocationCount;

        public CountingStubOAuthProvider(
            IServiceConfig serviceConfig,
            TimeProvider timeProvider,
            TimeSpan refreshDelay,
            OAuthTokens refreshedTokens)
        {
            ArgumentNullException.ThrowIfNull(serviceConfig);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(refreshedTokens);

            inner = new StubOAuthProvider(serviceConfig, timeProvider);
            this.refreshDelay = refreshDelay;
            this.refreshedTokens = refreshedTokens;
        }

        public int RefreshInvocationCount => Volatile.Read(ref refreshInvocationCount);

        public string ProviderKey => inner.ProviderKey;

        public IReadOnlyList<string> DefaultScopes => inner.DefaultScopes;

        public Uri BuildAuthorizationUrl(OAuthFlowContext context) => inner.BuildAuthorizationUrl(context);

        public Task<OAuthTokens> ExchangeCodeAsync(string code, OAuthFlowContext context, CancellationToken cancellationToken)
            => inner.ExchangeCodeAsync(code, context, cancellationToken);

        public async Task<OAuthTokens> RefreshTokenAsync(OAuthRefreshContext context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);

            Interlocked.Increment(ref refreshInvocationCount);
            // Artificial delay widens the contention window so a missing single-flight guard is observable.
            await Task.Delay(refreshDelay, cancellationToken);
            return refreshedTokens;
        }
    }
}
