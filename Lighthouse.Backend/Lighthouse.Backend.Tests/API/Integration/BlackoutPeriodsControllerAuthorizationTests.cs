using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;
using System.Net.Http.Json;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class BlackoutPeriodsControllerAuthorizationTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task GetAll_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/blackout-periods");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task Create_AsNonPremiumUser_Returns403()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 4, 10),
                End = new DateOnly(2026, 4, 15),
                Description = "Test"
            };

            var response = await Client.PostAsJsonAsync("/api/blackout-periods", dto);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task Update_AsNonPremiumUser_Returns403()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 5, 1),
                End = new DateOnly(2026, 5, 10),
                Description = "Test"
            };

            var response = await Client.PutAsJsonAsync("/api/blackout-periods/1", dto);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task Delete_AsNonPremiumUser_Returns403()
        {
            var response = await Client.DeleteAsync("/api/blackout-periods/1");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }
    }
}
