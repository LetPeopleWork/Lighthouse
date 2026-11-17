namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public sealed record OAuthRefreshOptions(TimeSpan SemaphoreTimeout)
    {
        public static OAuthRefreshOptions Default { get; } = new(TimeSpan.FromSeconds(30));
    }
}
