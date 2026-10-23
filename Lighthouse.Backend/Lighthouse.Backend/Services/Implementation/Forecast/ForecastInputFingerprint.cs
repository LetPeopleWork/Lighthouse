using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// Everything one Portfolio forecast is about to hand the simulation, as the simulation would receive it.
    /// Two runs handed equal inputs compute the same forecast, randomness aside, so the second need not run.
    /// </summary>
    /// <param name="Today">The instance day the forecast counts from.</param>
    /// <param name="Limits">How many runs, how far each may go, and how many at once.</param>
    /// <param name="FeaturesInForecastOrder">The Features simulated, in the order their Teams work on them.</param>
    /// <param name="ThroughputByTeam">Per Team the forecast reads, the throughput and chip status it is handed.</param>
    /// <param name="Waits">What each Feature has to wait for before its work can begin.</param>
    public sealed record ForecastInputs(
        DateOnly Today,
        ForecastSimulationLimits Limits,
        IReadOnlyList<Feature> FeaturesInForecastOrder,
        IReadOnlyDictionary<int, ForecastThroughputStatus> ThroughputByTeam,
        ForecastWaits Waits);

    /// <summary>
    /// One short value standing for everything a Portfolio forecast hands the simulation, so an unchanged
    /// round can be recognised by comparing two strings.
    /// </summary>
    /// <remarks>
    /// A stored fingerprint has to still match after a restart and after an upgrade, so nothing here reaches
    /// for <c>GetHashCode</c> or object identity, and every number is written in the invariant culture. Only
    /// the Features keep the order they were handed in, because that order decides which Feature a Team works
    /// on first; everything else arrives in whatever order the database produced and is sorted.
    /// </remarks>
    public static class ForecastInputFingerprint
    {
        /// <summary>
        /// Bumped whenever the rendering below changes, so a value stored by an older version can never
        /// match a new one by accident and every forecast computes once after the upgrade.
        /// </summary>
        private const string FormatVersion = "1";

        public static IReadOnlyList<string> RegisteredMembers { get; } =
        [
            nameof(FormatVersion),
            nameof(ForecastInputs.Today),
            nameof(ForecastSimulationLimits.Trials),
            nameof(ForecastSimulationLimits.MostDaysOneSimulatedRunMayCover),
            nameof(ForecastInputs.FeaturesInForecastOrder),
            "TeamsOfTheFeatures",
            "RemainingWorkRows",
            "ThroughputAsHanded",
            "ThroughputFilterApplied",
            "ThroughputExcludedSummary",
            nameof(ForecastThroughputStatus.HasSufficientData),
            "FeatureWip",
            "WhatEachFeatureWaitsFor",
        ];

        public static string Of(ForecastInputs inputs)
        {
            var features = inputs.FeaturesInForecastOrder;
            var throughputByTeam = inputs.ThroughputByTeam;

            string[] whatTheSimulationIsHanded =
            [
                FormatVersion,
                inputs.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Render(inputs.Limits.Trials),
                Render(inputs.Limits.MostDaysOneSimulatedRunMayCover),
                string.Join(BetweenItems, features.Select(feature => Render(feature.Id))),
                Unordered(throughputByTeam.Keys.Select(Render)),
                Unordered(features.SelectMany(feature => feature.FeatureWork.Select(row =>
                    string.Join(WithinAnItem, Render(feature.Id), Render(row.TeamId), Render(row.RemainingWorkItems))))),
                PerTeam(throughputByTeam, status => Render(status.Throughput)),
                PerTeam(throughputByTeam, status => Render(status.FilterApplied)),
                PerTeam(throughputByTeam, status => status.ExcludedSummary ?? Absent),
                PerTeam(throughputByTeam, status => Render(status.HasSufficientData)),
                Unordered(features
                    .SelectMany(feature => feature.FeatureWork)
                    .Select(row => string.Join(WithinAnItem, Render(row.TeamId), Render(row.Team.FeatureWIP)))
                    .Distinct(StringComparer.Ordinal)),
                Unordered(features.Select(feature => string.Join(
                    WithinAnItem,
                    feature.ReferenceId,
                    Unordered(inputs.Waits.Of(feature.ReferenceId), BetweenNestedItems)))),
            ];

            return Digest(string.Join(BetweenProperties, whatTheSimulationIsHanded));
        }

        private const string Absent = "-";

        private const char BetweenProperties = '\u001E';

        private const char BetweenItems = '\u001F';

        private const char WithinAnItem = '\u001D';

        private const char BetweenNestedItems = '\u001C';

        private static string Render(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Render(bool value) => value ? "1" : "0";

        /// <summary>
        /// Day by day, in day order: the simulation draws a day by its position, so the same counts on other
        /// days are a different throughput.
        /// </summary>
        private static string Render(RunChartData throughput)
            => string.Join(
                BetweenNestedItems,
                throughput.WorkItemsPerUnitOfTime
                    .OrderBy(day => day.Key)
                    .Select(day => string.Join(WithinAnItem, Render(day.Key), Render(day.Value.Count))));

        private static string PerTeam(IReadOnlyDictionary<int, ForecastThroughputStatus> throughputByTeam, Func<ForecastThroughputStatus, string> render)
            => Unordered(throughputByTeam.Select(team => string.Join(WithinAnItem, Render(team.Key), render(team.Value))));

        private static string Unordered(IEnumerable<string> renderedItems, char separator = BetweenItems)
            => string.Join(separator, renderedItems.OrderBy(item => item, StringComparer.Ordinal));

        private static string Digest(string renderedInput)
            => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(renderedInput)).AsSpan(0, 16));
    }
}
