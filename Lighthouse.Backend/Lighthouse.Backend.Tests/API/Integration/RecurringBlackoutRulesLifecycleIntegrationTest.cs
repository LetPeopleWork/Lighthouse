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
    public class RecurringBlackoutRulesLifecycleIntegrationTest : RecurringBlackoutRulesTestBase
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
        [Ignore("pending DELIVER — US-04")]
        public async Task EditRule_ChangeWeekendsToFridayOnly_ListReflectsTheNewWeekday()
        {
            Client.AsSystemAdmin();
            var ruleId = await CreateWeekendsForeverRuleReturningId();

            var edited = new
            {
                weekdays = new[] { "Friday" },
                intervalWeeks = 1,
                start = DateOnly.FromDateTime(Today).ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "Fridays only",
            };
            var updateResponse = await Client.PutAsJsonAsync($"/api/latest/recurring-blackout-rules/{ruleId}", edited);
            Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await updateResponse.Content.ReadAsStringAsync());

            Client.AsViewer();
            var listBody = await (await Client.GetAsync("/api/latest/recurring-blackout-rules")).Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(listBody, Does.Contain("Friday").Or.Contain("Fri"), listBody);
                Assert.That(listBody, Does.Not.Contain("Saturday"), listBody);
            }
        }

        [Test]
        [Ignore("pending DELIVER — US-04")]
        public async Task DeleteRule_RemovesItFromTheList()
        {
            Client.AsSystemAdmin();
            var ruleId = await CreateWeekendsForeverRuleReturningId();

            var deleteResponse = await Client.DeleteAsync($"/api/latest/recurring-blackout-rules/{ruleId}");
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                await deleteResponse.Content.ReadAsStringAsync());

            Client.AsViewer();
            var listResponse = await Client.GetAsync("/api/latest/recurring-blackout-rules");
            var listBody = await listResponse.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(listBody);
            Assert.That(document.RootElement.GetArrayLength(), Is.Zero, listBody);
        }

        [Test]
        [Ignore("pending DELIVER — US-04")]
        public async Task DeleteRule_UnknownId_Returns404()
        {
            Client.AsSystemAdmin();

            var response = await Client.DeleteAsync("/api/latest/recurring-blackout-rules/999999");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), body);
        }

        [Test]
        [Ignore("pending DELIVER — US-04")]
        public async Task CreateRule_WithNoWeekdays_RejectedWithWeekdayRequiredMessage()
        {
            Client.AsSystemAdmin();
            var rule = new
            {
                weekdays = Array.Empty<string>(),
                intervalWeeks = 1,
                start = DateOnly.FromDateTime(Today).ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "No weekday",
            };

            var response = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("Select at least one weekday for the rule to repeat on."), body);
            }
        }

        [Test]
        [Ignore("pending DELIVER — US-04")]
        public async Task CreateRule_WithIntervalBelowOne_RejectedWithIntervalMessage()
        {
            Client.AsSystemAdmin();
            var rule = new
            {
                weekdays = new[] { "Friday" },
                intervalWeeks = 0,
                start = DateOnly.FromDateTime(Today).ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "Bad interval",
            };

            var response = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("Repeat interval must be at least 1 week."), body);
            }
        }

        [Test]
        [Ignore("pending DELIVER — US-04")]
        public async Task CreateRule_WithEndBeforeStart_RejectedWithDateRangeMessage()
        {
            Client.AsSystemAdmin();
            var rule = new
            {
                weekdays = new[] { "Friday" },
                intervalWeeks = 1,
                start = "2026-12-31",
                end = "2026-06-12",
                description = "End before start",
            };

            var response = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("End date must be on or after the start date."), body);
            }
        }

        private async Task<int> CreateWeekendsForeverRuleReturningId()
        {
            var rule = new
            {
                weekdays = new[] { "Saturday", "Sunday" },
                intervalWeeks = 1,
                start = DateOnly.FromDateTime(Today).ToString("yyyy-MM-dd"),
                end = (string?)null,
                description = "Weekends",
            };

            var response = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), body);

            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("id").GetInt32();
        }
    }
}
