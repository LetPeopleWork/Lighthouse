using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class ForecastControllerAuthorizationTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task UpdateForecastForPortfolio_PortfolioRouteKey_DoesNotReturn500()
        {
            await SeedDatabase();

            var response = await Client.PostAsync("/api/latest/forecast/update/123", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.InternalServerError), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateForecastForPortfolio_PortfolioRouteKey_ReturnsOk()
        {
            await SeedDatabase();

            var response = await Client.PostAsync("/api/latest/forecast/update/123", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Response body: {body}");
        }
    }
}
