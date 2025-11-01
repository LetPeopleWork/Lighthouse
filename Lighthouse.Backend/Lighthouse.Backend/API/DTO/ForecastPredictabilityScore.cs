using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.API.DTO
{
    public class ForecastPredictabilityScore
    {
        public ForecastPredictabilityScore(HowManyForecast forecast)
        {
            Percentiles.Add(new PercentileValue(50, forecast.GetProbability(50)));
            Percentiles.Add(new PercentileValue(70, forecast.GetProbability(70)));
            Percentiles.Add(new PercentileValue(85, forecast.GetProbability(85)));
            Percentiles.Add(new PercentileValue(95, forecast.GetProbability(95)));

            ForecastResults = new Dictionary<int, int>(forecast.SimulationResult);
            PredictabilityScore = CalculatePredictabilityScore();
        }

        public double PredictabilityScore { get; }

        public Dictionary<int, int> ForecastResults { get; }

        public List<PercentileValue> Percentiles { get; } = new List<PercentileValue>();

        private double CalculatePredictabilityScore()
        {
            var firstPercentile = Percentiles[0];
            var lastPercentile = Percentiles[Percentiles.Count - 1];

            if (firstPercentile.Value == 0)
            {
                return 0;
            }

            return lastPercentile.Value / (double)firstPercentile.Value;
        }
    }
}
