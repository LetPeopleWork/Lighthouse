using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    /// <summary>
    /// What the Features list reads, and nothing it could write with. Everything returned is read without
    /// tracking, so nothing handed out here can reach a save.
    /// </summary>
    public interface IFeatureListReader
    {
        /// <summary>
        /// The Features of one Portfolio in forecast order, each with its Portfolios, its work per Team and those
        /// Teams, what it depends on, and every completion forecast with its histogram - the row's dates fold all of
        /// them. Of the start forecasts only the Feature's own is read, histogram included; the ones per Team are
        /// left in the database because a row never shows them.
        /// </summary>
        IReadOnlyList<Feature> FeaturesOfPortfolio(int portfolioId);

        /// <summary>
        /// The Features one Team has work in, across Portfolios, read exactly as <see cref="FeaturesOfPortfolio"/> reads them.
        /// </summary>
        IReadOnlyList<Feature> FeaturesOfTeam(int teamId);

        /// <summary>
        /// Every Feature held, in forecast order, with its Portfolios, its work per Team and those Teams, what
        /// it depends on, and the header of each completion forecast, but none of the simulation results
        /// behind them and no start forecasts. That is everything the dependency decision reads, and a
        /// small part of what reading a Feature whole costs. Nothing is tracked, yet a Team or Portfolio
        /// shared by several Features is still one object, as in a tracked read. Read only: nothing returned
        /// here may be saved.
        /// </summary>
        IReadOnlyList<Feature> WholeGraphWithoutHistograms();

        /// <summary>
        /// The parents the given reference ids name, as far as this instance holds them: reference, name, link
        /// and the Portfolios each sits in, and nothing else of the Feature. An id nothing holds is not in the
        /// answer; one held twice answers once.
        /// </summary>
        IReadOnlyList<FeatureParent> ParentsOf(IReadOnlyCollection<string> referenceIds);
    }

    /// <summary>A parent as the list names it. <see cref="PortfolioIds"/> is what decides whether a reader may see it.</summary>
    public sealed record FeatureParent(string ReferenceId, string Name, string Url, IReadOnlyCollection<int> PortfolioIds);
}
