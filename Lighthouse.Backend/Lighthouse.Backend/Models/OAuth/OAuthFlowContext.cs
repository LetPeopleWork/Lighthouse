namespace Lighthouse.Backend.Models.OAuth
{
    public record OAuthFlowContext(
        int ConnectionId,
        string ProviderKey,
        string ClientId,
        string ClientSecret,
        Uri RedirectUri,
        string State,
        IReadOnlyList<string> Scopes);
}
