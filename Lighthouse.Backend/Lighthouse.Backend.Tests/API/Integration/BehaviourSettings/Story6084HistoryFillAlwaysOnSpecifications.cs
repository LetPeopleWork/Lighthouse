using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Tests.TestHelpers;
using static Lighthouse.Backend.Tests.TestHelpers.TheHistoryFillAsAnEarlierReleaseStoredIt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// Step definitions for the history fill no longer being a setting. Backend-observable contract: after
    /// start-up, a restore or a clear, Behaviour Settings offers no history fill, the store holds neither
    /// the setting nor the record an earlier release kept of switching it on once, and nothing else an
    /// administrator stored has changed.
    /// </summary>
    public partial class Story6084HistoryFillAlwaysOnTest : BehaviourSettingsAcceptanceTest
    {
        public enum HowTheLastReleaseLeftTheFill
        {
            SwitchedOnByDefault,
            SwitchedOffByTheAdmin,
            SwitchedOffBeforeItWasSwitchedOnByDefault,
            SettingRemovedButTheRecordKept,
            NotYetOffered,
        }

        private const string WhatTheAdminTypedToProtectTheBackup = "chosen by the admin";

        private HowTheLastReleaseLeftTheFill? whatTheBackupHeld;

        private readonly record struct SettingAsTheAdminReadsIt(
            string Key, bool Enabled, string Name, string Description, bool IsPreview, bool IsPremium);

        private sealed record WhatTheInstanceHeld(List<SettingAsTheAdminReadsIt> Offered, List<string> StoredValues);

        // --- Given ---

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        /// <summary>
        /// The database as the last release left it, written straight into the store because that is where an
        /// upgrading instance carries it.
        /// </summary>
        private void GivenTheInstanceAsTheLastReleaseLeftIt(HowTheLastReleaseLeftTheFill lastRelease)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            StoreTheFillAs(context, lastRelease);
        }

        private async Task<WhatTheInstanceHeld> GivenWhatTheInstanceHeldBeforeTheUpgrade(HowTheLastReleaseLeftTheFill lastRelease)
        {
            var offered = await WhenTheAdminOpensBehaviourSettings();

            Assert.That(offered.Select(setting => setting.Key), Does.Contain(TheHistoryFillKey),
                "The last release was meant to have left the history fill in Behaviour Settings, so the upgrade has nothing to take away.");

            if (TheReleaseKeptTheRecordOfTheSwitchOn(lastRelease))
            {
                GivenTheRecordOfTheSwitchOnIsStored();
            }

            return new WhatTheInstanceHeld(offered, ReadEveryStoredValue());
        }

        /// <summary>
        /// Checked rather than assumed: that the upgrade clears the record away says nothing if the record was
        /// never there to clear.
        /// </summary>
        private void GivenTheRecordOfTheSwitchOnIsStored()
            => Assert.That(ReadStoredAppSetting(TheRecordOfTheOneTimeSwitchOn), Is.Not.Null,
                "The last release was meant to have kept the record of the one-time switch-on, so the upgrade has no record to clear away.");

        /// <summary>
        /// Checked rather than assumed: the restarts that follow are about keeping the setting away, and they
        /// say nothing if the upgrade never took it away to begin with.
        /// </summary>
        private async Task<WhatTheInstanceHeld> GivenAnInstanceUpgradedFromAReleaseWhoseAdminHadSwitchedTheFillOff()
        {
            GivenTheInstanceAsTheLastReleaseLeftIt(HowTheLastReleaseLeftTheFill.SwitchedOffByTheAdmin);
            WhenTheInstanceIsUpgraded();

            var offered = await WhenTheAdminOpensBehaviourSettings();

            Assert.That(offered.Select(setting => setting.Key), Does.Not.Contain(TheHistoryFillKey),
                "The upgrade should already have taken the history fill out of Behaviour Settings, so there is nothing for the restarts to keep away.");

            return new WhatTheInstanceHeld(offered, ReadEveryStoredValue());
        }

        private void GivenABackupThatHeldTheFill(HowTheLastReleaseLeftTheFill held) => whatTheBackupHeld = held;

        // --- When ---

        /// <summary>What a start-up does to stored settings: every seeder runs against the existing database.</summary>
        private void WhenTheInstanceIsUpgraded() => RunEverySeeder();

        private void WhenTheInstanceRestarts(int times)
        {
            for (var restart = 0; restart < times; restart++)
            {
                RunEverySeeder();
            }
        }

        private async Task<List<SettingAsTheAdminReadsIt>> WhenTheAdminOpensBehaviourSettings()
        {
            var rows = ParseOptionalFeatureRows(await GetOptionalFeatures());
            return [.. rows.Select(ReadSetting)];
        }

        private async Task<HttpStatusCode> WhenAScriptReadsTheHistoryFillSettingByName()
            => (await GetOptionalFeature(TheHistoryFillKey)).Status;

        /// <summary>
        /// The write a script made while the fill was a setting: the whole setting, as the release before this
        /// one served it, with only the switch changed.
        /// </summary>
        private async Task<HttpStatusCode> WhenAScriptSwitchesTheHistoryFillOff()
        {
            var asServed = TheFillAsTheLastReleaseStoredIt(enabled: true);
            var payload = JsonSerializer.Serialize(new
            {
                id = IdentityEverySeededSettingCarries,
                key = TheHistoryFillKey,
                name = asServed.Name,
                description = asServed.Description,
                enabled = false,
                isPremium = false,
                isPreview = true,
            });

            using var body = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"/api/latest/optionalfeatures/{TheHistoryFillKey}", body);

            return response.StatusCode;
        }

        /// <summary>
        /// Through the restore an admin starts from Settings, with only the database provider replaced, the way
        /// the database-management tests drive it. The provider's part of a restore is to put the backup's
        /// database in place, so the stand-in does exactly that: an empty database with the current schema,
        /// holding what the backup held. Everything after it, migrating and running every seeder, is the
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

        // --- Then ---

        /// <summary>
        /// Checked against a setting the list does offer first, because an empty or unreadable list would
        /// offer no history fill either.
        /// </summary>
        private static void ThenBehaviourSettingsOffersNoHistoryFill(List<SettingAsTheAdminReadsIt> offered)
        {
            var keys = offered.Select(setting => setting.Key).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(keys, Does.Contain(FeatureOrderingOptionalFeatureKey),
                    "Behaviour Settings did not offer the settings it always offers, so it showing no history fill proves nothing.");

                Assert.That(keys, Does.Not.Contain(TheHistoryFillKey),
                    "Behaviour Settings still offers the history fill. The fill always runs, so there is nothing to switch.");
            }
        }

        /// <summary>
        /// Read from the store because no port shows the record, and nothing should. Without this the setting
        /// could be hidden from the list while still stored, and an old replica would read it.
        /// </summary>
        private void ThenNothingIsLeftOfTheHistoryFillSetting()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadStoredOptionalFeature(TheHistoryFillKey).Found, Is.False,
                    "The history fill setting is still stored.");

                Assert.That(ReadStoredAppSetting(TheRecordOfTheOneTimeSwitchOn), Is.Null,
                    "The record of the one-time switch-on is still stored, though nothing will ever read it again.");
            }
        }

        private static void ThenOnlyTheHistoryFillLeftBehaviourSettings(WhatTheInstanceHeld before, List<SettingAsTheAdminReadsIt> after)
        {
            var expected = before.Offered.Where(setting => setting.Key != TheHistoryFillKey).ToList();

            Assert.That(after, Is.EquivalentTo(expected),
                "The upgrade should take the history fill out of Behaviour Settings and leave every other setting, its name, description, preview and premium flags and whether it is on, as it was.");
        }

        private void ThenOnlyTheRecordOfTheSwitchOnLeftTheStoredSettings(WhatTheInstanceHeld before)
        {
            var expected = before.StoredValues.Where(stored => !stored.StartsWith(TheRecordOfTheOneTimeSwitchOn + "=", StringComparison.Ordinal)).ToList();

            Assert.That(ReadEveryStoredValue(), Is.EqualTo(expected),
                "The upgrade should remove the record of the one-time switch-on and leave every other stored value as it was.");
        }

        private static void ThenBehaviourSettingsReadsExactlyAsItDid(List<SettingAsTheAdminReadsIt> before, List<SettingAsTheAdminReadsIt> after)
            => Assert.That(after, Is.EquivalentTo(before), "A restart changed what Behaviour Settings offers.");

        private void ThenEveryStoredValueReadsExactlyAsItDid(WhatTheInstanceHeld before)
            => Assert.That(ReadEveryStoredValue(), Is.EqualTo(before.StoredValues), "A restart changed a stored value.");

        private static void ThenTheScriptIsToldThereIsNoSuchSetting(HttpStatusCode answer, string what)
            => Assert.That(answer, Is.EqualTo(HttpStatusCode.NotFound),
                $"A script {what} the history fill should be told there is no such setting.");

        // --- The store as an earlier release left it ---

        private static void StoreTheFillAs(LighthouseAppContext context, HowTheLastReleaseLeftTheFill state)
        {
            context.OptionalFeatures.RemoveRange(context.OptionalFeatures.Where(feature => feature.Key == TheHistoryFillKey));
            context.AppSettings.RemoveRange(context.AppSettings.Where(setting => setting.Key == TheRecordOfTheOneTimeSwitchOn));

            if (state is HowTheLastReleaseLeftTheFill.SwitchedOnByDefault
                or HowTheLastReleaseLeftTheFill.SwitchedOffByTheAdmin
                or HowTheLastReleaseLeftTheFill.SwitchedOffBeforeItWasSwitchedOnByDefault)
            {
                context.OptionalFeatures.Add(TheFillAsTheLastReleaseStoredIt(state == HowTheLastReleaseLeftTheFill.SwitchedOnByDefault));
            }

            if (TheReleaseKeptTheRecordOfTheSwitchOn(state))
            {
                context.AppSettings.Add(TheRecordOfTheOneTimeSwitchOnAsStored());
            }

            context.SaveChanges();
        }

        private static bool TheReleaseKeptTheRecordOfTheSwitchOn(HowTheLastReleaseLeftTheFill state)
            => state is HowTheLastReleaseLeftTheFill.SwitchedOnByDefault
                or HowTheLastReleaseLeftTheFill.SwitchedOffByTheAdmin
                or HowTheLastReleaseLeftTheFill.SettingRemovedButTheRecordKept;

        /// <summary>Every stored app setting as key and value, in key order, so two readings compare line by line.</summary>
        private List<string> ReadEveryStoredValue()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return [.. context.AppSettings
                .AsNoTracking()
                .Select(setting => setting.Key + "=" + setting.Value)
                .AsEnumerable()
                .Order(StringComparer.Ordinal)];
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

            StoreTheFillAs(context, whatTheBackupHeld!.Value);
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
