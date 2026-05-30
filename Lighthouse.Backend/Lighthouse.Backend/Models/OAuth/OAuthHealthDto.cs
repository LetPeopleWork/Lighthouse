namespace Lighthouse.Backend.Models.OAuth
{
    public sealed record OAuthHealthDto(
        int TotalOAuthConnections,
        int DisconnectedCount,
        int? FirstDisconnectedConnectionId);
}
