using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IFeatureRepository : IRepository<Feature>, IFeatureListReader
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
        /// The Features holding these reference ids and no others, in forecast order, each read whole as
        /// <see cref="IRepository{T}.GetAll"/> reads it. One database statement however many ids are asked
        /// for; an id nothing holds simply has no Feature in the answer.
        /// </summary>
        IReadOnlyList<Feature> FeaturesWithReferenceIds(IReadOnlyCollection<string> referenceIds);

        /// <summary>
        /// Marks the forecasts of these Features as confirmed current at the given moment, leaving every
        /// forecast value as it is. One statement, because a large Portfolio holds thousands of forecasts.
        /// A forecast that already reads a later time keeps it, so an overlapping round that stamps last
        /// cannot move it back.
        /// </summary>
        Task ConfirmForecastsAsOf(IReadOnlyCollection<int> featureIds, DateTime confirmedAt, CancellationToken cancellationToken);
    }
}
