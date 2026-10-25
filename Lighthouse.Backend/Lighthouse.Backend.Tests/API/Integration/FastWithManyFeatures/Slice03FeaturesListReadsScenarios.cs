using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// What the Features tab reads is counted at the store's edge, so these scenarios hold however the reads are
    /// rewritten; the goldens hold the answers, withheld blockers included, to what they are today.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-03")]
    [Category("us-03")]
    [Category("story-6407")]
    public class Slice03FeaturesListReadsScenarios : FastWithManyFeaturesAcceptanceTest
    {
        private const string TheFeaturesOfNorthwindPlatform = "the Features of Northwind Platform";
        private const string TheParentsOfNorthwindPlatform = "the parents of Northwind Platform's Features";
        private const string WhatAtlasWorksOn = "what Atlas works on";

        private const string AConcurrencyToken = "concurrencyToken";

        private const string AnOlderCopyOfCheckoutRedesign = "Checkout redesign, an older copy";

        private const int TeamsOnCheckoutRedesign = 2;
        private const int TeamsOnSearchRevamp = 1;

        [TestCase(TheFeaturesOfNorthwindPlatform)]
        [TestCase(TheParentsOfNorthwindPlatform)]
        [TestCase(WhatAtlasWorksOn)]
        [Category("driving_port")]
        [Category("guardrail-G2")]
        [Category("guardrail-G3")]
        [Category("kpi-OUT-6400-answers-unchanged")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Every_row_dependency_and_Team_the_Features_tab_reads_is_exactly_as_before(string whatIsRead)
        {
            var northwind = GivenTheNorthwindInstance();

            var body = await TheBodyOf(TheRouteTo(whatIsRead, northwind));

            ApprovedBodies.Verify(body, ApprovedBodiesFolder, $"slice03-{TheApprovedNameOf(whatIsRead)}", AConcurrencyToken);
        }

        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-first-feature-row")]
        [Category("contract-shape:pure-function")]
        public async Task Northwind_Platforms_list_reads_the_forecast_histograms_of_the_Features_it_shows_and_no_others()
        {
            GivenTheNorthwindInstance();

            var answer = await TheAnswerCountingWhatTheDatabaseHandsBack(TheFeaturesByIdRoute(CheckoutRedesign, SearchRevamp));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheNamesIn(answer), Is.EquivalentTo(TheTwoOpenFeaturesOfNorthwindPlatform));
                Assert.That(Materialised.Of<IndividualSimulationResult>(),
                    Is.EqualTo(TheHistogramRowsOfAFeatureWorkedOnBy(TeamsOnCheckoutRedesign) + TheHistogramRowsOfAFeatureWorkedOnBy(TeamsOnSearchRevamp)),
                    $"Working out what two Features wait on read the forecast histograms of the whole instance. Read: {Materialised.Describe()}");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("kpi-OUT-6400-first-feature-row")]
        [Category("contract-shape:pure-function")]
        public async Task Asking_what_Atlas_works_on_reads_the_Feature_Atlas_works_on_and_no_others()
        {
            var northwind = GivenTheNorthwindInstance();

            var answer = await TheAnswerCountingWhatTheDatabaseHandsBack(TheFeaturesInProgressRoute(northwind.Atlas));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheNamesIn(answer), Is.EqualTo(OnlyCheckoutRedesign));
                Assert.That(Materialised.Of<Feature>(), Is.EqualTo(1),
                    $"Finding the one Feature Atlas works on read other Features too. Read: {Materialised.Describe()}");
            }
        }

        /// <summary>
        /// A tracker can hand Lighthouse two Features under one id, and a Team's page must still open. The copy in
        /// Northwind Archive is later in forecast order than the one in Northwind Platform, so its name tells the two
        /// apart. The warning is remembered alongside the Team's cached metrics, so a second read with nothing recomputed
        /// in between must not warn again: the operator hears about the id once each time the Team's metrics are worked
        /// out again (a refresh, a recomputation, or the cached figures running out), not once per page view.
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_stored_twice_under_one_id_answers_once_with_the_first_in_forecast_order_and_the_operator_is_warned_once_per_refresh()
        {
            var northwind = GivenTheNorthwindInstance();
            GivenThePortfolioHolds(northwind.Archive, new HeldFeature(CheckoutRedesign, AnOlderCopyOfCheckoutRedesign, [northwind.Atlas]));
            CapturedLogs.Clear();

            var firstStatus = await TheStatusOf(TheFeaturesInProgressRoute(northwind.Atlas), Client);
            var answer = await TheAnswerTo(TheFeaturesInProgressRoute(northwind.Atlas));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstStatus, Is.EqualTo(HttpStatusCode.OK), "Two Features under one id must not break the Team's page.");
                Assert.That(TheNamesIn(answer), Is.EqualTo(OnlyCheckoutRedesign),
                    "The Feature first in forecast order answers for the id, and only once.");
                Assert.That(TheWarningsNaming(CheckoutRedesign), Is.EqualTo(1),
                    "Two reads without a refresh between them warn once, naming the id. Warnings: " + string.Join(" | ", CapturedLogs.Warnings));
            }
        }

        /// <summary>
        /// A Team's refresh ends by recomputing its metrics, which throws away the remembered warning along with the cached figures, so the next read finds the id
        /// twice again and warns again. The refresh is run as that last step, because the faked tracker would hand the
        /// Team no work items and the in-progress Feature would vanish with them.
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task After_the_Team_is_refreshed_a_Feature_stored_twice_under_one_id_is_warned_about_once_more()
        {
            var northwind = GivenTheNorthwindInstance();
            GivenThePortfolioHolds(northwind.Archive, new HeldFeature(CheckoutRedesign, AnOlderCopyOfCheckoutRedesign, [northwind.Atlas]));
            CapturedLogs.Clear();
            var beforeTheRefresh = await TheStatusOf(TheFeaturesInProgressRoute(northwind.Atlas), Client);

            await WhenTheTeamIsRefreshed(northwind.Atlas);
            var afterTheRefresh = await TheAnswerTo(TheFeaturesInProgressRoute(northwind.Atlas));

            Assert.That(beforeTheRefresh, Is.EqualTo(HttpStatusCode.OK), "Two Features under one id must not break the Team's page.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheNamesIn(afterTheRefresh), Is.EqualTo(OnlyCheckoutRedesign),
                    "After the refresh the Feature first in forecast order still answers for the id, and only once.");
                Assert.That(TheWarningsNaming(CheckoutRedesign), Is.EqualTo(2),
                    "One warning before the refresh and one after it. Warnings: " + string.Join(" | ", CapturedLogs.Warnings));
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("error")]
        [Category("guardrail-G2")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_blocker_in_a_Portfolio_the_reader_may_not_read_stays_withheld()
        {
            var northwind = GivenTheNorthwindInstance();

            var answer = await TheAnswerTo(TheFeaturesByIdRoute(CheckoutRedesign), AViewerOfThePortfolio(northwind.Platform));

            var waitsOn = TheFeatureNamed(answer, CheckoutRedesignsName).GetProperty("dependsOn");
            Assert.That(waitsOn.GetArrayLength(), Is.EqualTo(1), $"Checkout redesign still waits on one Feature: {waitsOn}");
            var theBlocker = waitsOn.EnumerateArray().Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(theBlocker.GetProperty("isWithheld").GetBoolean(), Is.True,
                    $"Payment provider switch sits in Northwind Payments, which this reader may not read: {waitsOn}");
                Assert.That(theBlocker.ToString(), Does.Not.Contain("Payment provider switch"));
            }
        }

        private static readonly string[] TheTwoOpenFeaturesOfNorthwindPlatform = [CheckoutRedesignsName, "Search revamp"];

        private static readonly string[] OnlyCheckoutRedesign = [CheckoutRedesignsName];

        private int TheWarningsNaming(string referenceId)
            => CapturedLogs.Warnings.Count(warning => warning.Contains(referenceId, StringComparison.Ordinal));

        private async Task WhenTheTeamIsRefreshed(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            await scope.ServiceProvider.GetRequiredService<ITeamMetricsService>().UpdateTeamMetrics(team);
        }

        private static int TheHistogramRowsOfAFeatureWorkedOnBy(int teams)
            => (teams * (HistogramRowsOfACompletionForecast + HistogramRowsOfAStartForecast)) + HistogramRowsOfAStartForecast;

        private string TheRouteTo(string whatIsRead, Northwind northwind) => whatIsRead switch
        {
            TheFeaturesOfNorthwindPlatform => TheFeaturesByIdRoute(CheckoutRedesign, SearchRevamp, LegacyCleanup),
            TheParentsOfNorthwindPlatform => TheFeaturesByReferenceRoute(Platform2027),
            WhatAtlasWorksOn => TheFeaturesInProgressRoute(northwind.Atlas),
            _ => throw new ArgumentOutOfRangeException(nameof(whatIsRead), whatIsRead, "Not a read of the Features tab."),
        };

        private static string TheApprovedNameOf(string whatIsRead) => whatIsRead switch
        {
            TheFeaturesOfNorthwindPlatform => "features-by-id-northwind-platform",
            TheParentsOfNorthwindPlatform => "features-by-reference-parents",
            WhatAtlasWorksOn => "features-in-progress-atlas",
            _ => throw new ArgumentOutOfRangeException(nameof(whatIsRead), whatIsRead, "Not a read of the Features tab."),
        };
    }
}
