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
        [TestCase(OptionalFeatureKeys.LighthouseChartKey)]
        [TestCase(OptionalFeatureKeys.CycleTimeScatterPlotKey)]
        [TestCase(OptionalFeatureKeys.LinearIntegrationKey)]
        [TestCase(OptionalFeatureKeys.McpServerKey)]
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
        public async Task SeedAsync_CanBeCalledMultipleTimes_WithoutErrors()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();
            await subject.Seed();
            await subject.Seed();

            // Assert
            var features = DatabaseContext.OptionalFeatures.ToList();
            Assert.That(features, Has.Count.EqualTo(0));
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