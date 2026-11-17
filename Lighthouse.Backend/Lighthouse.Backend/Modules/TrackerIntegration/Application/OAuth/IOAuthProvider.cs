
namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public interface IOAuthProvider
    {
        string ProviderKey { get; }

        IReadOnlyList<string> DefaultScopes { get; }

        Uri BuildAuthorizationUrl(OAuthFlowContext context);

        Task<OAuthTokens> ExchangeCodeAsync(string code, OAuthFlowContext context, CancellationToken cancellationToken);

        Task<OAuthTokens> RefreshTokenAsync(OAuthRefreshContext context, CancellationToken cancellationToken);
    }
}
