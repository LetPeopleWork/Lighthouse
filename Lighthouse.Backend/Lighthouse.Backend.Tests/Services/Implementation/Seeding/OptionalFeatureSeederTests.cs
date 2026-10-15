using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class OptionalFeatureSeederTests() : IntegrationTestBase
    {
        private static readonly string[] TheSettingsTheProductSeeds =
        [
            OptionalFeatureKeys.FeatureOrderingKey,
            OptionalFeatureKeys.UsageDataKey,
        ];

        /// <summary>
        /// The key of the history fill setting, which no longer exists. Written out rather than taken from
        /// the product's constant because an instance upgrading into this release carries this exact string,
        /// so renaming the constant's value must fail these tests, not silently rename what they check.
        /// </summary>
        private const string OverTimeHistoryFillKey = "OverTimeHistoryFill";

        // A retired row goes whichever way the operator left it: once the switch is gone there is no
        // choice left to preserve.
        [Test]
        [TestCase(OptionalFeatureKeys.LighthouseChartKey, false)]
        [TestCase(OptionalFeatureKeys.CycleTimeScatterPlotKey, false)]
        [TestCase(OptionalFeatureKeys.LinearIntegrationKey, false)]
        [TestCase(OptionalFeatureKeys.McpServerKey, false)]
        [TestCase(OptionalFeatureKeys.DeltaSyncKey, true)]
        [TestCase(OptionalFeatureKeys.DeltaSyncKey, false)]
        [TestCase(OverTimeHistoryFillKey, true)]
        [TestCase(OverTimeHistoryFillKey, false)]
        public async Task SeedAsync_RemovesDeprecatedFeatures(string deprecatedKey, bool leftEnabled)
        {
            // Arrange
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 0,
                Key = deprecatedKey,
                Name = "Deprecated Feature",
                Description = "Old feature",
                Enabled = leftEnabled,
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
        public async Task SeedAsync_OnAnEmptyDatabase_NeverAddsTheRetiredFasterUpdatesSwitch()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            Assert.That(DatabaseContext.OptionalFeatures.Any(f => f.Key == OptionalFeatureKeys.DeltaSyncKey), Is.False);
        }

        // The fill always runs, so a new instance has neither the setting nor any record of having switched it.
        [Test]
        public async Task SeedAsync_OnAnEmptyDatabase_NeverAddsTheHistoryFillNorARecordAboutIt()
        {
            var subject = CreateSubject();

            await subject.Seed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DatabaseContext.OptionalFeatures.Any(feature => feature.Key == OverTimeHistoryFillKey), Is.False,
                    "A new instance was given a history fill setting, though there is nothing left to switch.");
                Assert.That(DatabaseContext.AppSettings.Any(setting => setting.Key == "OptionalFeatures:HistoryFillSwitchedOnByDefault"), Is.False,
                    "A new instance was given a record of switching the history fill on, which nothing reads any more.");
            }
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

            Assert.That(features.Select(feature => feature.Key), Is.EquivalentTo(TheSettingsTheProductSeeds));
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
                Key = OptionalFeatureKeys.UsageDataKey,
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
            var usageData = DatabaseContext.OptionalFeatures.Single(f => f.Key == OptionalFeatureKeys.UsageDataKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(usageData.Name, Is.EqualTo("Never send usage data"));
                Assert.That(usageData.Description, Does.Not.Contain("older description"));
                Assert.That(usageData.IsPreview, Is.False);
                Assert.That(usageData.Enabled, Is.True, "An upgrade must not switch off something the operator turned on.");
            }
        }

        [Test]
        [TestCase("ManualOrder", true)]
        [TestCase("SourceOrder", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("Nonsense", false)]
        public async Task SeedAsync_AddsFeatureOrdering_CarryingAcrossWhatTheInstanceHadAlreadyChosen(string? storedPolicy, bool expectedToBeOn)
        {
            // Arrange - the instance as it stood before the setting joined the table: the choice lived in
            // an app setting, and only the one word meant this instance had taken the order over.
            if (storedPolicy != null)
            {
                DatabaseContext.AppSettings.Add(new AppSetting { Key = AppSettingKeys.FeatureOrderingPolicy, Value = storedPolicy });
                await DatabaseContext.SaveChangesAsync();
            }

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var featureOrdering = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OptionalFeatureKeys.FeatureOrderingKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureOrdering.Enabled, Is.EqualTo(expectedToBeOn));
                Assert.That(featureOrdering.IsPremium, Is.True);
                Assert.That(featureOrdering.IsPreview, Is.False);
            }
        }

        [Test]
        public async Task SeedAsync_FeatureOrderingSwitchedOnSinceTheUpgrade_KeepsTheInstancesOwnAnswer()
        {
            DatabaseContext.AppSettings.Add(new AppSetting { Key = AppSettingKeys.FeatureOrderingPolicy, Value = nameof(FeatureOrderingPolicy.SourceOrder) });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();
            await subject.Seed();

            DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OptionalFeatureKeys.FeatureOrderingKey).Enabled = true;
            await DatabaseContext.SaveChangesAsync();

            // Act
            await subject.Seed();

            // Assert
            var featureOrdering = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OptionalFeatureKeys.FeatureOrderingKey);
            Assert.That(featureOrdering.Enabled, Is.True, "The carry-across happens once, when the row is added. After that the switch belongs to whoever flipped it.");
        }

        [Test]
        public async Task SeedAsync_FeatureOrdering_ReadsTheWayAnAdministratorSeesIt()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert - spelled out rather than read off the seeder, because a test that compares a value
            // to the constant it came from passes even when the words are blanked. These are the words an
            // administrator reads, and the docs fold the same help text in.
            var featureOrdering = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OptionalFeatureKeys.FeatureOrderingKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureOrdering.Name, Is.EqualTo("Let Lighthouse own the order of your {{features}}"));
                Assert.That(featureOrdering.Description, Is.EqualTo("While this is on, Lighthouse forecasts your {{features}} in the order you gave them, and a refresh from your work tracking system no longer re-sequences it. Turning it off hands the order straight back to your work tracking system — the places you chose are kept, so turning it on again restores them."));
            }
        }

        private OptionalFeatureSeeder CreateSubject()
        {
            return new OptionalFeatureSeeder(DatabaseContext, Mock.Of<ILogger<OptionalFeatureSeeder>>());
        }
    }
}