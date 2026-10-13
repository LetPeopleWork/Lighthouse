using System.Globalization;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// Step definitions for slice 02, read off the wire as the Feature Size Percentiles widget reads
    /// them: the four percentiles, and a comparison whose direction and rows carry the trend.
    /// </summary>
    public partial class Slice02TeamFeatureSizePercentilesTest : TeamFeatureSizeAcceptanceTest
    {
        // --- Given ---

        /// <summary>Closed 35 days ago and after, inside the 30 days before the window.</summary>
        private void GivenInThePeriodBeforeGravitysPartsWere(params int[] parts)
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var gravity = TeamNamed(Gravity);

            for (var index = 0; index < parts.Length; index++)
            {
                SeedFeature($"OE-1{index:00}", StateCategories.Done, [oceanExplorer], [(gravity, parts[index])], closedDaysAgo: 35 + index);
            }
        }

        /// <summary>
        /// Gravity's part of the Feature closed on the first day of the period before is 6, of the one
        /// closed the day before that 50, and of the one closed on the window's first day 1.
        /// </summary>
        private void GivenGravityClosedFeaturesOnTheEdgesOfThePeriodBefore()
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var gravity = TeamNamed(Gravity);
            var windowStartDaysAgo = (Today.UtcDateTime.Date - WindowStart).Days;

            SeedFeature("OE-701", StateCategories.Done, [oceanExplorer], [(gravity, 6)], closedDaysAgo: windowStartDaysAgo + 30);
            SeedFeature("OE-702", StateCategories.Done, [oceanExplorer], [(gravity, 50)], closedDaysAgo: windowStartDaysAgo + 31);
            SeedFeature("OE-703", StateCategories.Done, [oceanExplorer], [(gravity, 1)], closedDaysAgo: windowStartDaysAgo);
        }

        private void GivenFeaturesClosedInTheWindowThatAreNotGravitys()
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var gravity = TeamNamed(Gravity);
            var zenith = TeamNamed(Zenith);

            SeedFeature("OE-201", StateCategories.Done, [oceanExplorer], [(gravity, 60)], closedDaysAgo: 3, sizedByThePortfolioDefault: true);
            SeedFeature("OE-202", StateCategories.Done, [oceanExplorer], [(zenith, 40)], closedDaysAgo: 3);
            SeedFeature("OE-203", StateCategories.Done, [oceanExplorer], [(gravity, 0), (zenith, 40)], closedDaysAgo: 3);
        }

        private void GivenGravityHasOnlyOpenFeatures()
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var gravity = TeamNamed(Gravity);

            SeedFeature("OE-501", StateCategories.ToDo, [oceanExplorer], [(gravity, 4)]);
            SeedFeature("OE-502", StateCategories.Doing, [oceanExplorer], [(gravity, 9)]);
        }

        // --- When ---

        private async Task WhenTheFeatureSizePercentilesAreAskedFor(string team)
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(team), FeatureSizePercentilesInfo, WindowStart, WindowEnd));
        }

        private async Task WhenTheFeatureSizePercentilesAreAskedForTheSingleDay(string team, int daysAgo)
        {
            var day = Today.UtcDateTime.Date.AddDays(-daysAgo);

            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(team), FeatureSizePercentilesInfo, day, day));
        }

        private async Task WhenTheFeatureSizePercentilesOfATeamThatDoesNotExistAreAskedFor()
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(NoSuchTeam, FeatureSizePercentilesInfo, WindowStart, WindowEnd));
        }

        private async Task WhenTheFeatureSizePercentilesAreAskedForAWindowThatEndsBeforeItStarts(string team)
        {
            Client.AsSystemAdmin();
            await Ask(TeamMetric(TeamNamed(team), FeatureSizePercentilesInfo, WindowEnd, WindowStart));
        }

        private async Task WhenOceanExplorersFeatureSizePercentilesAreAskedFor()
        {
            Client.AsSystemAdmin();
            await Ask(PortfolioMetric(PortfolioNamed(OceanExplorer), FeatureSizePercentilesInfo, WindowStart, WindowEnd));
        }

        private Task WhenAReaderOfGravityAndAReaderOfZenithBothAskForGravitysFeatureSizePercentiles()
            => WhenAReaderOfTheTeamAndAReaderOfAnotherTeamBothAsk(
                TeamMetric(TeamNamed(Gravity), FeatureSizePercentilesInfo, WindowStart, WindowEnd), TeamNamed(Gravity), TeamNamed(Zenith));

        // --- Then ---

        private void ThenThePercentilesAre(int p50, int p70, int p85, int p95)
        {
            var expected = new Dictionary<int, int> { [50] = p50, [70] = p70, [85] = p85, [95] = p95 };

            Assert.That(PercentilesShown(), Is.EquivalentTo(expected), $"Body: {Body}");
        }

        private void ThenThePercentilesAreThoseOfTheFeaturesGravityWorksOn(int howMany)
        {
            Assert.That(PercentilesShown(), Is.EquivalentTo(PercentilesOfTheFeaturesGravityWorksOn(howMany)), $"Body: {Body}");
        }

        private void ThenThereAreNoPercentiles()
        {
            Assert.That(PercentilesShown(), Is.Empty, $"Body: {Body}");
        }

        /// <summary>
        /// The direction, and the 50th row's previous value, which is the number the direction was
        /// read from.
        /// </summary>
        private void ThenTheTrendReads(string direction, int previousMedian)
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            var comparison = document.RootElement.GetProperty("comparison");
            var medianRow = comparison.GetProperty("detailRows")
                .EnumerateArray()
                .Single(row => row.GetProperty("label").GetString() == "50th");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(comparison.GetProperty("direction").GetString(), Is.EqualTo(direction), $"Body: {Body}");
                Assert.That(medianRow.GetProperty("previousValue").GetString(),
                    Is.EqualTo(previousMedian.ToString(CultureInfo.InvariantCulture)), $"Body: {Body}");
            }
        }

        /// <summary>The period the widget names as the one compared against, both ends counted.</summary>
        private void ThenThePeriodBeforeRuns(DateTime firstDay, DateTime lastDay)
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            var previousLabel = document.RootElement.GetProperty("comparison").GetProperty("previousLabel").GetString();

            Assert.That(previousLabel,
                Is.EqualTo(string.Create(CultureInfo.InvariantCulture, $"{firstDay:yyyy-MM-dd} – {lastDay:yyyy-MM-dd}")),
                $"Body: {Body}");
        }

        private void ThenTheMedianOfThePeriodBeforeIs(int previousMedian)
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            var medianRow = document.RootElement.GetProperty("comparison").GetProperty("detailRows")
                .EnumerateArray()
                .Single(row => row.GetProperty("label").GetString() == "50th");

            Assert.That(medianRow.GetProperty("previousValue").GetString(),
                Is.EqualTo(previousMedian.ToString(CultureInfo.InvariantCulture)), $"Body: {Body}");
        }

        // --- Helpers ---

        private Dictionary<int, int> PercentilesShown()
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            return document.RootElement
                .GetProperty("percentiles")
                .EnumerateArray()
                .ToDictionary(
                    percentile => percentile.GetProperty("percentile").GetInt32(),
                    percentile => percentile.GetProperty("value").GetInt32());
        }
    }
}
