namespace Lighthouse.Backend.API.DTO
{
    public sealed record OAuthHealthMetric(double? Value, string? UnavailableReason);
}
