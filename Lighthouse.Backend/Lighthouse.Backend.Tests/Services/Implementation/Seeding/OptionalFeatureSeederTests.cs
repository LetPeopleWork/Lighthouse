using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class OptionalFeatureSeederTests() : IntegrationTestBase
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Has.Count.EqualTo(1));
                Assert.That(features[0].Key, Is.EqualTo(OptionalFeatureKeys.DeltaSyncKey));
            }
        }

        [Test]
        public async Task SeedAsync_AddsDeltaSync_DisabledAndInPreview()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var deltaSync = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OptionalFeatureKeys.DeltaSyncKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deltaSync.Enabled, Is.False);
                Assert.That(deltaSync.IsPreview, Is.True);
                Assert.That(deltaSync.IsPremium, Is.False);
            }
        }

        [Test]
        public async Task SeedAsync_DeltaSyncEnabledByOperator_StaysEnabled()
        {
            var subject = CreateSubject();
            await subject.Seed();

            DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OptionalFeatureKeys.DeltaSyncKey).Enabled = true;
            await DatabaseContext.SaveChangesAsync();

            // Act
            await subject.Seed();

            // Assert
            var deltaSync = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OptionalFeatureKeys.DeltaSyncKey);
            Assert.That(deltaSync.Enabled, Is.True);
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

        [Test]
        public async Task SeedAsync_FeatureWasRenamedOrRedescribed_RefreshesTheTextWithoutTouchingTheOperatorsChoice()
        {
            // Arrange - an instance that already carries the row from an earlier release, switched on.
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 0,
                Key = OptionalFeatureKeys.DeltaSyncKey,
                Name = "An older name",
                Description = "An older description that named an internal work item.",
                Enabled = true,
                IsPreview = true
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var deltaSync = DatabaseContext.OptionalFeatures.Single(f => f.Key == OptionalFeatureKeys.DeltaSyncKey);

            Assert.That(deltaSync.Name, Is.EqualTo("Faster Updates"));
            Assert.That(deltaSync.Description, Does.Not.Contain("older description"));
            Assert.That(deltaSync.Enabled, Is.True, "An upgrade must not switch off something the operator turned on.");
        }

        private OptionalFeatureSeeder CreateSubject()
        {
            return new OptionalFeatureSeeder(DatabaseContext, Mock.Of<ILogger<OptionalFeatureSeeder>>());
        }
    }
}