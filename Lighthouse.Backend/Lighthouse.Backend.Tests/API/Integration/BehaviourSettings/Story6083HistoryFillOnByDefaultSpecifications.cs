using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// Step definitions for the history fill being on by default. Backend-observable contract: after
    /// start-up the history fill reads on for a new instance and for one upgraded from any earlier state;
    /// the instance keeps a record that it switched the fill on; and once that record exists, start-up
    /// never switches the fill again, so an administrator's off holds.
    /// </summary>
    public partial class Story6083HistoryFillOnByDefaultTest : BehaviourSettingsAcceptanceTest
    {
        public enum HowAnEarlierReleaseLeftTheFill
        {
            SwitchedOff,
            SwitchedOn,
            NotYetOffered,
        }

        /// <summary>
        /// The key the setting is addressed by, spelled out rather than read off the product's constant: it
        /// is the identity a browser and a script both use, and an instance upgrading into this release
        /// carries this exact string whatever the constant says.
        /// </summary>
        private const string TheHistoryFillKey = "OverTimeHistoryFill";

        private const string WhatTheAdminTypedToProtectTheBackup = "chosen by the admin";

        private HowAnEarlierReleaseLeftTheFill? whatTheBackupHeld;

        private readonly record struct SettingAsTheAdminReadsIt(
            string Key, bool Enabled, string Name, string Description, bool IsPreview, bool IsPremium);

        // --- Given ---

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        /// <summary>
        /// The database as a release from before the fill shipped on left it: no record that anything
        /// switched the fill on, and the setting itself switched off, switched on, or not stored at all by a
        /// release that predates it. Written straight into the store because that is where an upgrading
        /// instance carries it.
        /// </summary>
        private void GivenTheInstanceAsAnEarlierReleaseLeftIt(HowAnEarlierReleaseLeftTheFill earlierState)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.AppSettings.RemoveRange(
                context.AppSettings.Where(setting => setting.Key == AppSettingKeys.HistoryFillSwitchedOnByDefault));

            var fill = context.OptionalFeatures.Single(feature => feature.Key == TheHistoryFillKey);

            if (earlierState == HowAnEarlierReleaseLeftTheFill.NotYetOffered)
            {
                context.OptionalFeatures.Remove(fill);
            }
            else
            {
                fill.Enabled = earlierState == HowAnEarlierReleaseLeftTheFill.SwitchedOn;
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Checked rather than assumed: the scenarios built on it are about what happens to an admin's off
        /// afterwards, and they say nothing if the fill was never on to begin with.
        /// </summary>
        private async Task GivenANewInstanceWhoseAdminFindsTheHistoryFillOn()
        {
            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            Assert.That(fill.Enabled, Is.True,
                "A new instance should start with the history fill on, so there is no admin's off for the restarts to keep.");
        }

        private async Task GivenTheInstanceWasUpgradedFromAnEarlierReleaseThatLeftTheFillOff()
        {
            GivenTheInstanceAsAnEarlierReleaseLeftIt(HowAnEarlierReleaseLeftTheFill.SwitchedOff);
            WhenTheInstanceIsUpgraded();

            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            Assert.That(fill.Enabled, Is.True,
                "The upgrade should have switched the history fill on, so there is no admin's off for the restarts to keep.");
        }

        private async Task<List<SettingAsTheAdminReadsIt>> GivenWhatTheAdminSawInBehaviourSettingsBeforeTheUpgrade()
        {
            var seen = await WhenTheAdminOpensBehaviourSettings();

            Assert.That(seen.Single(setting => setting.Key == TheHistoryFillKey).Enabled, Is.False,
                "The earlier release was meant to have left the history fill off, so the upgrade has nothing to switch on.");

            return seen;
        }

        /// <summary>
        /// Someone deleted the stored setting after start-up had already recorded switching it on. The record
        /// is checked first, because without it this is just an instance that never had the setting.
        /// </summary>
        private void GivenTheHistoryFillSettingWasRemovedByHand()
        {
            Assert.That(ReadStoredAppSetting(AppSettingKeys.HistoryFillSwitchedOnByDefault), Is.Not.Null,
                "The instance holds no record of having switched the fill on, so removing the setting now describes a different instance.");

            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.OptionalFeatures.Remove(context.OptionalFeatures.Single(feature => feature.Key == TheHistoryFillKey));
            context.SaveChanges();
        }

        /// <summary>
        /// The backup an admin took before upgrading: the history fill stored off, and nothing recorded about a
        /// switch-on, because the release that wrote it never made one.
        /// </summary>
        private void GivenABackupFromBeforeThisReleaseThatHeldTheHistoryFillOff()
            => whatTheBackupHeld = HowAnEarlierReleaseLeftTheFill.SwitchedOff;

        /// <summary>
        /// A backup taken by a release older than the setting: no history fill stored at all, and nothing
        /// recorded about a switch-on. That instance never filled a day.
        /// </summary>
        private void GivenABackupFromBeforeTheHistoryFillSettingExisted()
            => whatTheBackupHeld = HowAnEarlierReleaseLeftTheFill.NotYetOffered;

        // --- When ---

        /// <summary>
        /// Through the restore an admin starts from Settings, with only the database provider replaced, the way
        /// the database-management tests drive it. The provider's part of a restore is to put the backup's
        /// database in place, so the stand-in does exactly that: an empty database with the current schema,
        /// holding what the backup held. Everything after it - migrating and running every seeder - is the
        /// product's own.
        /// </summary>
        private async Task WhenTheAdminRestoresTheBackup()
        {
            var provider = ADatabaseProvider();
            provider.Setup(p => p.RestoreBackup(It.IsAny<string>()))
                .Returns(() =>
                {
                    TheBackupsDatabaseIsPutInPlace();
                    return Task.CompletedTask;
                });

            using var backup = EncryptedBackupStream.Create(WhatTheAdminTypedToProtectTheBackup);
            var status = await TheDatabaseManagementWith(provider).RestoreBackup(backup, WhatTheAdminTypedToProtectTheBackup);

            AssertTheOperationRanThrough(status, "restore");
        }

        /// <summary>The provider's part of clearing is to leave an empty database; the product rebuilds it.</summary>
        private async Task WhenTheAdminClearsTheDatabase()
        {
            var provider = ADatabaseProvider();
            provider.Setup(p => p.ClearDatabase())
                .Returns(() =>
                {
                    using var scope = Factory.Services.CreateScope();
                    scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
                    return Task.CompletedTask;
                });

            var status = await TheDatabaseManagementWith(provider).ClearDatabase();

            AssertTheOperationRanThrough(status, "clear");
        }

        /// <summary>What a start-up does to stored settings: every seeder runs against the existing database.</summary>
        private void WhenTheInstanceIsUpgraded() => RunEverySeeder();

        private void WhenTheInstanceRestarts(int times)
        {
            for (var restart = 0; restart < times; restart++)
            {
                RunEverySeeder();
            }
        }

        private async Task<SettingAsTheAdminReadsIt> WhenTheAdminReadsTheHistoryFillSetting()
        {
            var response = await GetOptionalFeature(TheHistoryFillKey);

            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"Behaviour settings should offer the history fill. Body: {Excerpt(response.Body)}");

            using var document = JsonDocument.Parse(response.Body);
            return ReadSetting(document.RootElement);
        }

        private async Task<List<SettingAsTheAdminReadsIt>> WhenTheAdminOpensBehaviourSettings()
        {
            var rows = ParseOptionalFeatureRows(await GetOptionalFeatures());
            return [.. rows.Select(ReadSetting)];
        }

        private async Task WhenTheAdminSwitchesTheHistoryFillOff()
        {
            var response = await ToggleOptionalFeature(TheHistoryFillKey, enabled: false);

            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"A System Admin asked to switch the history fill off and was refused, so every step after this watches the wrong position. Body: {Excerpt(response.Body)}");
        }

        // --- Then ---

        private static void ThenTheHistoryFillReadsOn(SettingAsTheAdminReadsIt fill)
            => Assert.That(fill.Enabled, Is.True, "The history fill should read on.");

        private static void ThenTheHistoryFillReadsOff(SettingAsTheAdminReadsIt fill)
            => Assert.That(fill.Enabled, Is.False, "The admin switched the history fill off, and a restart turned it back on.");

        /// <summary>
        /// Without this record the next start-up cannot tell the switch-on already happened, and would undo
        /// an admin's off. Read from the store because no port shows it, and nothing should.
        /// </summary>
        private void ThenTheInstanceRemembersItSwitchedTheFillOn()
            => Assert.That(ReadStoredAppSetting(AppSettingKeys.HistoryFillSwitchedOnByDefault), Is.Not.Null,
                "The instance holds no record that the one-time switch-on is settled, so the next start-up will switch the fill on.");

        private static void ThenTheHistoryFillReadsAsTheBackupHeldIt(SettingAsTheAdminReadsIt fill)
            => Assert.That(fill.Enabled, Is.False, "The backup held the history fill off, and the restore brought it back on.");

        private static void ThenTheHistoryFillReadsOffLikeTheBackupThatNeverOfferedIt(SettingAsTheAdminReadsIt fill, string when)
            => Assert.That(fill.Enabled, Is.False,
                $"The backup came from before the history fill existed, so it never filled a day, and {when} the fill reads on.");

        private static void ThenOnlyTheHistoryFillWasSwitchedOn(
            List<SettingAsTheAdminReadsIt> before, List<SettingAsTheAdminReadsIt> after)
        {
            var expected = before
                .Select(setting => setting.Key == TheHistoryFillKey ? setting with { Enabled = true } : setting)
                .ToList();

            Assert.That(after, Is.EquivalentTo(expected),
                "The upgrade should switch the history fill on and leave its name, description, preview and premium flags, and every other setting, as they were.");
        }

        // --- Database management, with only the provider replaced ---

        private static Mock<IDatabaseManagementProvider> ADatabaseProvider()
        {
            var provider = new Mock<IDatabaseManagementProvider>();
            provider.Setup(p => p.ProviderName).Returns("sqlite");
            provider.Setup(p => p.IsToolingAvailable()).Returns(true);

            return provider;
        }

        private DatabaseManagementService TheDatabaseManagementWith(Mock<IDatabaseManagementProvider> provider)
            => new(
                provider.Object,
                Factory.Services.GetRequiredService<DatabaseMaintenanceGate>(),
                Factory.Services.GetRequiredService<DatabaseOperationTracker>(),
                Factory.Services.GetRequiredService<ILogger<DatabaseManagementService>>(),
                Factory.Services);

        private void TheBackupsDatabaseIsPutInPlace()
        {
            Assert.That(whatTheBackupHeld, Is.Not.Null, "No backup was described, so there is nothing to restore.");

            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.Database.EnsureDeleted();
            context.Database.Migrate();

            if (whatTheBackupHeld == HowAnEarlierReleaseLeftTheFill.NotYetOffered)
            {
                return;
            }

            context.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 0,
                Key = TheHistoryFillKey,
                Name = "Fill in past days on over-time charts",
                Description = "A preview. While this is on, opening Percentiles Over Time or PBC Over Time fills in the days the chart is missing.",
                Enabled = false,
                IsPreview = true,
                IsPremium = false,
            });
            context.SaveChanges();
        }

        /// <summary>
        /// A restore or clear that reports success has still not proven the seeders ran afterwards, because a
        /// failure there is logged rather than reported. The ordering setting is never in the restored or
        /// cleared database, so finding it means they did.
        /// </summary>
        private void AssertTheOperationRanThrough(DatabaseOperationStatus status, string operation)
        {
            var seededAfterwards = ReadStoredOptionalFeature(FeatureOrderingOptionalFeatureKey).Found;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Completed),
                    $"The {operation} did not complete: {status.FailureReason}");
                Assert.That(seededAfterwards, Is.True,
                    $"The seeders did not run after the {operation}, so nothing here says what they do there.");
            }
        }

        private static SettingAsTheAdminReadsIt ReadSetting(JsonElement row)
            => new(
                row.GetProperty("key").GetString() ?? string.Empty,
                row.GetProperty("enabled").GetBoolean(),
                row.GetProperty("name").GetString() ?? string.Empty,
                row.GetProperty("description").GetString() ?? string.Empty,
                row.GetProperty("isPreview").GetBoolean(),
                row.GetProperty("isPremium").GetBoolean());
    }
}
