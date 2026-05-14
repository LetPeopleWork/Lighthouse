namespace Lighthouse.Backend.Models.OAuth
{
    public record OAuthCompleteResult(int ConnectionId, OAuthCredentialStatus Status, string? ErrorMessage);
}
