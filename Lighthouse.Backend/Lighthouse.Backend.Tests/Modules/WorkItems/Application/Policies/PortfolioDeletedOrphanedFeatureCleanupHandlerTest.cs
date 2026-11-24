using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Modules.WorkItems.Application.Policies;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.Services.Implementation;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.WorkItems.Application.Policies
{
    /// <summary>
    /// Deleting a Portfolio cleans up its Features at once, with the same rule the refresh and the start use:
    /// a Feature it leaves in no Portfolio, and a parent item no Feature in a Portfolio names any more, are gone
    /// from the Team's Features tab without waiting for another refresh or a restart.
    /// </summary>
    [TestFixture]
    public class PortfolioDeletedOrphanedFeatureCleanupHandlerTest
    {
        private FeatureStore store = null!;

        [SetUp]
        public void SetUp()
        {
            store = new FeatureStore();
        }

        [TearDown]
        public void TearDown()
        {
            store.Dispose();
        }

        [Test]
        public async Task DeletingAPortfolio_RemovesItsFeatureAndThatFeaturesFormerParentFromTheTeamsFeaturesTabAtOnce()
        {
            var team = await store.ATeam("Team");
            var deleted = await store.APortfolio("Deleted Portfolio");
            var other = await store.APortfolio("Other Portfolio");
            await store.AFormerParentItem(team, "PARENT-1", remainingWork: 4, withAForecast: false);
            await store.AFeature(team, "CHILD-1", remainingWork: 2, inPortfolios: [deleted], parent: "PARENT-1");
            await store.AFeature(team, "OTHER-1", remainingWork: 3, inPortfolios: [other]);

            await ThePortfolioIsDeleted(deleted);

            await TheStoredFeaturesAre("OTHER-1");
            TheTeamsFeaturesTabShows(team, "OTHER-1");
        }

        [Test]
        public async Task DeletingAPortfolio_KeepsAParentItemThatAFeatureInAnotherPortfolioStillNamesAsItsParent()
        {
            var team = await store.ATeam("Team");
            var deleted = await store.APortfolio("Deleted Portfolio");
            var other = await store.APortfolio("Other Portfolio");
            await store.AFormerParentItem(team, "PARENT-1", remainingWork: 0, withAForecast: false, inPortfolios: [deleted]);
            await store.AFeature(team, "CHILD-1", remainingWork: 2, inPortfolios: [other], parent: "PARENT-1");

            await ThePortfolioIsDeleted(deleted);

            await TheStoredFeaturesAre("CHILD-1", "PARENT-1");
        }

        private async Task ThePortfolioIsDeleted(Portfolio portfolio)
        {
            using (var context = store.NewContext())
            {
                var portfolios = new PortfolioRepository(context, Mock.Of<ILogger<PortfolioRepository>>());
                portfolios.Remove(portfolio.Id);
                await portfolios.Save();
            }

            await new PortfolioDeletedOrphanedFeatureCleanupHandler(store.TheCleanup()).HandleAsync(new PortfolioDeleted(portfolio.Id), CancellationToken.None);
        }

        private async Task TheStoredFeaturesAre(params string[] references)
        {
            Assert.That(await store.StoredFeatures(), Is.EquivalentTo(references),
                "The Features stored right after the Portfolio deletion are not the ones expected. " +
                "A Feature is kept only while it is in a Portfolio or while a Feature in a Portfolio names it as its parent, and a deletion must apply that at once, not at the next refresh or start; " +
                "a Feature that still parents a Feature in another Portfolio must survive the deletion. " +
                "Stop the Portfolio removal from deleting Features itself, publish the deletion event after the save, and let the Work Items reaction run the clean-up.");
        }

        private void TheTeamsFeaturesTabShows(Team team, params string[] references)
        {
            Assert.That(store.TheTeamsFeaturesTab(team), Is.EquivalentTo(references),
                "The Team's Features tab right after the Portfolio deletion does not show exactly the Features expected. " +
                "A Feature the deletion leaves in no Portfolio must leave the tab at once. " +
                "Let the Work Items reaction to the deletion run the clean-up.");
        }
    }
}
