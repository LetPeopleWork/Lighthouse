using Lighthouse.Backend.MCP;

namespace Lighthouse.Backend.Tests.MCP
{
    public class LighthouseResourcesTest : LighthosueToolsBaseTest
    {
        [Test]
        public async Task ListDocumentationResources_ShouldReturnExpectedResources()
        {
            // Arrange
            var lighthouseResources = new LighthouseResources(ServiceScopeFactory);

            // Act
            var result = await lighthouseResources.ListDocumentationResources();


            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Resources, Is.Not.Null);
                Assert.That(result.Resources, Has.Count.EqualTo(7));

                // Verify specific resources
                var mainDoc = result.Resources.FirstOrDefault(r => r.Name == "Lighthouse Documentation");
                Assert.That(mainDoc, Is.Not.Null);
                Assert.That(mainDoc.Uri, Is.EqualTo("https://docs.lighthouse.letpeople.work/"));
                Assert.That(mainDoc.MimeType, Is.EqualTo("text/html"));

                var concepts = result.Resources.FirstOrDefault(r => r.Name == "Concepts");
                Assert.That(concepts, Is.Not.Null);
                Assert.That(concepts.Uri, Is.EqualTo("https://docs.lighthouse.letpeople.work/concepts/concepts.html"));

                var teams = result.Resources.FirstOrDefault(r => r.Name == "Teams");
                Assert.That(teams, Is.Not.Null);
                Assert.That(teams.Uri, Is.EqualTo("https://docs.lighthouse.letpeople.work/teams/teams.html"));

                var projects = result.Resources.FirstOrDefault(r => r.Name == "Projects");
                Assert.That(projects, Is.Not.Null);
                Assert.That(projects.Uri, Is.EqualTo("https://docs.lighthouse.letpeople.work/projects/projects.html"));

                var metrics = result.Resources.FirstOrDefault(r => r.Name == "Metrics");
                Assert.That(metrics, Is.Not.Null);
                Assert.That(metrics.Uri, Is.EqualTo("https://docs.lighthouse.letpeople.work/metrics/metrics.html"));

                var aiIntegration = result.Resources.FirstOrDefault(r => r.Name == "AI Integration");
                Assert.That(aiIntegration, Is.Not.Null);
                Assert.That(aiIntegration.Uri, Is.EqualTo("https://docs.lighthouse.letpeople.work/aiintegration.html"));

                var forecasting = result.Resources.FirstOrDefault(r => r.Name == "How Lighthouse Forecasts");
                Assert.That(forecasting, Is.Not.Null);
                Assert.That(forecasting.Uri, Is.EqualTo("https://docs.lighthouse.letpeople.work/concepts/howlighthouseforecasts.html"));
            }
        }

        [Test]
        public void LighthouseResources_ShouldImplementIDisposable()
        {
            // Arrange & Act
            var lighthouseResources = new LighthouseResources(ServiceScopeFactory);

            // Assert - Should not throw when disposing
            Assert.DoesNotThrow(() => lighthouseResources.Dispose());
        }
    }
}
// ============================================================================