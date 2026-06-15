using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
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
        public async Task WhenForecast_WithEveryFourthFridayRule_NoPercentileDateLandsOnAFriday()
        {
            var unshiftedDate = await PercentileDate(85, remainingItems: 5);

            Client.AsSystemAdmin();
            var createResponse = await CreateOffSiteFridayRuleAnchoredOnNextFriday();
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await createResponse.Content.ReadAsStringAsync());

            var shiftedDate = await PercentileDate(85, remainingItems: 5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shiftedDate.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Friday), $"{shiftedDate:yyyy-MM-dd} is a Friday");
                Assert.That(shiftedDate, Is.GreaterThan(unshiftedDate),
                    $"interval rule did not step the forecast past any off-site Friday: shifted {shiftedDate:yyyy-MM-dd} vs unshifted {unshiftedDate:yyyy-MM-dd}");
            }
        }

        [Test]
        public async Task WhenForecast_IntervalOneWeekRuleReproducesPlainWeeklyBehaviour_NoPercentileDateLandsOnAWeekend()
        {
            var rule = new
            {
                weekdays = new[] { "Saturday", "Sunday" },
                intervalWeeks = 1,
                start = DateOnly.FromDateTime(Today).ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "Weekends via interval 1",
            };
            var unshiftedDate = await PercentileDate(85, remainingItems: 5);

            Client.AsSystemAdmin();
            var createResponse = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await createResponse.Content.ReadAsStringAsync());

            var shiftedDate = await PercentileDate(85, remainingItems: 5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shiftedDate.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Saturday), $"{shiftedDate:yyyy-MM-dd} is a Saturday");
                Assert.That(shiftedDate.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Sunday), $"{shiftedDate:yyyy-MM-dd} is a Sunday");
                Assert.That(shiftedDate, Is.GreaterThan(unshiftedDate),
                    $"interval-1 weekend rule did not reproduce plain weekly behaviour: shifted {shiftedDate:yyyy-MM-dd} vs unshifted {unshiftedDate:yyyy-MM-dd}");
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
