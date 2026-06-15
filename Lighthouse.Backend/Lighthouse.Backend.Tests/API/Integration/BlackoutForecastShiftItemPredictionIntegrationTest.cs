using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class BlackoutForecastShiftItemPredictionIntegrationTest : BlackoutForecastShiftTestBase
    {
        [SetUp]
        public void Init()
        {
            StartApplicationWithDeterministicForecast();
            StubHowManyEchoingTheDayCount();
        }

        [TearDown]
        public void Cleanup()
        {
            StopApplication();
        }

        [Test]
        public async Task RunManualForecast_TargetDateSpanningTwoBlackoutDays_ScoresHowManyOnTheWorkingDayCount()
        {
            ConfigureBlackoutPeriod(
                DateOnly.FromDateTime(Today.AddDays(3)),
                DateOnly.FromDateTime(Today.AddDays(4)));

            using var document = await RunManualForecastWithTarget(remainingItems: 5, targetDate: Today.AddDays(12));

            var likeliestCount = HowManyValueForProbability(document, 50);
            Assert.That(likeliestCount, Is.EqualTo(10));
        }

        [Test]
        public async Task RunManualForecast_TargetDateSpanningTwoBlackoutDays_ScoresLikelihoodOnTheWorkingDayCount()
        {
            ConfigureBlackoutPeriod(
                DateOnly.FromDateTime(Today.AddDays(3)),
                DateOnly.FromDateTime(Today.AddDays(4)));

            using var document = await RunManualForecastWithTarget(remainingItems: 5, targetDate: Today.AddDays(12));

            var likelihood = document.RootElement.GetProperty("likelihood").GetDouble();
            Assert.That(likelihood, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public async Task RunManualForecast_NoBlackoutDaysInWindow_WorkingDayCountEqualsCalendarDayCount()
        {
            using var document = await RunManualForecastWithTarget(remainingItems: 5, targetDate: Today.AddDays(12));

            var likeliestCount = HowManyValueForProbability(document, 50);
            Assert.That(likeliestCount, Is.EqualTo(12));
        }

        [Test]
        public async Task RunManualForecast_TargetDateInThePast_KeepsExistingGuardAndReturnsNoHowMany()
        {
            ConfigureBlackoutPeriod(
                DateOnly.FromDateTime(Today.AddDays(3)),
                DateOnly.FromDateTime(Today.AddDays(4)));

            using var document = await RunManualForecastWithTarget(remainingItems: 5, targetDate: Today.AddDays(-3));

            Assert.That(document.RootElement.GetProperty("howManyForecasts").GetArrayLength(), Is.Zero);
        }

        private static int HowManyValueForProbability(JsonDocument document, int probability)
        {
            return document.RootElement.GetProperty("howManyForecasts")
                .EnumerateArray()
                .Single(p => p.GetProperty("probability").GetInt32() == probability)
                .GetProperty("value").GetInt32();
        }

        private void StubHowManyEchoingTheDayCount()
        {
            ForecastServiceMock
                .Setup(s => s.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns((RunChartData _, int days) =>
                {
                    var simulation = new Dictionary<int, int> { [days] = 100 };
                    return new HowManyForecast(simulation, days);
                });
        }

        private async Task<JsonDocument> RunManualForecastWithTarget(int remainingItems, DateTime targetDate)
        {
            Client.AsTeamViewer(SeededTeamId);

            var input = new { RemainingItems = remainingItems, TargetDate = targetDate };
            var response = await Client.PostAsJsonAsync($"/api/latest/forecast/manual/{SeededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return JsonDocument.Parse(body);
        }
    }
}
