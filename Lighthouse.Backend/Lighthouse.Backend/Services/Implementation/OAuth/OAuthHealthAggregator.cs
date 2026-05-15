using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public sealed class OAuthHealthAggregator : IOAuthHealthAggregator
    {
        // The OAuth event store (per-event history of initiated/completed/failed/refreshed flows) is not
        // yet implemented; see feature-delta.md "Wave: DELIVER / [WHY] Upstream issues — OAuth event store deferred".
        private const string EventStorePendingReason = "event_store_pending";

        private static readonly OAuthHealthMetric PendingMetric = new(Value: null, UnavailableReason: EventStorePendingReason);

        private readonly IRepository<OAuthCredential> credentialRepository;
        private readonly TimeProvider timeProvider;

        public OAuthHealthAggregator(IRepository<OAuthCredential> credentialRepository, TimeProvider timeProvider)
        {
            this.credentialRepository = credentialRepository ?? throw new ArgumentNullException(nameof(credentialRepository));
            this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public Task<OAuthHealthDto> AggregateAsync(CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow();
            var twentyFourHoursAgo = now - TimeSpan.FromHours(24);
            var sevenDaysAgo = now - TimeSpan.FromDays(7);

            var refreshFailed = credentialRepository
                .GetAll()
                .Where(credential => credential.Status == OAuthCredentialStatus.RefreshFailed)
                .ToList();

            var stale24h = refreshFailed.LongCount(credential => credential.UpdatedAt < twentyFourHoursAgo);
            var stale7d = refreshFailed.LongCount(credential => credential.UpdatedAt < sevenDaysAgo);

            var dto = new OAuthHealthDto(
                SetupSuccessRate30d: PendingMetric,
                RefreshSuccessRate7d: PendingMetric,
                StaleRefreshFailedCount24h: stale24h,
                StaleRefreshFailedCount7d: stale7d);

            return Task.FromResult(dto);
        }
    }
}
