using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// Step definitions for slice 01. Sizes are read off the wire, field by field, as the scatter
    /// reads them: a Feature's <c>size</c> on a Team route is that Team's part of it.
    /// </summary>
    public partial class Slice01TeamFeatureSizeTest : TeamFeatureSizeAcceptanceTest
    {
        // Ocean Explorer's demo Features that several Teams work on, and one only Gravity works on.
        private const string DeepSeaMapping = "OE-001";
        private const string CoralReefRestoration = "OE-002";
        private const string KelpForestConservation = "OE-007";
        private const string DeepWaterMineralSurvey = "OE-008";
        private const string OnlyGravityWorkedOnIt = "OE-011";

        // Made up for the rules the numbers turn on.
        private const string OnlyZenithWorksOnIt = "OE-201";
        private const string GravitysPartDroppedToNothing = "OE-202";
        private const string SizedByThePortfolioDefault = "OE-203";
        private const string GravityClosedItBeforeTheWindow = "OE-204";
        private const string OrionsFeature = "OR-001";
        private const string InTwoPortfolios = "OE-601";

        // Seeded by GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen, with Gravity's part of 3.
        private const string GravityClosedItThreeDaysAgo = "OE-303";

        private static readonly string[] OnlyTheFeatureGravityClosedThreeDaysAgo = [GravityClosedItThreeDaysAgo];

        private static readonly string[] TeamsWorkingOnOceanExplorer = [Equinox, Gravity, Lightspeed, Zenith, Meridian];

        private static readonly string[] GravitysFeaturesInTheWindow =
            [DeepSeaMapping, CoralReefRestoration, KelpForestConservation, DeepWaterMineralSurvey, OnlyGravityWorkedOnIt, OrionsFeature];

        private int sumOfTheParts;

        public enum PolarisHas
        {
            NoWorkItemAtAll,
            WorkOnlyUnderAParentNoPortfolioLoads,
        }

        // --- Given ---

        /// <summary>
        /// Ocean Explorer's Features that several Teams work on, with the demo's Work Items per Team,
        /// in states chosen for the rules rather than the demo's own:
        /// <list type="bullet">
        /// <item>OE-001, Done 5 days ago: Gravity 4, Zenith 5, Meridian 8 (17).</item>
        /// <item>OE-002, To Do: Equinox 8, Gravity 4, Lightspeed 4, Zenith 3 (19).</item>
        /// <item>OE-007, In Progress: Gravity 4, Lightspeed 4, Zenith 2 (10).</item>
        /// <item>OE-008, To Do: Gravity 4, Zenith 6 (10).</item>
        /// <item>OE-011, Done 4 days ago: Gravity 4 and nobody else.</item>
        /// </list>
        /// And the made-up ones:
        /// <list type="bullet">
        /// <item>OE-201, To Do: Zenith 9 and nobody else.</item>
        /// <item>OE-202, In Progress: Gravity 0, Zenith 2.</item>
        /// <item>OE-203, To Do, sized by the Portfolio's default: Gravity 10, made up.</item>
        /// <item>OE-204, Done 45 days ago, before the window: Gravity 7.</item>
        /// <item>OR-001, In Progress, in Orion: Gravity 2.</item>
        /// </list>
        /// </summary>
        private void GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt()
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var orion = PortfolioNamed(Orion);
            var equinox = TeamNamed(Equinox);
            var gravity = TeamNamed(Gravity);
            var lightspeed = TeamNamed(Lightspeed);
            var zenith = TeamNamed(Zenith);
            var meridian = TeamNamed(Meridian);

            SeedFeature(DeepSeaMapping, StateCategories.Done, [oceanExplorer], [(gravity, 4), (zenith, 5), (meridian, 8)], closedDaysAgo: 5);
            SeedFeature(CoralReefRestoration, StateCategories.ToDo, [oceanExplorer], [(equinox, 8), (gravity, 4), (lightspeed, 4), (zenith, 3)]);
            SeedFeature(KelpForestConservation, StateCategories.Doing, [oceanExplorer], [(gravity, 4), (lightspeed, 4), (zenith, 2)]);
            SeedFeature(DeepWaterMineralSurvey, StateCategories.ToDo, [oceanExplorer], [(gravity, 4), (zenith, 6)]);
            SeedFeature(OnlyGravityWorkedOnIt, StateCategories.Done, [oceanExplorer], [(gravity, 4)], closedDaysAgo: 4);

            SeedFeature(OnlyZenithWorksOnIt, StateCategories.ToDo, [oceanExplorer], [(zenith, 9)]);
            SeedFeature(GravitysPartDroppedToNothing, StateCategories.Doing, [oceanExplorer], [(gravity, 0), (zenith, 2)]);
            SeedFeature(SizedByThePortfolioDefault, StateCategories.ToDo, [oceanExplorer], [(gravity, 10)], sizedByThePortfolioDefault: true);
            SeedFeature(GravityClosedItBeforeTheWindow, StateCategories.Done, [oceanExplorer], [(gravity, 7)], closedDaysAgo: 45);
            SeedFeature(OrionsFeature, StateCategories.Doing, [orion], [(gravity, 2)]);
        }

        private void GivenPolaris(PolarisHas work)
        {
            var polaris = TeamNamed(Polaris);

            if (work == PolarisHas.WorkOnlyUnderAParentNoPortfolioLoads)
            {
                SeedWorkItemInProgress(polaris, parentReferenceId: "UNTRACKED-1");
            }
        }

        private void GivenGravityIsWorkingOn(string feature) => GivenTheTeamIsWorkingOn(Gravity, feature);

        private void GivenTheTeamIsWorkingOn(string team, string feature) => SeedWorkItemInProgress(TeamNamed(team), feature);

        private void GivenBiggerFeaturesThatDoNotCount()
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var gravity = TeamNamed(Gravity);

            SeedFeature("OE-390", StateCategories.Doing, [oceanExplorer], [(gravity, 50)]);
            SeedFeature("OE-391", StateCategories.Done, [oceanExplorer], [(gravity, 60)], closedDaysAgo: 3, sizedByThePortfolioDefault: true);
            SeedFeature("OE-392", StateCategories.Done, [oceanExplorer], [(gravity, 100)], closedDaysAgo: 45);
        }

        /// <summary>
        /// OE-601, In Progress, sits in Ocean Explorer and in Orion, and Gravity has a Work Item in
        /// progress under it. Only Ocean Explorer counts a Feature in progress as blocked.
        /// </summary>
        private void GivenGravityIsWorkingOnAFeatureOnlyOneOfItsPortfoliosCountsAsBlocked()
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var orion = PortfolioNamed(Orion);
            var gravity = TeamNamed(Gravity);

            CountFeaturesInProgressAsBlocked(oceanExplorer);
            SeedFeature(InTwoPortfolios, StateCategories.Doing, [oceanExplorer, orion], [(gravity, 4)]);
            SeedWorkItemInProgress(gravity, InTwoPortfolios);
        }

        // --- When ---

        private async Task WhenTheFeatureSizesAreAskedFor(string team)
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(team), AllFeaturesForSizeChart, WindowStart, WindowEnd));
        }

        private async Task WhenGravityIsAskedFor(string metric)
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(Gravity), metric, WindowStart, WindowEnd));
        }

        private async Task WhenTheFeatureSizesOfATeamThatDoesNotExistAreAskedFor(string metric)
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(NoSuchTeam, metric, WindowStart, WindowEnd));
        }

        private async Task WhenEveryTeamThatWorksOnOceanExplorerIsAskedForItsPartOf(string referenceId)
        {
            sumOfTheParts = 0;
            foreach (var team in TeamsWorkingOnOceanExplorer)
            {
                await WhenTheFeatureSizesAreAskedFor(team);
                sumOfTheParts += SizesShown().GetValueOrDefault(referenceId);
            }
        }

        private async Task WhenOceanExplorersFeatureSizesAreAskedFor()
        {
            Client.AsSystemAdmin();
            await Ask(PortfolioMetric(PortfolioNamed(OceanExplorer), AllFeaturesForSizeChart, WindowStart, WindowEnd));
        }

        private async Task WhenSomeoneWhoReadsTheTeamAndOceanExplorerAsksForItsFeatureSizes(string team)
        {
            AsAReaderOf(TeamNamed(team), PortfolioNamed(OceanExplorer));
            await Ask(TeamMetric(TeamNamed(team), AllFeaturesForSizeChart, WindowStart, WindowEnd));
        }

        private Task WhenAReaderOfGravityAndAReaderOfZenithBothAskForGravitys(string metric)
            => WhenAReaderOfTheTeamAndAReaderOfAnotherTeamBothAsk(
                TeamMetric(TeamNamed(Gravity), metric, WindowStart, WindowEnd), TeamNamed(Gravity), TeamNamed(Zenith));

        private Task WhenAReaderOfGravityAndAReaderOfZenithBothAskForTheFeaturesGravityIsWorkingOn()
            => WhenAReaderOfTheTeamAndAReaderOfAnotherTeamBothAsk(
                TeamFeaturesInProgress(TeamNamed(Gravity)), TeamNamed(Gravity), TeamNamed(Zenith));

        private async Task WhenAskedForAWindowThatEndsBeforeItStarts(string team, string metric)
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(team), metric, WindowEnd, WindowStart));
        }

        private async Task WhenGravityIsAskedForTheSingleDay(string metric, int daysAgo)
        {
            var day = Today.UtcDateTime.Date.AddDays(-daysAgo);

            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(Gravity), metric, day, day));
        }

        private async Task WhenGravityIsAskedForItsFeatures(string list)
        {
            var gravity = TeamNamed(Gravity);
            var route = list == FeaturesInProgress
                ? TeamFeaturesInProgress(gravity)
                : TeamMetric(gravity, list, WindowStart, WindowEnd);

            Client.AsSystemAdmin();
            await Ask(route);
        }

        private async Task WhenTheSizePercentilesAreAskedFor(string team)
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(team), SizePercentiles, WindowStart, WindowEnd));
        }

        private async Task WhenTheFeaturesWorkedOnAreAskedFor(string team)
        {
            Client.AsSystemAdmin();
            await Ask(TeamFeaturesInProgress(TeamNamed(team)));
        }

        // --- Then ---

        private void ThenTheFeatureReads(string referenceId, int expectedSize)
        {
            Assert.That(SizesShown(), Does.ContainKey(referenceId).WithValue(expectedSize),
                $"{referenceId} should read {expectedSize}. Body: {Body}");
        }

        private void ThenThePartsAddUpTo(int sizeOnThePortfolio)
        {
            Assert.That(sumOfTheParts, Is.EqualTo(sizeOnThePortfolio),
                "Every Team's part of the Feature, added up, should be the Feature's size on its Portfolio.");
        }

        private void ThenTheFeaturesShownAre(string[] referenceIds)
        {
            Assert.That(SizesShown().Keys, Is.EquivalentTo(referenceIds), $"Body: {Body}");
        }

        private void ThenTheseFeaturesAreNotShown(params string[] referenceIds)
        {
            var shown = SizesShown();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.Keys, Is.Not.Empty, $"The Team's own Features should still be shown. Body: {Body}");

                foreach (var referenceId in referenceIds)
                {
                    Assert.That(shown.Keys, Does.Not.Contain(referenceId), $"Body: {Body}");
                }
            }
        }

        private void ThenTheFeatureNamesThePortfolios(string referenceId, params string[] portfolios)
        {
            var portfolioNames = TheFeature(referenceId)
                .GetProperty("projects")
                .EnumerateArray()
                .Select(project => project.GetProperty("name").GetString())
                .ToList();

            Assert.That(portfolioNames, Is.EquivalentTo(portfolios), $"Body: {Body}");
        }

        private void ThenTheFeatureIsBlocked(string referenceId)
        {
            Assert.That(TheFeature(referenceId).GetProperty("isBlocked").GetBoolean(), Is.True,
                $"{referenceId} is blocked in one of its Portfolios. Body: {Body}");
        }

        private void ThenNoFeatureIsShown()
        {
            Assert.That(SizesShown(), Is.Empty, $"Body: {Body}");
        }

        private void ThenTheSizePercentilesAre(int p50, int p70, int p85, int p95)
        {
            var expected = new Dictionary<int, int> { [50] = p50, [70] = p70, [85] = p85, [95] = p95 };

            Assert.That(PercentilesShown(), Is.EquivalentTo(expected), $"Body: {Body}");
        }

        private void ThenThereAreNoSizePercentiles()
        {
            Assert.That(PercentilesShown(), Is.Empty, $"Body: {Body}");
        }

        /// <summary>
        /// The answer holds every Feature Gravity works on, or the percentiles of the Done ones, so
        /// an answer left over from fewer Features cannot pass for the right one.
        /// </summary>
        private void ThenTheAnswerCoversTheFeaturesGravityWorksOn(string metric, int howMany)
        {
            if (metric == AllFeaturesForSizeChart)
            {
                Assert.That(SizesShown(), Has.Count.EqualTo(howMany), $"Body: {Body}");
            }
            else
            {
                Assert.That(PercentilesShown(), Is.EquivalentTo(PercentilesOfTheFeaturesGravityWorksOn(howMany)), $"Body: {Body}");
            }
        }

        // --- Helpers ---

        private void CountFeaturesInProgressAsBlocked(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = portfolios.GetById(portfolioId)!;

            portfolio.BlockedRuleSetJson = JsonSerializer.Serialize(new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = "feature.state", Operator = RuleOperators.Equals, Value = InProgress },
                ],
            });

            portfolios.Update(portfolio);
            portfolios.Save().GetAwaiter().GetResult();
        }

        private Dictionary<string, int> SizesShown()
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            return document.RootElement
                .EnumerateArray()
                .ToDictionary(
                    feature => feature.GetProperty("referenceId").GetString()!,
                    feature => feature.GetProperty("size").GetInt32());
        }

        private JsonElement TheFeature(string referenceId)
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            return document.RootElement
                .EnumerateArray()
                .Single(feature => feature.GetProperty("referenceId").GetString() == referenceId)
                .Clone();
        }

        private Dictionary<int, int> PercentilesShown()
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            return document.RootElement
                .EnumerateArray()
                .ToDictionary(
                    percentile => percentile.GetProperty("percentile").GetInt32(),
                    percentile => percentile.GetProperty("value").GetInt32());
        }
    }
}
