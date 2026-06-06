using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    [Category("recurring-blackout-events")]
    public class RecurringBlackoutRulesIntervalRuleIntegrationTest : RecurringBlackoutRulesTestBase
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
        [Ignore("pending DELIVER — US-02")]
        public async Task CreateEveryFourthFridayRule_AsPremiumSystemAdmin_RuleIsAcceptedAndListed()
        {
            Client.AsSystemAdmin();

            var createResponse = await CreateOffSiteFridayRule();
            var createBody = await createResponse.Content.ReadAsStringAsync();
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), createBody);

            Client.AsViewer();
            var listResponse = await Client.GetAsync("/api/latest/recurring-blackout-rules");
            var listBody = await listResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), listBody);
                Assert.That(listBody, Does.Contain("Friday").Or.Contain("Fri"), listBody);
            }
        }

        [Test]
        [Ignore("pending DELIVER — US-02")]
        public async Task WhenForecast_WithEveryFourthFridayRule_NoPercentileDateLandsOnAFriday()
        {
            Client.AsSystemAdmin();
            var createResponse = await CreateOffSiteFridayRuleAnchoredOnNextFriday();
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await createResponse.Content.ReadAsStringAsync());

            var matchedFriday = await PercentileDate(85, remainingItems: 5);

            Assert.That(matchedFriday.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Friday), $"{matchedFriday:yyyy-MM-dd} is a Friday");
        }

        [Test]
        [Ignore("pending DELIVER — US-02")]
        public async Task WhenForecast_IntervalOneWeekRuleReproducesPlainWeeklyBehaviour_NoPercentileDateLandsOnAWeekend()
        {
            Client.AsSystemAdmin();
            var rule = new
            {
                weekdays = new[] { "Saturday", "Sunday" },
                intervalWeeks = 1,
                start = DateOnly.FromDateTime(Today).ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "Weekends via interval 1",
            };
            var createResponse = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await createResponse.Content.ReadAsStringAsync());

            var expectedDate = await PercentileDate(85, remainingItems: 5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(expectedDate.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Saturday), $"{expectedDate:yyyy-MM-dd} is a Saturday");
                Assert.That(expectedDate.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Sunday), $"{expectedDate:yyyy-MM-dd} is a Sunday");
            }
        }

        private async Task<HttpResponseMessage> CreateOffSiteFridayRule()
        {
            var rule = new
            {
                weekdays = new[] { "Friday" },
                intervalWeeks = 4,
                start = "2026-06-12",
                end = "2026-12-31",
                description = "Team off-site",
            };

            return await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
        }

        private async Task<HttpResponseMessage> CreateOffSiteFridayRuleAnchoredOnNextFriday()
        {
            var anchor = NextFridayOnOrAfter(DateOnly.FromDateTime(Today));
            var rule = new
            {
                weekdays = new[] { "Friday" },
                intervalWeeks = 1,
                start = anchor.ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "Every Friday off-site",
            };

            return await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
        }

        private static DateOnly NextFridayOnOrAfter(DateOnly date)
        {
            var offset = ((int)DayOfWeek.Friday - (int)date.DayOfWeek + 7) % 7;
            return date.AddDays(offset);
        }

        private async Task<DateTime> PercentileDate(int probability, int remainingItems)
        {
            Client.AsTeamViewer(SeededTeamId);

            var input = new { RemainingItems = remainingItems };
            var response = await Client.PostAsJsonAsync($"/api/latest/forecast/manual/{SeededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("whenForecasts")
                .EnumerateArray()
                .Single(p => p.GetProperty("probability").GetInt32() == probability)
                .GetProperty("expectedDate").GetDateTime();
        }
    }
}
