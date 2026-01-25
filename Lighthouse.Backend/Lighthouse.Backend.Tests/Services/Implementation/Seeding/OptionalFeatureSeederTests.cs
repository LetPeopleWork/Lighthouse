using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class OptionalFeatureSeederTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        [TestCase(OptionalFeatureKeys.McpServerKey, "MCP Server", false)]
        [TestCase(OptionalFeatureKeys.LinearIntegrationKey, "Linear Integration", true)]
        public async Task SeedAsync_AddsCurrentFeatures_WhenDatabaseIsEmpty(string key, string expectedName, bool expectedIsPreview)
        {
            var subject = CreateSubject();

            await subject.Seed();

            var feature = DatabaseContext.OptionalFeatures.Single(f => f.Key == key);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Name, Is.EqualTo(expectedName));
                Assert.That(feature.IsPreview, Is.EqualTo(expectedIsPreview));
                Assert.That(feature.Enabled, Is.False);
            }
        }

        [Test]
        public async Task SeedAsync_DoesNotDuplicate_WhenFeaturesAlreadyExist()
        {
            // Arrange
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 2,
                Key = OptionalFeatureKeys.McpServerKey,
                Name = "MCP Server",
                Description = "Custom description",
                Enabled = true, // User enabled it
                IsPreview = false
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var features = DatabaseContext.OptionalFeatures
                .Where(f => f.Key == OptionalFeatureKeys.McpServerKey)
                .ToList();

            Assert.That(features, Has.Count.EqualTo(1));
            Assert.That(features[0].Enabled, Is.True); // User's setting preserved
        }

        [Test]
        [TestCase(OptionalFeatureKeys.LighthouseChartKey)]
        [TestCase(OptionalFeatureKeys.CycleTimeScatterPlotKey)]
        public async Task SeedAsync_RemovesDeprecatedFeatures(string deprecatedKey)
        {
            // Arrange
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 0,
                Key = deprecatedKey,
                Name = "Deprecated Feature",
                Description = "Old feature",
                Enabled = false,
                IsPreview = false
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var deprecatedFeature = DatabaseContext.OptionalFeatures
                .FirstOrDefault(f => f.Key == deprecatedKey);

            Assert.That(deprecatedFeature, Is.Null);
        }

        [Test]
        public async Task SeedAsync_UpdatesIsPreviewFlag_WhenFeatureExists()
        {
            // Arrange
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 3,
                Key = OptionalFeatureKeys.LinearIntegrationKey,
                Name = "Linear Integration",
                Description = "Enables Experimental Support for Linear.app",
                Enabled = false,
                IsPreview = false // Old value
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var feature = DatabaseContext.OptionalFeatures
                .Single(f => f.Key == OptionalFeatureKeys.LinearIntegrationKey);

            Assert.That(feature.IsPreview, Is.True); // Updated to true
        }

        [Test]
        public async Task SeedAsync_CanBeCalledMultipleTimes_WithoutErrors()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();
            await subject.Seed();
            await subject.Seed();

            // Assert
            var features = DatabaseContext.OptionalFeatures.ToList();
            Assert.That(features, Has.Count.EqualTo(2)); // Should still be 2, not duplicated
        }

        [Test]
        public async Task SeedAsync_RemovesMultipleDeprecatedFeatures_InSingleOperation()
        {
            // Arrange
            var deprecatedKeys = new[]
            {
                OptionalFeatureKeys.LighthouseChartKey,
                OptionalFeatureKeys.CycleTimeScatterPlotKey
            };

            foreach (var key in deprecatedKeys)
            {
                DatabaseContext.OptionalFeatures.Add(new OptionalFeature
                {
                    Id = 12,
                    Key = key,
                    Name = $"Deprecated {key}",
                    Description = "Old",
                    Enabled = false,
                    IsPreview = false
                });
            }
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var remainingDeprecated = DatabaseContext.OptionalFeatures
                .Where(f => deprecatedKeys.Contains(f.Key))
                .ToList();

            Assert.That(remainingDeprecated, Is.Empty);
        }

        private OptionalFeatureSeeder CreateSubject()
        {
            return new OptionalFeatureSeeder(DatabaseContext, Mock.Of<ILogger<OptionalFeatureSeeder>>());
        }
    }
}