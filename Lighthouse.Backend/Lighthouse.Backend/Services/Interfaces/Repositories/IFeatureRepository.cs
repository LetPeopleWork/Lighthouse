using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IFeatureRepository : IRepository<Feature>
    {
        /// <summary>
        /// The reference id of every Feature held, and nothing else about them. Reading a Feature brings
        /// its Portfolios, its work per team and every simulation result of every forecast with it, which
        /// is a great deal of reading to answer whether an id names something this instance holds.
        /// </summary>
        IReadOnlyCollection<string> GetAllReferenceIds();

        /// <summary>
        /// The reference id and created date of every Feature held, including the ones stored only as the
        /// level above a Portfolio's own. Read once per fetch, for the same reason as the member above. A
        /// reference id stored more than once answers with its earliest known date; the empty date Jira sends
        /// for some migrated issues is not a known one.
        /// </summary>
        StoredFeatures GetStoredFeatureCreatedDates();

        /// <summary>
        /// Marks the forecasts of these Features as confirmed current at the given moment, leaving every
        /// forecast value as it is. One statement, because a large Portfolio holds thousands of forecasts.
        /// </summary>
        Task ConfirmForecastsAsOf(IReadOnlyCollection<int> featureIds, DateTime confirmedAt, CancellationToken cancellationToken);
    }
}
