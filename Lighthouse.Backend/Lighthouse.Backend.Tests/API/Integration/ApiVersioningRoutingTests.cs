using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class ApiVersioningRoutingTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        private const string V1UpdateSupportedPath = "/api/v1/version/updateSupported";
        private const string LatestUpdateSupportedPath = "/api/latest/version/updateSupported";
        private const string V1WorkTrackingConnectionPath = "/api/v1/worktrackingsystemconnections/1";
        private const string LatestWorkTrackingConnectionPath = "/api/latest/worktrackingsystemconnections/1";

        [Test]
        public async Task GetUpdateSupported_OnV1Route_ReturnsSuccess()
        {
            var response = await Client.GetAsync(V1UpdateSupportedPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.IsSuccessStatusCode, Is.True);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task GetUpdateSupported_OnLatestRoute_MatchesV1Contract()
        {
            var v1Response = await Client.GetAsync(V1UpdateSupportedPath);
            var v1Body = await v1Response.Content.ReadAsStringAsync();

            var latestResponse = await Client.GetAsync(LatestUpdateSupportedPath);
            var latestBody = await latestResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(v1Response.IsSuccessStatusCode, Is.True);
                Assert.That(latestResponse.StatusCode, Is.EqualTo(v1Response.StatusCode));
                Assert.That(latestBody, Is.EqualTo(v1Body));
            }
        }

        [Test]
        public async Task GetWorkTrackingConnection_OnV1Route_ReturnsSuccess()
        {
            await SeedWorkTrackingConnection();

            var response = await Client.GetAsync(V1WorkTrackingConnectionPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.IsSuccessStatusCode, Is.True);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task GetWorkTrackingConnection_OnLatestRoute_MatchesV1Contract()
        {
            await SeedWorkTrackingConnection();

            var v1Response = await Client.GetAsync(V1WorkTrackingConnectionPath);
            var v1Body = await v1Response.Content.ReadAsStringAsync();

            var latestResponse = await Client.GetAsync(LatestWorkTrackingConnectionPath);
            var latestBody = await latestResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(v1Response.IsSuccessStatusCode, Is.True);
                Assert.That(latestResponse.StatusCode, Is.EqualTo(v1Response.StatusCode));
                Assert.That(latestBody, Is.EqualTo(v1Body));
            }
        }

        private async Task SeedWorkTrackingConnection()
        {
            DatabaseContext.WorkTrackingSystemConnections.Add(new WorkTrackingSystemConnection
            {
                Id = 1,
                Name = "Connection One",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = "pat",
            });
            await DatabaseContext.SaveChangesAsync();
        }
    }
}
