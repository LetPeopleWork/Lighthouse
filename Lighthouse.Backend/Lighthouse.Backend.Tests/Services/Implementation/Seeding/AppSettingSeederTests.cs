using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class AppSettingSeederTests() : IntegrationTestBase
    {
        [Test]
        [TestCase(AppSettingKeys.TeamDataRefreshInterval, "60")]
        [TestCase(AppSettingKeys.TeamDataRefreshAfter, "180")]
        [TestCase(AppSettingKeys.TeamDataRefreshStartDelay, "10")]
        [TestCase(AppSettingKeys.FeaturesRefreshInterval, "60")]
        [TestCase(AppSettingKeys.FeaturesRefreshAfter, "180")]
        [TestCase(AppSettingKeys.FeaturesRefreshStartDelay, "15")]
        public async Task SeedAsync_AddsDefaultSettings_WhenDatabaseIsEmpty(string key, string expectedValue)
        {
            var subject = CreateSubject();

            await subject.Seed();

            var appSetting = DatabaseContext.AppSettings.Single(s => s.Key == key);
            Assert.That(appSetting.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task SeedAsync_DoesNotDuplicate_WhenSettingsAlreadyExist()
        {
            // Arrange
            DatabaseContext.AppSettings.Add(new AppSetting
            {
                Id = 0,
                Key = AppSettingKeys.TeamDataRefreshInterval,
                Value = "120" // Different value
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var settings = DatabaseContext.AppSettings
                .Where(s => s.Key == AppSettingKeys.TeamDataRefreshInterval)
                .ToList();

            Assert.That(settings, Has.Count.EqualTo(1));
            Assert.That(settings[0].Value, Is.EqualTo("120")); // Original value preserved
        }

        [Test]
        [TestCase(9)]
        [TestCase(15)]
        [TestCase(25)]
        [TestCase(42)]
        public async Task SeedAsync_RemovesObsoleteSettings(int obsoleteId)
        {
            // Arrange
            DatabaseContext.AppSettings.Add(new AppSetting
            {
                Id = obsoleteId,
                Key = $"ObsoleteSetting{obsoleteId}",
                Value = "old"
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var obsoleteSetting = DatabaseContext.AppSettings
                .FirstOrDefault(s => s.Id == obsoleteId);

            Assert.That(obsoleteSetting, Is.Null);
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
            var settings = DatabaseContext.AppSettings.ToList();
            Assert.That(settings, Has.Count.EqualTo(7));
        }

        [Test]
        public async Task SeedAsync_RemovesMultipleObsoleteSettings_InSingleOperation()
        {
            // Arrange
            var obsoleteIds = new[] { 9, 10, 15, 20, 25, 30 };
            foreach (var id in obsoleteIds)
            {
                DatabaseContext.AppSettings.Add(new AppSetting
                {
                    Id = id,
                    Key = $"ObsoleteSetting{id}",
                    Value = "old"
                });
            }
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var remainingObsolete = DatabaseContext.AppSettings
                .Where(s => obsoleteIds.Contains(s.Id))
                .ToList();

            Assert.That(remainingObsolete, Is.Empty);
        }

        // The record that the history fill was switched on once is cleared by its key, because it was written
        // at runtime with the number every runtime setting gets, zero, which a default setting also carries. So
        // clearing it must not take any other setting that shares its number, and the clear-out by number must
        // go on removing exactly what it removed before and keeping everything it kept, numbers it skips included.
        [Test]
        public async Task SeedAsync_RemovesTheRecordThatTheHistoryFillWasSwitchedOn_AndKeepsEverythingTheClearOutByNumberKeeps()
        {
            // Arrange
            DatabaseContext.AppSettings.AddRange(
                new AppSetting { Id = 0, Key = TheRecordThatTheHistoryFillWasSwitchedOn, Value = "true" },
                new AppSetting { Id = 0, Key = AppSettingKeys.InstallTimestamp, Value = "2026-01-01T00:00:00Z" },
                new AppSetting { Id = 9, Key = "RetiredUnderNumber9", Value = "retired" },
                new AppSetting { Id = 18, Key = "KeptUnderNumber18", Value = "18" },
                new AppSetting { Id = 26, Key = "KeptUnderNumber26", Value = "26" },
                new AppSetting { Id = 37, Key = "KeptUnderNumber37", Value = "37" });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();
            await subject.Seed();

            // Assert
            Assert.That(DatabaseContext.AppSettings.Select(setting => setting.Key).ToList(), Is.EquivalentTo(WhatStartUpKeeps),
                "Start-up should remove the record that the history fill was switched on and the setting retired under number 9, and keep every other setting.");
        }

        private const string TheRecordThatTheHistoryFillWasSwitchedOn = "OptionalFeatures:HistoryFillSwitchedOnByDefault";

        private static readonly string[] WhatStartUpKeeps =
        [
            AppSettingKeys.TeamDataRefreshInterval,
            AppSettingKeys.TeamDataRefreshAfter,
            AppSettingKeys.TeamDataRefreshStartDelay,
            AppSettingKeys.FeaturesRefreshInterval,
            AppSettingKeys.FeaturesRefreshAfter,
            AppSettingKeys.FeaturesRefreshStartDelay,
            AppSettingKeys.RefreshLogRetentionRuns,
            AppSettingKeys.InstallTimestamp,
            "KeptUnderNumber18",
            "KeptUnderNumber26",
            "KeptUnderNumber37",
        ];

        private AppSettingSeeder CreateSubject()
        {
            return new AppSettingSeeder(DatabaseContext, Mock.Of<ILogger<AppSettingSeeder>>());
        }
    }
}