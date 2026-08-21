using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Slice 04 of the dependencies epic, over the real route the Features screen calls.
    ///
    /// Setting a Portfolio's dependencies aside is not hiding them. The whole reason for the switch, over
    /// the alternative of editing links in the work tracking system, is that the lead can still see what
    /// they set aside - so every scenario here checks that the list is unchanged as carefully as it checks
    /// that nothing is acted on.
    /// </summary>
    [TestFixture]
    [Category("epic-4365-dependencies")]
    [Category("slice-04")]
    public partial class Slice04DependencySettingsTest
    {
        private static readonly string[] TheOneItWaitsOn = ["Retire the legacy importer"];

        // @rbac @us-10 - "the switch starts off everywhere". A Portfolio that existed before this feature
        // was built acts on its dependencies exactly as it did, without anybody going to look at it.
        [Test]
        public void A_Portfolio_acts_on_its_dependencies_until_somebody_says_otherwise()
        {
            var portfolioId = GivenAPortfolio("Platform");

            Assert.That(ThePortfolioActsOnItsDependencies(portfolioId), Is.True);
        }

        // @driving_adapter @us-10 - "Setting the dependencies aside leaves every one of them in plain
        // sight". A shorter list, or one that named nothing, would read the same as an instance that has
        // no dependencies at all - which is the confusion this whole feature refuses.
        [Test]
        public async Task Setting_them_aside_leaves_every_dependency_named_and_says_so_on_each_one()
        {
            var portfolioId = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                portfolioId,
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2"]),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"));

            WhenThePortfolioSetsItsDependenciesAside(portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheNamesItWaitsOn("F-1"), Is.EqualTo(TheOneItWaitsOn));
                Assert.That(await TheReasonsAgainst("F-1"), Has.All.EqualTo("IgnoredByPortfolio"));
            }
        }

        // @us-04 - a reference somebody typed into a field of their own travels the whole way unchanged,
        // so a reader can tell it from a link drawn in the work tracking system.
        [Test]
        public async Task A_dependency_read_from_a_named_field_says_so_all_the_way_to_the_reader()
        {
            var portfolioId = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                portfolioId,
                AFeatureWhoseWaitWasTypedIntoAField("F-1", "Rebuild the search index", ["F-2"]),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"));

            var entries = await TheEntriesFor("F-1");

            Assert.That(entries.Single().GetProperty("source").GetString(), Is.EqualTo("PortfolioField"));
        }

        // @regression @us-10 - "no refresh asked for and nothing re-downloaded". A setting that needed a
        // full re-download to take effect is a setting nobody experiments with, and experimenting is the
        // entire point of this one.
        [Test]
        public async Task Setting_them_aside_deletes_nothing_and_changes_nothing_that_was_stored()
        {
            var portfolioId = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                portfolioId,
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2", "F-9"]),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"));

            var storedBefore = EveryStoredDependency();
            WhenThePortfolioSetsItsDependenciesAside(portfolioId);

            Assert.That(EveryStoredDependency(), Is.EqualTo(storedBefore));
        }

        // @regression @us-10 - "putting it back changes nothing". Every verdict has to be the one it would
        // have had all along, not one computed for the first time on a plan already being read.
        [Test]
        public async Task Acting_on_them_again_restores_every_verdict_exactly()
        {
            var portfolioId = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                portfolioId,
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2", "F-3", "F-9"]),
                AFeatureWaitingOn("F-2", "Retire the legacy importer", ["F-1"]),
                AFeatureTheTrackerHolds("F-3", "Move the catalogue"));
            GivenTheTeamBehindItHasNoMeasuredDelivery("F-3");

            var verdictsBefore = await EveryVerdictInThePayload();
            WhenThePortfolioSetsItsDependenciesAside(portfolioId);
            var verdictsWhileAside = await EveryVerdictInThePayload();
            WhenThePortfolioActsOnThemAgain(portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await EveryVerdictInThePayload(), Is.EqualTo(verdictsBefore));
                Assert.That(verdictsWhileAside, Is.Not.EqualTo(verdictsBefore),
                    "A switch that changed nothing at all would satisfy the assertion above for free.");
            }
        }

        // @error @us-10 - "A loop is still found while the dependencies are set aside". The loop check is
        // what stops a forecast running forever, so the switch turns off what is acted on and never what is
        // looked for.
        [Test]
        public async Task A_loop_is_still_found_while_the_dependencies_are_set_aside()
        {
            var portfolioId = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                portfolioId,
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2"]),
                AFeatureWaitingOn("F-2", "Retire the legacy importer", ["F-1"]));

            WhenThePortfolioSetsItsDependenciesAside(portfolioId);
            var whileAside = await TheReasonsAgainst("F-1");
            WhenThePortfolioActsOnThemAgain(portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileAside, Has.All.EqualTo("IgnoredByPortfolio"),
                    "Being set aside is the more actionable thing to be told, so it is the reason that wins.");
                Assert.That(await TheReasonsAgainst("F-1"), Has.All.EqualTo("InALoop"),
                    "And the loop was there all along rather than found on the way back.");
            }
        }

        // @edge @us-10 - "Nothing is warned about while the dependencies are set aside". A refresh that
        // still complained about a circle would be telling the operator off for a plan they asked for.
        [Test]
        public async Task A_refresh_says_nothing_about_dependencies_a_Portfolio_has_set_aside()
        {
            var thePortfolioThatActsOnThem = GivenAPortfolio("Platform");
            var thePortfolioThatDoesNot = GivenAPortfolio("Payments");
            SetWhetherItActsOnItsDependencies(thePortfolioThatDoesNot, setThemAside: true);

            await GivenARefreshedPortfolio(
                thePortfolioThatActsOnThem,
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2"]),
                AFeatureWaitingOn("F-2", "Retire the legacy importer", ["F-1"]));
            var whatItSaidAboutTheCircle = TheWarningsTheRefreshRaised();

            await GivenARefreshedPortfolio(
                thePortfolioThatDoesNot,
                AFeatureWaitingOn("F-3", "Move the catalogue", ["F-4"]),
                AFeatureWaitingOn("F-4", "Retire the old catalogue", ["F-3"]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whatItSaidAboutTheCircle, Has.Some.Contains("circle"),
                    "A refresh that never mentioned a circle would satisfy the assertion below for free.");
                Assert.That(TheWarningsTheRefreshRaised(), Has.None.Contains("circle"));
            }
        }

        // @edge @us-10 - a Feature waited on that this Portfolio cannot see is a broken link when the
        // dependencies are being acted on, and nothing worth saying when they are not.
        [Test]
        public async Task A_wait_on_something_outside_the_Portfolio_goes_quiet_too()
        {
            var portfolioId = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                portfolioId,
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2"]),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"));
            await GivenARefreshedPortfolio(GivenAPortfolio("Somewhere else"), AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"));

            WhenThePortfolioSetsItsDependenciesAside(portfolioId);

            Assert.That(await TheReasonsAgainst("F-1"), Has.All.EqualTo("IgnoredByPortfolio"));
        }

        // @edge @us-10 - "A dependency another Portfolio still honours keeps the verdict it had". A Feature
        // can belong to several Portfolios, and one of them trying out a different order must not decide
        // what another one's plan is allowed to see.
        [Test]
        public async Task A_dependency_another_Portfolio_still_acts_on_keeps_the_verdict_it_had()
        {
            var oneOfThem = GivenAPortfolio("Platform");
            var theOtherOne = GivenAPortfolio("Payments");

            TrackedFeature[] theSameTwoFeatures =
            [
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2"]),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"),
            ];

            await GivenARefreshedPortfolio(oneOfThem, theSameTwoFeatures);
            await GivenARefreshedPortfolio(theOtherOne, theSameTwoFeatures);

            WhenThePortfolioSetsItsDependenciesAside(oneOfThem);
            var whileOnlyOneHasSetThemAside = await TheReasonsAgainst("F-1");

            WhenThePortfolioSetsItsDependenciesAside(theOtherOne);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileOnlyOneHasSetThemAside, Has.All.Null,
                    "One Portfolio's what-if would otherwise rewrite the other's plan.");
                Assert.That(await TheReasonsAgainst("F-1"), Has.All.EqualTo("IgnoredByPortfolio"));
            }
        }

        // @edge @us-10 - a Feature waited on that sits lower down in the order is the one thing worth
        // saying about a dependency that is no reason to leave it out. While the dependencies are set
        // aside there is nothing to leave out, so there is nothing to say about the order either.
        [Test]
        public async Task Nothing_is_said_about_the_order_while_the_dependencies_are_set_aside()
        {
            var portfolioId = GivenAPortfolio("Platform");
            await GivenARefreshedPortfolio(
                portfolioId,
                AFeatureWaitingOn("F-1", "Rebuild the search index", ["F-2"]),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"));
            GivenTheFeatureIsPlaced("F-1", 1);
            GivenTheFeatureIsPlaced("F-2", 2);

            WhenThePortfolioSetsItsDependenciesAside(portfolioId);

            Assert.That(await TheReasonsAgainst("F-1"), Has.All.EqualTo("IgnoredByPortfolio"));
        }
    }
}
