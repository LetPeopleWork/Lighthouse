using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.Forecasting.Contracts
{
    public sealed record ForecastRoundFinished(
        int PortfolioId, string PortfolioName, int ItemCount, long DurationMs, DateTime FinishedAt, ForecastRoundFinished.RoundOutcome Outcome) : IDomainEvent
    {
        public enum RoundOutcome
        {
            Succeeded,
            Failed,
            Cancelled,
            Skipped,
        }

        /// <summary>
        /// A stop asked for by the operator wins over a failure, and only a round that succeeded can have been
        /// skipped, so a round always ends in exactly one of the four outcomes.
        /// </summary>
        public static ForecastRoundFinished For(
            Portfolio portfolio, int itemCount, long durationMs, DateTime finishedAt, bool success, bool cancelled, bool skipped)
        {
            ArgumentNullException.ThrowIfNull(portfolio);

            return new ForecastRoundFinished(portfolio.Id, portfolio.Name, itemCount, durationMs, finishedAt, OutcomeOf(success, cancelled, skipped));
        }

        private static RoundOutcome OutcomeOf(bool success, bool cancelled, bool skipped)
        {
            if (cancelled)
            {
                return RoundOutcome.Cancelled;
            }

            if (!success)
            {
                return RoundOutcome.Failed;
            }

            return skipped ? RoundOutcome.Skipped : RoundOutcome.Succeeded;
        }
    }
}
