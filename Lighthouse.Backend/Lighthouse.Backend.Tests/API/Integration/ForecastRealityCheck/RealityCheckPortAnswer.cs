using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Reads the answer the check's service returned, with no host in between, into the same reading a caller
    /// over HTTP gets, so a scenario reads it the same way wherever it ran.
    /// </summary>
    public static class RealityCheckPortAnswer
    {
        private static readonly JsonSerializerOptions AsACallerReadsIt = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>
        /// Every check is held here to learning from exactly its own window: no scenario has a blackout day, so a
        /// history one day longer or shorter than every window swept means a check read days that were not its own.
        /// </summary>
        public static RealityCheckAnswer ReadFrom(RealityCheckResultDto result, RealityCheckForecastScript forecasts)
        {
            var strayHistories = forecasts.HistoriesNoSweptWindowIsAsLongAs;

            Assert.That(strayHistories, Is.Empty,
                $"every check learns from exactly as many days as its window holds, yet a check learned from {string.Join(", ", strayHistories.Select(days => $"{days} days"))}, which is no window swept");

            return new RealityCheckAnswer(JsonSerializer.SerializeToElement(result, AsACallerReadsIt));
        }
    }
}
