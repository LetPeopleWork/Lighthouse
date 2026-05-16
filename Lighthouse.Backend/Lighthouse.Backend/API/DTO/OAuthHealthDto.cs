namespace Lighthouse.Backend.API.DTO
{
    public sealed record OAuthHealthDto(int TotalOAuthConnections, int DisconnectedCount);
}
