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

        // Retired settings are cleared by their stored number. The record that the history fill was switched on
        // is written at runtime, so it must never land on one of those numbers, or the next start-up deletes it
        // and switches the fill on again over an administrator's off. Written here by the real seeder, so the
        // number under test is whatever start-up actually gives it.
        [Test]
        public async Task SeedAsync_KeepsTheRecordThatTheHistoryFillWasSwitchedOn()
        {
            // Arrange
            await new OptionalFeatureSeeder(DatabaseContext, Mock.Of<ILogger<OptionalFeatureSeeder>>()).Seed();

            Assert.That(DatabaseContext.AppSettings.Any(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault), Is.True,
                "Start-up recorded nothing about switching the history fill on, so there is no record for the clean-up to keep.");

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            Assert.That(DatabaseContext.AppSettings.Any(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault), Is.True,
                "Clearing out retired settings removed the record that the history fill was switched on.");
        }

        private AppSettingSeeder CreateSubject()
        {
            return new AppSettingSeeder(DatabaseContext, Mock.Of<ILogger<AppSettingSeeder>>());
        }
    }
}