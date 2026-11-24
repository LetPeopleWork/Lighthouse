using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
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
        private const string TheDeletionEvent = "Lighthouse.Backend.Models.Events.PortfolioDeleted";

        private const string TheWorkItemsReaction = "Lighthouse.Backend.Modules.WorkItems.Application.Policies.PortfolioDeletedOrphanedFeatureCleanupHandler";

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

            await TheWorkItemsReactionHandles(TheEventOfDeleting(portfolio));
        }

        private async Task TheWorkItemsReactionHandles(object portfolioDeleted)
        {
            var reaction = TheProductionType(TheWorkItemsReaction,
                "Without a Work Items reaction to a Portfolio deletion, the Features the deletion leaves in no Portfolio stay on the Team's Features tab until the next refresh or start.",
                "Add the reaction under Modules/WorkItems/Application/Policies; it runs the orphaned-Feature clean-up.");
            var handlerPort = typeof(IDomainEventHandler<>).MakeGenericType(portfolioDeleted.GetType());
            Assert.That(handlerPort.IsAssignableFrom(reaction), Is.True,
                $"{reaction.Name} does not handle {portfolioDeleted.GetType().Name}. " +
                "The dispatcher only hands an event to the handlers of that event, so the clean-up would never run on a Portfolio deletion. " +
                $"Make it implement IDomainEventHandler<{portfolioDeleted.GetType().Name}>.");

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(store.TheCleanup());
            using var provider = services.BuildServiceProvider();
            var handler = ActivatorUtilities.CreateInstance(provider, reaction);

            var handling = (Task)handlerPort.GetMethod("HandleAsync")!.Invoke(handler, [portfolioDeleted, CancellationToken.None])!;
            await handling;
        }

        private static object TheEventOfDeleting(Portfolio portfolio)
        {
            var portfolioDeleted = TheProductionType(TheDeletionEvent,
                "Without an event that says a Portfolio was deleted, Work Items cannot clean up the Features the deletion leaves in no Portfolio until the next refresh or start.",
                "Add the domain event record PortfolioDeleted(int PortfolioId) beside TeamDeleted.");
            return Activator.CreateInstance(portfolioDeleted, portfolio.Id)!;
        }

        // Found by name so that, until they exist, this class still builds and fails here with a reason instead of
        // stopping the whole test project from compiling.
        private static Type TheProductionType(string fullName, string why, string how)
        {
            var type = typeof(Portfolio).Assembly.GetType(fullName);
            Assert.That(type, Is.Not.Null, $"The production code holds no {fullName}. {why} {how}");
            return type!;
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
