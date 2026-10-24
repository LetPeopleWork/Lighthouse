using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// A row of the new list is meant to be today's Feature row minus the per-Team forecasts the grid never reads, so
    /// each row is compared with the same Feature read the old way: that comparison is what shows the dates, progress
    /// and dependencies did not move.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    [Category("us-04")]
    [Category("story-6408")]
    public class Slice04OneListPerOwnerScenarios : FastWithManyFeaturesAcceptanceTest
    {
        private const string TeamForecasts = "teamForecasts";
        private const string Features = "features";
        private const string Parents = "parents";
        private const string ActiveWork = "activeWork";
        private const string ReferenceId = "referenceId";
        private const string Name = "name";

        private const int AnOwnerThatDoesNotExist = 9999;

        private static readonly string[] NorthwindPlatformInForecastOrder = [CheckoutRedesignsName, "Search revamp", "Legacy cleanup"];

        private static readonly string[] WhatBorealisWorksOn = ["Payment provider switch", "Search revamp"];

        private static readonly string[] AtlasAndCobalt = ["Atlas", "Cobalt"];

        private static readonly string[] OnlyPaymentProviderSwitch = [PaymentProviderSwitch];

        private static string TheListOfThePortfolio(int portfolioId) => $"/api/latest/portfolios/{portfolioId}/feature-list";

        private static string TheListOfTheTeam(int teamId) => $"/api/latest/teams/{teamId}/feature-list";

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-first-feature-row")]
        [Category("contract-shape:pure-function")]
        public async Task Martin_opens_Northwind_Platforms_Features_and_one_answer_carries_every_row_its_parents_and_who_works_on_them()
        {
            var northwind = GivenTheNorthwindInstance();

            var list = await TheAnswerTo(TheListOfThePortfolio(northwind.Platform));

            var checkoutRedesign = TheFeatureNamed(list.GetProperty(Features), CheckoutRedesignsName);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheNamesIn(list.GetProperty(Features)), Is.EqualTo(NorthwindPlatformInForecastOrder));
                Assert.That(checkoutRedesign.TryGetProperty(TeamForecasts, out _), Is.False,
                    "A row leaves out the per-Team forecasts the grid never reads.");
                Assert.That(checkoutRedesign.GetProperty("forecasts").GetArrayLength(), Is.GreaterThan(0),
                    "A row still carries the Feature's forecast dates.");
                Assert.That(TheParentNamed(list, Platform2027), Is.EqualTo("Platform 2027"));
                Assert.That(TheTeamsWorkingOn(list, CheckoutRedesign, northwind), Is.EquivalentTo(AtlasAndCobalt));
            }
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("contract-shape:pure-function")]
        public async Task Aisha_opens_Team_Borealis_Features_and_gets_the_Features_Borealis_works_on_across_Portfolios()
        {
            var northwind = GivenTheNorthwindInstance();

            var list = await TheAnswerTo(TheListOfTheTeam(northwind.Borealis));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheNamesIn(list.GetProperty(Features)), Is.EquivalentTo(WhatBorealisWorksOn));
                Assert.That(TheReferencesWorkedOnBy(list, northwind.Borealis), Is.EqualTo(OnlyPaymentProviderSwitch));
            }
        }

        /// <summary>
        /// Every field of every row is the field today's Features read returns for the same Feature, the
        /// per-Team forecasts aside. Dates, progress, position, what it waits on and whether it can be moved
        /// are compared as the client receives them.
        /// </summary>
        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("guardrail-G2")]
        [Category("kpi-OUT-6400-answers-unchanged")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Every_row_reads_exactly_as_the_same_Feature_reads_today()
        {
            var northwind = GivenTheNorthwindInstance();

            var list = await TheAnswerTo(TheListOfThePortfolio(northwind.Platform));
            var today = await TheAnswerTo(TheFeaturesByIdRoute(CheckoutRedesign, SearchRevamp, LegacyCleanup));

            Assert.That(TheRowsByReference(list.GetProperty(Features)), Is.EqualTo(TheRowsByReference(today, TeamForecasts)),
                "A row of the list differs from today's row for the same Feature.");
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("guardrail-G2")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task The_parents_and_the_Teams_at_work_read_as_the_two_reads_they_replace()
        {
            var northwind = GivenTheNorthwindInstance();

            var list = await TheAnswerTo(TheListOfThePortfolio(northwind.Platform));
            var parentToday = TheFeatureNamed(await TheAnswerTo(TheFeaturesByReferenceRoute(Platform2027)), "Platform 2027");
            var atlasToday = await TheAnswerTo(TheFeaturesInProgressRoute(northwind.Atlas));
            var cobaltToday = await TheAnswerTo(TheFeaturesInProgressRoute(northwind.Cobalt));

            var parent = TheParent(list, Platform2027);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(parent.GetProperty(Name).GetString(), Is.EqualTo(parentToday.GetProperty(Name).GetString()));
                Assert.That(parent.GetProperty("url").GetString(), Is.EqualTo(parentToday.GetProperty("url").GetString()));
                Assert.That(TheReferencesWorkedOnBy(list, northwind.Atlas), Is.EqualTo(TheReferencesIn(atlasToday)));
                Assert.That(TheReferencesWorkedOnBy(list, northwind.Cobalt), Is.EqualTo(TheReferencesIn(cobaltToday)));
            }
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-first-feature-row")]
        [Category("contract-shape:pure-function")]
        public async Task The_list_reads_its_own_Features_completion_histograms_and_leaves_the_per_Team_start_histograms_out()
        {
            var northwind = GivenTheNorthwindInstance();

            await TheAnswerCountingWhatTheDatabaseHandsBack(TheListOfThePortfolio(northwind.Platform));

            const int checkoutRedesign = (2 * HistogramRowsOfACompletionForecast) + HistogramRowsOfAStartForecast;
            const int searchRevamp = HistogramRowsOfACompletionForecast + HistogramRowsOfAStartForecast;
            Assert.That(Materialised.Of<IndividualSimulationResult>(), Is.EqualTo(checkoutRedesign + searchRevamp),
                $"The list read histograms it never shows. Read: {Materialised.Describe()}");
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public async Task A_Portfolio_that_holds_no_Features_yet_answers_with_an_empty_list()
        {
            var northwind = GivenTheNorthwindInstance();

            var list = await TheAnswerTo(TheListOfThePortfolio(northwind.Mobile));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.GetProperty(Features).GetArrayLength(), Is.Zero);
                Assert.That(list.GetProperty(Parents).GetArrayLength(), Is.Zero);
            }
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("error")]
        [Category("rbac")]
        [Category("contract-shape:pure-function")]
        public async Task A_reader_who_may_not_open_Northwind_Platform_is_refused_its_list()
        {
            var northwind = GivenTheNorthwindInstance();

            var status = await TheStatusOf(TheListOfThePortfolio(northwind.Platform), AViewerOfThePortfolio(northwind.Payments));

            Assert.That(status, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("error")]
        [Category("rbac")]
        [Category("contract-shape:pure-function")]
        public async Task A_reader_who_may_not_open_Team_Borealis_is_refused_its_list()
        {
            var northwind = GivenTheNorthwindInstance();

            var status = await TheStatusOf(TheListOfTheTeam(northwind.Borealis), AViewerOfTheTeam(northwind.Atlas));

            Assert.That(status, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("error")]
        [Category("rbac")]
        [Category("guardrail-G2")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Inside_a_list_the_reader_may_open_a_blocker_they_may_not_read_is_withheld()
        {
            var northwind = GivenTheNorthwindInstance();

            var list = await TheAnswerTo(TheListOfThePortfolio(northwind.Platform), AViewerOfThePortfolio(northwind.Platform));

            var waitsOn = TheFeatureNamed(list.GetProperty(Features), CheckoutRedesignsName).GetProperty("dependsOn");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(waitsOn.GetArrayLength(), Is.EqualTo(1));
                Assert.That(waitsOn.EnumerateArray().Single().GetProperty("isWithheld").GetBoolean(), Is.True,
                    $"Payment provider switch sits in a Portfolio this reader may not read: {waitsOn}");
            }
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_list_of_a_Portfolio_that_does_not_exist_is_not_found()
        {
            var northwind = GivenTheNorthwindInstance();

            var anOwnerThatExists = await TheStatusOf(TheListOfThePortfolio(northwind.Platform), Client);
            var anOwnerThatDoesNot = await TheStatusOf(TheListOfThePortfolio(AnOwnerThatDoesNotExist), Client);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(anOwnerThatExists, Is.EqualTo(HttpStatusCode.OK), "Without a list for a Portfolio that exists, a missing one proves nothing.");
                Assert.That(anOwnerThatDoesNot, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        // --- Reading the list ---

        private static string? TheParentNamed(JsonElement list, string referenceId)
            => TheParent(list, referenceId).GetProperty(Name).GetString();

        private static JsonElement TheParent(JsonElement list, string referenceId)
        {
            foreach (var parent in list.GetProperty(Parents).EnumerateArray())
            {
                if (parent.GetProperty(ReferenceId).GetString() == referenceId)
                {
                    return parent;
                }
            }

            Assert.Fail($"The list names no parent {referenceId}: {list.GetProperty(Parents)}");
            return default;
        }

        private static List<string> TheReferencesWorkedOnBy(JsonElement list, int teamId)
        {
            foreach (var work in list.GetProperty(ActiveWork).EnumerateArray())
            {
                if (work.GetProperty("teamId").GetInt32() == teamId)
                {
                    return [.. work.GetProperty("featureReferenceIds").EnumerateArray().Select(reference => reference.GetString() ?? string.Empty)];
                }
            }

            return [];
        }

        private static List<string> TheTeamsWorkingOn(JsonElement list, string featureReferenceId, Northwind northwind)
        {
            var teams = new Dictionary<int, string> { [northwind.Atlas] = "Atlas", [northwind.Borealis] = "Borealis", [northwind.Cobalt] = "Cobalt" };

            return [.. teams.Where(team => TheReferencesWorkedOnBy(list, team.Key).Contains(featureReferenceId)).Select(team => team.Value)];
        }

        private static List<string> TheReferencesIn(JsonElement features)
            => [.. features.EnumerateArray().Select(feature => feature.GetProperty(ReferenceId).GetString() ?? string.Empty)];

        private static Dictionary<string, string> TheRowsByReference(JsonElement features, string? leftOut = null)
            => features.EnumerateArray().ToDictionary(
                feature => feature.GetProperty(ReferenceId).GetString() ?? string.Empty,
                feature => string.Join("\n", feature.EnumerateObject()
                    .Where(property => property.Name != leftOut)
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => $"{property.Name}={property.Value.GetRawText()}")));
    }
}
