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
    public class RecurringBlackoutRulesWeekendsForeverIntegrationTest : RecurringBlackoutRulesTestBase
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
        public async Task CreateWeekendsForeverRule_AsPremiumSystemAdmin_RuleIsAcceptedAndListedWithHumanReadableSummary()
        {
            Client.AsSystemAdmin();

            var createResponse = await CreateWeekendsForeverRule();
            var createBody = await createResponse.Content.ReadAsStringAsync();
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), createBody);

            Client.AsViewer();
            var listResponse = await Client.GetAsync("/api/latest/recurring-blackout-rules");
            var listBody = await listResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), listBody);
                Assert.That(listBody, Does.Contain("Saturday").Or.Contain("Sat"), listBody);
                Assert.That(listBody, Does.Contain("Sunday").Or.Contain("Sun"), listBody);
            }
        }

        [Test]
        [Ignore("pending DELIVER — US-01")]
        public async Task WhenForecast_WithWeekendsForeverRule_NoPercentileDateLandsOnAWeekend()
        {
            Client.AsSystemAdmin();
            var createResponse = await CreateWeekendsForeverRule();
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await createResponse.Content.ReadAsStringAsync());

            var percentileDates = await AllPercentileDates(remainingItems: 5);

            Assert.That(percentileDates, Is.Not.Empty);
            using (Assert.EnterMultipleScope())
            {
                foreach (var date in percentileDates)
                {
                    Assert.That(date.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Saturday), $"{date:yyyy-MM-dd} is a Saturday");
                    Assert.That(date.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Sunday), $"{date:yyyy-MM-dd} is a Sunday");
                }
            }
        }

        private async Task<HttpResponseMessage> CreateWeekendsForeverRule()
        {
            var rule = new
            {
                weekdays = new[] { "Saturday", "Sunday" },
                intervalWeeks = 1,
                start = DateOnly.FromDateTime(Today).ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "Weekends",
            };

            return await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
        }

        private async Task<IReadOnlyList<DateTime>> AllPercentileDates(int remainingItems)
        {
            Client.AsTeamViewer(SeededTeamId);

            var input = new { RemainingItems = remainingItems };
            var response = await Client.PostAsJsonAsync($"/api/latest/forecast/manual/{SeededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("whenForecasts")
                .EnumerateArray()
                .Select(p => p.GetProperty("expectedDate").GetDateTime())
                .ToList();
        }
    }
}
