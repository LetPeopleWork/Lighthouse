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

        public static RealityCheckAnswer ReadFrom(RealityCheckResultDto result, RealityCheckForecastScript forecasts)
        {
            RealityCheckAnswer.ThenEveryCheckLearnedFromExactlyItsOwnWindow(forecasts);

            return new RealityCheckAnswer(JsonSerializer.SerializeToElement(result, AsACallerReadsIt));
        }
    }
}
