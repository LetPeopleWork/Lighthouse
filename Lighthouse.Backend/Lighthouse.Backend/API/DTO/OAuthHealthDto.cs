namespace Lighthouse.Backend.API.DTO
{
    public sealed record OAuthHealthDto(
        OAuthHealthMetric SetupSuccessRate30d,
        OAuthHealthMetric RefreshSuccessRate7d,
        long StaleRefreshFailedCount24h,
        long StaleRefreshFailedCount7d);
}
