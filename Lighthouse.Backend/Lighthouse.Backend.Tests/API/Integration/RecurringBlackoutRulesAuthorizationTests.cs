using System.Net;
using System.Net.Http.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("recurring-blackout-events")]
    public class RecurringBlackoutRulesAuthorizationTests() : IntegrationTestBase
    {
        [Test]
        public async Task GetAll_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/latest/recurring-blackout-rules");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task Create_AsNonPremiumUser_Returns403()
        {
            var rule = new
            {
                weekdays = new[] { "Saturday", "Sunday" },
                intervalWeeks = 1,
                start = "2026-06-06",
                end = (string?)null,
                description = "Weekends",
            };

            var response = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task Update_AsNonPremiumUser_Returns403()
        {
            var rule = new
            {
                weekdays = new[] { "Friday" },
                intervalWeeks = 4,
                start = "2026-06-12",
                end = "2026-12-31",
                description = "Off-site",
            };

            var response = await Client.PutAsJsonAsync("/api/latest/recurring-blackout-rules/1", rule);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task Delete_AsNonPremiumUser_Returns403()
        {
            var response = await Client.DeleteAsync("/api/latest/recurring-blackout-rules/1");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }
    }
}
