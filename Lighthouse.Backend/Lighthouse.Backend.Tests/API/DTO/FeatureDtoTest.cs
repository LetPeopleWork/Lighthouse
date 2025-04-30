using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class FeatureDtoTest
    {
        [Test]
        public void CreateFeatureDto_GivenForecastCreationTime_ReturnsDateAsUTC()
        {
            var forecastCreationTime = DateTime.Now;

            var forecast = new WhenForecast()
            {
                CreationTime = forecastCreationTime
            };

            var feature = new Feature();
            feature.SetFeatureForecasts(new List<WhenForecast> { forecast });

            var subject = CreateSubject(feature);

            Assert.Multiple(() =>
            {
                Assert.That(subject.LastUpdated, Is.EqualTo(forecastCreationTime));
                Assert.That(subject.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        private FeatureDto CreateSubject(Feature feature)
        {
            return new FeatureDto(feature);
        }
    }
}
