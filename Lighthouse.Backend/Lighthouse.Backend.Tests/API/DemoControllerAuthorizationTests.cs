using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;

namespace Lighthouse.Backend.Tests.API
{
    public class DemoControllerAuthorizationTests : IntegrationTestBase
    {
        public DemoControllerAuthorizationTests()
            : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task LoadAllScenarios_AsNonPremiumUser_Returns403()
        {
            var response = await Client.PostAsync("/api/demo/scenarios/load-all", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }
    }
}
