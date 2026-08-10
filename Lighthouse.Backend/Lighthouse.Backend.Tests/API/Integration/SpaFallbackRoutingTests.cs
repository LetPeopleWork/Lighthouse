using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lighthouse.Backend.Tests.API.Integration
{
    // Bug #5732: the SPA fallback answered unmatched requests with index.html and a 200,
    // so a stale client saw HTML where it expected JSON and the browser could never
    // replace a removed service worker script.
    public class SpaFallbackRoutingTests() : IntegrationTestBase(new SpaFallbackWebApplicationFactory())
    {
        private const string SpaShellMediaType = "text/html";

        [TestCase("/api/v1/this-route-does-not-exist")]
        [TestCase("/api/latest/this-route-does-not-exist")]
        [TestCase("/api/this-route-does-not-exist")]
        public async Task UnknownApiRoute_ReturnsNotFound_WithoutServingSpaShell(string path)
        {
            var response = await Client.GetAsync(path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo(SpaShellMediaType));
            }
        }

        [TestCase("/sw.js")]
        [TestCase("/registerSW.js")]
        [TestCase("/assets/index-DqfqphaE.js")]
        [TestCase("/manifest.webmanifest")]
        public async Task MissingStaticAsset_ReturnsNotFound_WithoutServingSpaShell(string path)
        {
            var response = await Client.GetAsync(path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo(SpaShellMediaType));
            }
        }

        [TestCase("/")]
        [TestCase("/teams/1")]
        public async Task SpaShell_IsServedWithoutCaching_OnEveryRoute(string path)
        {
            var response = await Client.GetAsync(path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(SpaShellMediaType));
                Assert.That(response.Headers.CacheControl?.NoStore, Is.True);
            }
        }

        [Test]
        public async Task KnownApiRoute_StillReachesItsController()
        {
            var response = await Client.GetAsync("/api/latest/version/updateSupported");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.IsSuccessStatusCode, Is.True);
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo(SpaShellMediaType));
            }
        }

        [Test]
        public async Task NotificationHub_IsNotSwallowedByTheApiGuard()
        {
            var response = await Client.GetAsync("/api/updateNotificationHub");

            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.NotFound));
        }
    }

    public sealed class SpaFallbackWebApplicationFactory : TestWebApplicationFactory<Program>
    {
        private readonly string webRootPath = Path.Combine(Path.GetTempPath(), $"lighthouse-spa-{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            Directory.CreateDirectory(webRootPath);
            File.WriteAllText(
                Path.Combine(webRootPath, "index.html"),
                "<!doctype html><html lang=\"en\"><body><div id=\"root\"></div></body></html>");

            builder.UseWebRoot(webRootPath);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (Directory.Exists(webRootPath))
            {
                Directory.Delete(webRootPath, true);
            }
        }
    }
}
