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
        /// The Features any of these Teams has work in, in forecast order, tracked so a forecast round can
        /// replace their forecasts and save. Read with their Portfolios, their work per Team and those Teams,
        /// what they depend on, and the header of every completion and start forecast, but none of the
        /// simulation results behind them: a round that replaces a forecast has no use for the histogram it
        /// replaces, and one that finds nothing changed never needs it.
        /// </summary>
        IReadOnlyList<Feature> FeaturesWorkedOnBy(IReadOnlyCollection<int> teamIds);

        /// <summary>
        /// The Features in which this Team has at least one work item, leaving out those still on the
        /// default size, in forecast order, each read whole as <see cref="IRepository{T}.GetAll"/> reads it
        /// and tracked. With <paramref name="onlyIn"/> set, only the Features in that state category.
        /// </summary>
        IReadOnlyList<Feature> FeaturesWithRealWorkForTeam(int teamId, StateCategories? onlyIn);

        /// <summary>
        /// The Done Features of this Portfolio, in forecast order, each read whole as
        /// <see cref="IRepository{T}.GetAll"/> reads it and tracked.
        /// </summary>
        IReadOnlyList<Feature> DoneFeaturesOfPortfolio(int portfolioId);

        /// <summary>
        /// The Doing or Done Features of this Portfolio, in forecast order, each read whole as
        /// <see cref="IRepository{T}.GetAll"/> reads it and tracked.
        /// </summary>
        IReadOnlyList<Feature> DoingOrDoneFeaturesOfPortfolio(int portfolioId);

        /// <summary>
        /// The To Do or Doing Features of this Portfolio, in forecast order, each read whole as
        /// <see cref="IRepository{T}.GetAll"/> reads it and tracked.
        /// </summary>
        IReadOnlyList<Feature> ToDoOrDoingFeaturesOfPortfolio(int portfolioId);

        /// <summary>
        /// The Features of this Portfolio in any of the To Do, Doing or Done categories, in forecast order,
        /// each read whole as <see cref="IRepository{T}.GetAll"/> reads it and tracked.
        /// </summary>
        IReadOnlyList<Feature> ToDoDoingOrDoneFeaturesOfPortfolio(int portfolioId);

        /// <summary>
        /// Every Feature of this Portfolio, in forecast order, each read whole as
        /// <see cref="IRepository{T}.GetAll"/> reads it and tracked. Unlike the list read of the same
        /// Portfolio, it carries every start forecast and the change tracker sees it.
        /// </summary>
        IReadOnlyList<Feature> EveryFeatureOfPortfolio(int portfolioId);

        /// <summary>
        /// Marks the forecasts of these Features as confirmed current at the given moment, leaving every
        /// forecast value as it is. One statement, because a large Portfolio holds thousands of forecasts.
        /// A forecast that already reads a later time keeps it, so an overlapping round that stamps last
        /// cannot move it back.
        /// </summary>
        Task ConfirmForecastsAsOf(IReadOnlyCollection<int> featureIds, DateTime confirmedAt, CancellationToken cancellationToken);
    }
}
