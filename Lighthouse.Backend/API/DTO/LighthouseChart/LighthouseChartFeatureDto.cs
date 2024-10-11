using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO.LighthouseChart
{
    public class LighthouseChartFeatureDto
    {
        public LighthouseChartFeatureDto(Feature feature)
        {
            Name = feature.Name;

            if (feature.Forecast != null && feature.FeatureWork.Sum(r => r.RemainingWorkItems) > 0)
            {
                var forecastDtos = feature.Forecast.CreateForecastDtos([50, 70, 85, 95]);
                Forecasts.AddRange(forecastDtos.Select(f => f.ExpectedDate));
            }
        }

        public string Name { get; set; }

        public List<DateTime> Forecasts { get; set; } = new List<DateTime>();

        public List<RemainingItemsDto> RemainingItemsTrend { get; set; } = new List<RemainingItemsDto>();
    }
}
