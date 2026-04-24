using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class ApiVersioningRoutingTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        private const string UnversionedUpdateSupportedPath = "/api/version/updateSupported";
        private const string V1UpdateSupportedPath = "/api/v1/version/updateSupported";
        private const string LatestUpdateSupportedPath = "/api/latest/version/updateSupported";

        [Test]
        public async Task GetUpdateSupported_OnV1Route_ReturnsSuccess()
        {
            var response = await Client.GetAsync(V1UpdateSupportedPath);

            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetUpdateSupported_OnLatestRoute_MatchesV1Contract()
        {
            var v1Response = await Client.GetAsync(V1UpdateSupportedPath);
            var v1Body = await v1Response.Content.ReadAsStringAsync();

            var latestResponse = await Client.GetAsync(LatestUpdateSupportedPath);
            var latestBody = await latestResponse.Content.ReadAsStringAsync();

            Assert.That(v1Response.IsSuccessStatusCode, Is.True);
            Assert.That(latestResponse.StatusCode, Is.EqualTo(v1Response.StatusCode));
            Assert.That(latestBody, Is.EqualTo(v1Body));
        }

        [Test]
        public async Task GetUpdateSupported_OnUnversionedRoute_DoesNotMatchVersionedContract()
        {
            var v1Response = await Client.GetAsync(V1UpdateSupportedPath);
            var v1Body = await v1Response.Content.ReadAsStringAsync();

            var unversionedResponse = await Client.GetAsync(UnversionedUpdateSupportedPath);
            var unversionedBody = await unversionedResponse.Content.ReadAsStringAsync();

            Assert.That(v1Response.IsSuccessStatusCode, Is.True);
            Assert.That(unversionedBody, Is.Not.EqualTo(v1Body));
        }
    }
}
