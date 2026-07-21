using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class FeatureDtoTest
    {
        private const int WorkingDaysToCompletion = 10;

        private static DateTime Today => DateTime.UtcNow.Date;

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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.LastUpdated, Is.EqualTo(forecastCreationTime));
                Assert.That(subject.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            };
        }

        [Test]
        public void CreateFeatureDto_FeatureWithFutureBlackoutDays_ForecastPercentileDateStepsOverTheSpan()
        {
            var feature = ForecastedFeature();
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod
                {
                    Start = DateOnly.FromDateTime(Today.AddDays(3)),
                    End = DateOnly.FromDateTime(Today.AddDays(4)),
                },
            };

            var subject = new FeatureDto(feature, blackoutPeriods, false, null);

            var expectedDate = subject.Forecasts.Single(f => f.Probability == 85).ExpectedDate;
            Assert.That(expectedDate, Is.EqualTo(Today.AddDays(12)));
        }

        [Test]
        public void CreateFeatureDto_FeatureWithNoBlackoutPeriods_ForecastPercentileDateUnchanged()
        {
            var feature = ForecastedFeature();

            var subject = new FeatureDto(feature, Array.Empty<BlackoutPeriod>(), false, null);

            var expectedDate = subject.Forecasts.Single(f => f.Probability == 85).ExpectedDate;
            Assert.That(expectedDate, Is.EqualTo(Today.AddDays(WorkingDaysToCompletion)));
        }

        private static Feature ForecastedFeature()
        {
            var feature = new Feature();
            feature.SetFeatureForecasts([DeterministicForecast(WorkingDaysToCompletion)]);
            return feature;
        }

        private static WhenForecast DeterministicForecast(int workingDays)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[workingDays] = 100;
            return new WhenForecast(simulation) { HasSufficientData = true };
        }

        private FeatureDto CreateSubject(Feature feature)
        {
            return new FeatureDto(feature, Array.Empty<BlackoutPeriod>(), false, null);
        }
    }
}
