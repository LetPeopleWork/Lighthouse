using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class BlackoutForecastShiftTeamForecastIntegrationTest : BlackoutForecastShiftTestBase
    {
        [SetUp]
        public void Init()
        {
            StartApplicationWithDeterministicForecast();
        }

        [TearDown]
        public void Cleanup()
        {
            StopApplication();
        }

        [Test]
        public async Task RunManualWhenForecast_TeamWithTwoBlackoutDaysInWindow_PercentileDateStepsOverTheBlackoutSpan()
        {
            ConfigureBlackoutPeriod(
                DateOnly.FromDateTime(Today.AddDays(3)),
                DateOnly.FromDateTime(Today.AddDays(4)));

            var expectedDate = await ExpectedDateForPercentile(85, remainingItems: 5);

            Assert.That(expectedDate, Is.EqualTo(Today.AddDays(12)));
        }

        [Test]
        public async Task RunManualWhenForecast_PercentileDateLandsOnBlackoutDay_RollsForwardToNextWorkingDay()
        {
            ConfigureBlackoutPeriod(
                DateOnly.FromDateTime(Today.AddDays(WorkingDaysAtAllPercentiles)),
                DateOnly.FromDateTime(Today.AddDays(WorkingDaysAtAllPercentiles)));

            var expectedDate = await ExpectedDateForPercentile(85, remainingItems: 5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(expectedDate, Is.Not.EqualTo(Today.AddDays(WorkingDaysAtAllPercentiles)));
                Assert.That(expectedDate, Is.EqualTo(Today.AddDays(WorkingDaysAtAllPercentiles + 1)));
            }
        }

        [Test]
        public async Task RunManualWhenForecast_PercentileDateLandsOnFirstDayOfConsecutiveBlackoutSpan_RollsForwardPastTheWholeSpan()
        {
            const int consecutiveBlackoutDays = 4;
            ConfigureBlackoutPeriod(
                DateOnly.FromDateTime(Today.AddDays(WorkingDaysAtAllPercentiles)),
                DateOnly.FromDateTime(Today.AddDays(WorkingDaysAtAllPercentiles + consecutiveBlackoutDays - 1)));

            var expectedDate = await ExpectedDateForPercentile(85, remainingItems: 5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(expectedDate, Is.Not.EqualTo(Today.AddDays(WorkingDaysAtAllPercentiles + 1)));
                Assert.That(expectedDate, Is.EqualTo(Today.AddDays(WorkingDaysAtAllPercentiles + consecutiveBlackoutDays)));
            }
        }

        [Test]
        public async Task RunManualWhenForecast_TeamWithNoBlackoutPeriods_PercentileDateEqualsTodayPlusDays()
        {
            var expectedDate = await ExpectedDateForPercentile(85, remainingItems: 5);

            Assert.That(expectedDate, Is.EqualTo(Today.AddDays(WorkingDaysAtAllPercentiles)));
        }

        private async Task<DateTime> ExpectedDateForPercentile(int probability, int remainingItems)
        {
            Client.AsTeamViewer(SeededTeamId);

            var input = new { RemainingItems = remainingItems };
            var response = await Client.PostAsJsonAsync($"/api/latest/forecast/manual/{SeededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var percentile = document.RootElement.GetProperty("whenForecasts")
                .EnumerateArray()
                .Single(p => p.GetProperty("probability").GetInt32() == probability);

            return percentile.GetProperty("expectedDate").GetDateTime();
        }
    }
}
