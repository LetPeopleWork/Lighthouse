using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Notices which days of a requested window carry no reading yet and asks for them. It runs on the
    /// thread a person is waiting on, so it may only look at rows that read has already materialised -
    /// no further query, and nothing that writes.
    ///
    /// Returns nothing on purpose: a reader gets the series that exists at the moment they asked, and
    /// what is missing arrives on a later visit. A return value here would be something a caller could
    /// be tempted to wait for.
    /// </summary>
    public interface IOverTimeGapReconciler
    {
        void AskForTheDaysThatAreMissing(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            DateOnly? from,
            DateOnly? to,
            IReadOnlyList<PercentilesOverTimeSnapshot> daysAlreadyHeld);
    }
}
