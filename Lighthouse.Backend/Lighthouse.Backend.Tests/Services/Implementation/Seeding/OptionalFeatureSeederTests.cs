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
            OptionalFeatureKeys.OverTimeHistoryFillKey,
        ];

        /// <summary>
        /// The switch that decides whether this instance fills in past days on the over-time charts, spelled
        /// as a caller addresses it. Written out rather than taken from the product's constant because this
        /// is the wire identity a script switching it from outside uses: renaming the constant's value must
        /// fail these tests, not silently rename what they check.
        /// </summary>
        private const string OverTimeHistoryFillKey = "OverTimeHistoryFill";

        [Test]
        public async Task SeedAsync_AddsTheOverTimeHistoryFill_OnInPreviewAndFree_AndRecordsThatItSwitchedItOn()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var fill = DatabaseContext.OptionalFeatures.SingleOrDefault(feature => feature.Key == OverTimeHistoryFillKey);

            Assert.That(fill, Is.Not.Null, "A fresh instance offers no switch for filling in past days, so no administrator can ever turn it off.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fill!.Enabled, Is.True, "The fill is on unless an administrator switches it off, so nobody has to find the switch before the charts fill in.");
                Assert.That(fill.IsPreview, Is.True, "The fill is still offered as a preview and may still change; the list says so beside the switch.");
                Assert.That(fill.IsPremium, Is.False, "The over-time charts are free, so the only way to fill them in cannot sit behind a licence.");
                Assert.That(DatabaseContext.AppSettings.Any(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault), Is.True,
                    "A fresh instance must record the switch-on too, or the next start-up treats it as an upgrade and undoes an administrator's off.");
            }
        }

        // The same seeder, the same context, twice: what a second start-up in one process looks like. It must
        // see the record the first run wrote rather than adding it again or flipping the fill a second time.
        [Test]
        public async Task SeedAsync_RunAgainInTheSameProcessAfterTheAdministratorSwitchedTheFillOff_LeavesItOff()
        {
            // Arrange - the fill as a release from before the switch-on left it: stored off, nothing recorded.
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 0,
                Key = OverTimeHistoryFillKey,
                Name = "Fill in past days on over-time charts",
                Description = "An earlier description.",
                Enabled = false,
                IsPreview = true,
                IsPremium = false,
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();
            await subject.Seed();

            var fill = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OverTimeHistoryFillKey);
            Assert.That(fill.Enabled, Is.True, "The first start-up of this release should switch the fill on, so there is no administrator's off for the second run to keep.");

            fill.Enabled = false;
            await DatabaseContext.SaveChangesAsync();

            // Act
            await subject.Seed();

            // Assert
            var afterTheSecondRun = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OverTimeHistoryFillKey);
            var recordsOfTheSwitchOn = DatabaseContext.AppSettings.Count(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheSecondRun.Enabled, Is.False, "The switch-on happens once; the administrator's off has to hold.");
                Assert.That(recordsOfTheSwitchOn, Is.EqualTo(1), "The switch-on is recorded once, not once per run.");
            }
        }

        [Test]
        public async Task SeedAsync_OverTimeHistoryFillSwitchedOnBeforeTheUpgrade_StaysOnAndIsRedescribed()
        {
            // Arrange - an instance whose administrator already opted in, carrying an older wording and flags.
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 0,
                Key = OverTimeHistoryFillKey,
                Name = "An older name",
                Description = "An older description.",
                Enabled = true,
                IsPreview = false,
                IsPremium = true,
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var fill = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OverTimeHistoryFillKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fill.Enabled, Is.True, "An upgrade must never switch off a fill an administrator switched on.");
                Assert.That(fill.Name, Is.EqualTo("Fill in past days on over-time charts"), "How the setting presents itself is ours and is refreshed on every upgrade.");
                Assert.That(fill.IsPreview, Is.True, "The preview flag is ours and is refreshed on every upgrade.");
                Assert.That(fill.IsPremium, Is.False, "The premium flag is ours and is refreshed on every upgrade.");
            }
        }

        // Spelled out rather than read off the seeder, because comparing a value to the constant it came from
        // passes even when the words are blanked. These are the words an administrator decides on.
        [Test]
        public async Task SeedAsync_OverTimeHistoryFill_ReadsTheWayAnAdministratorSeesIt()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var fill = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OverTimeHistoryFillKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fill.Name, Is.EqualTo("Fill in past days on over-time charts"));
                Assert.That(fill.Description, Is.EqualTo("A preview. While this is on, opening Percentiles Over Time or PBC Over Time fills in the days the chart is missing, working them out in the background from the history Lighthouse already stores. Turning it off stops any further filling; days already filled stay."));
            }
        }

        // A backup from before the setting existed never filled a day, so the restore adds it off and records
        // the switch-on as done, and the start-up seeding that follows keeps it off.
        [Test]
        public async Task SettleTheHistoryFillAfterARestore_BackupWithoutTheSetting_AddsItOffAndSeedingKeepsItOff()
        {
            // Arrange - the restored database holds neither the fill nor the record.
            DatabaseContext.OptionalFeatures.RemoveRange(DatabaseContext.OptionalFeatures.Where(feature => feature.Key == OverTimeHistoryFillKey));
            DatabaseContext.AppSettings.RemoveRange(DatabaseContext.AppSettings.Where(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault));
            await DatabaseContext.SaveChangesAsync();

            // Act
            OptionalFeatureSeeder.SettleTheHistoryFillAfterARestore(DatabaseContext);
            await CreateSubject().Seed();

            // Assert
            var fill = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OverTimeHistoryFillKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fill.Enabled, Is.False, "The backup never offered the fill, so it never filled a day, and the restore switched it on.");
                Assert.That(fill.Name, Is.EqualTo("Fill in past days on over-time charts"), "The restored setting reads the way an administrator sees it.");
                Assert.That(fill.IsPreview, Is.True);
                Assert.That(fill.IsPremium, Is.False);
                Assert.That(DatabaseContext.AppSettings.Count(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault), Is.EqualTo(1),
                    "Without exactly one record the next start-up treats the restore as an upgrade and switches the fill on.");
            }
        }

        // A backup that already holds the record and the fill comes back untouched: no second record, and the
        // fill keeps the position the backup held.
        [TestCase(true)]
        [TestCase(false)]
        public async Task SettleTheHistoryFillAfterARestore_BackupWithTheSettingAndTheRecord_LeavesBothAsTheBackupHeldThem(bool heldOn)
        {
            // Arrange
            await CreateSubject().Seed();
            var stored = DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OverTimeHistoryFillKey);
            stored.Enabled = heldOn;
            await DatabaseContext.SaveChangesAsync();

            // Act
            OptionalFeatureSeeder.SettleTheHistoryFillAfterARestore(DatabaseContext);
            await CreateSubject().Seed();

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(DatabaseContext.OptionalFeatures.Single(feature => feature.Key == OverTimeHistoryFillKey).Enabled, Is.EqualTo(heldOn),
                    "The restore should bring the fill back exactly as the backup held it.");
                Assert.That(DatabaseContext.AppSettings.Count(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault), Is.EqualTo(1),
                    "The backup already held the record, so a second one must not be added.");
            }
        }

        // A retired row goes whichever way the operator left it: once the switch is gone there is no
        // choice left to preserve.
        [Test]
        [TestCase(OptionalFeatureKeys.LighthouseChartKey, false)]
        [TestCase(OptionalFeatureKeys.CycleTimeScatterPlotKey, false)]
        [TestCase(OptionalFeatureKeys.LinearIntegrationKey, false)]
        [TestCase(OptionalFeatureKeys.McpServerKey, false)]
        [TestCase(OptionalFeatureKeys.DeltaSyncKey, true)]
        [TestCase(OptionalFeatureKeys.DeltaSyncKey, false)]
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