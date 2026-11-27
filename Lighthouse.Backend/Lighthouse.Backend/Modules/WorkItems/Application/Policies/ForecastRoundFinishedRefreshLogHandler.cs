using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;

namespace Lighthouse.Backend.Modules.WorkItems.Application.Policies
{
    public class ForecastRoundFinishedRefreshLogHandler(IRefreshLogService refreshLogService) : IDomainEventHandler<ForecastRoundFinished>
    {
        /// <summary>
        /// A forecast reads what an earlier refresh already fetched, so it never contacts the work tracking
        /// system and its entry records nothing scanned or downloaded.
        /// </summary>
        public Task HandleAsync(ForecastRoundFinished domainEvent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var nothingFetched = SyncOutcome.None;
            var (success, cancelled, skipped) = FlagsOf(domainEvent.Outcome);

            return refreshLogService.LogRefreshAsync(new RefreshLog
            {
                Type = RefreshType.Forecast,
                EntityId = domainEvent.PortfolioId,
                EntityName = domainEvent.PortfolioName,
                ItemCount = domainEvent.ItemCount,
                Mode = nothingFetched.Mode,
                RecordsScanned = nothingFetched.RecordsScanned,
                RecordsFetched = nothingFetched.RecordsFetched,
                DurationMs = domainEvent.DurationMs,
                ExecutedAt = domainEvent.FinishedAt,
                Success = success,
                Cancelled = cancelled,
                Skipped = skipped,
            });
        }

        // Every outcome has an arm. The compiler also wants one for an int cast to the round outcome outside the
        // enum; writing it would be a catch-all that hides a new outcome, so only that is silenced.
#pragma warning disable CS8524
        private static (bool Success, bool Cancelled, bool Skipped) FlagsOf(ForecastRoundFinished.RoundOutcome outcome) => outcome switch
        {
            ForecastRoundFinished.RoundOutcome.Succeeded => (true, false, false),
            ForecastRoundFinished.RoundOutcome.Failed => (false, false, false),
            ForecastRoundFinished.RoundOutcome.Cancelled => (false, true, false),
            ForecastRoundFinished.RoundOutcome.Skipped => (true, false, true),
        };
#pragma warning restore CS8524
    }
}
