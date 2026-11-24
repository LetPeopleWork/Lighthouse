using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Platform.UpdateQueue;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Platform.DatabaseManagement;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Platform.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Tests.Acceptance.DatabaseManagement
{
    /// <summary>
    /// A backup holds the stored credentials encrypted, but not the key they were encrypted with. Restored into an
    /// installation whose key reads none of them, the next start refuses, and the operator is locked out by surprise.
    /// So a restore first looks at the backup's credentials with this installation's key and stops, changing nothing,
    /// when it holds credentials and can read none of them. Any other backup restores as before.
    /// </summary>
    [TestFixture]
    public class RestoreStopsWhenNoStoredCredentialCanBeReadTest
    {
        private const string BackupPassword = "restore-check-credentials";

        private const string PeriodInTheBackup = "Company holiday";

        private const string PeriodAddedAfterTheBackup = "Team offsite";

        private const string ReasonTheRestoreStopped =
            "None of the stored credentials in this backup can be read with this installation's encryption key. Nothing has been changed.";

        private static readonly EncryptionKey KeyOfTheInstallationThatMadeTheBackup =
            new("key-backup-origin", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly EncryptionKey KeyOfThisInstallation =
            new("key-this-installation", Convert.FromBase64String("2gMy+eBfMpbvIUlN9fyFHwpBNlNBpw+SVuAOmMVXsaE="));

        private ServicePortHarness harness = null!;

        private EncryptionKeyRingHolder keyInUse = null!;

        private MemoryStream backup = null!;

        private DatabaseOperationStatus outcome = null!;

        private WritesSeen writesDuringTheRestore = null!;

        public enum CredentialsInTheBackup
        {
            None,
            OneReadableAndOneNot,
            AllReadable,
        }

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            keyInUse = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyOfTheInstallationThatMadeTheBackup));
            harness = new ServicePortHarness(RegisterDatabaseManagementAsTheProductBuildsIt);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            backup?.Dispose();
            harness.Dispose();
        }

        [Test]
        public async Task Restoring_a_backup_whose_credentials_this_key_cannot_read_stops_and_changes_nothing()
        {
            await GivenABlackoutPeriodIsStored(PeriodInTheBackup);
            await GivenAConnectionWithASecretOptionIsStored("Contoso Board");
            await GivenAnOAuthCredentialIsStored();
            await GivenABackupIsCreated();
            GivenThisInstallationRunsOnAnotherKey();
            await GivenABlackoutPeriodIsStored(PeriodAddedAfterTheBackup);
            var liveBefore = LiveRows();

            await WhenTheOperatorRestoresTheBackupWatchingTheLiveDatabase();

            ThenTheRestoreStoppedBecauseNoCredentialCanBeRead();
            ThenTheLiveDatabaseIsUnchanged(liveBefore);
            await ThenAnotherDatabaseOperationCanStart();
        }

        [TestCase(CredentialsInTheBackup.None)]
        [TestCase(CredentialsInTheBackup.OneReadableAndOneNot)]
        [TestCase(CredentialsInTheBackup.AllReadable)]
        public async Task Restoring_a_backup_with_no_credentials_or_at_least_one_this_key_can_read_restores_the_backup(
            CredentialsInTheBackup credentials)
        {
            await GivenABlackoutPeriodIsStored(PeriodInTheBackup);
            await GivenTheBackupHolds(credentials);
            await GivenABackupIsCreated();
            await GivenABlackoutPeriodIsStored(PeriodAddedAfterTheBackup);

            await WhenTheOperatorRestoresTheBackupWatchingTheLiveDatabase();

            ThenTheRestoreCompleted();
            ThenTheLiveDatabaseHoldsExactlyTheseBlackoutPeriods([PeriodInTheBackup]);
        }

        private void RegisterDatabaseManagementAsTheProductBuildsIt(IServiceCollection services)
        {
            services.AddSingleton<IEncryptionKeyRingHolder>(keyInUse);
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddScoped<ISeeder, TerminologySeeder>();
            services.AddSingleton<IUpdateStatusStore>(provider =>
                new InProcessUpdateStatusStore(new(), provider.GetRequiredService<ILighthouseClock>()));
            services.AddSingleton<DatabaseMaintenanceGate>();
            services.AddSingleton<DatabaseOperationTracker>();
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(TestToday.FrozenNow));
            services.AddSingleton<IDatabaseManagementProvider>(_ => new SqliteDatabaseManagementProvider(
                Options.Create(new DatabaseConfiguration { Provider = "Sqlite", ConnectionString = $"Data Source={harness.DatabasePath}" }),
                NullLogger<SqliteDatabaseManagementProvider>.Instance));
            services.AddSingleton<IDatabaseManagementService, DatabaseManagementService>();
        }

        private IDatabaseManagementService DatabaseManagement()
            => harness.NewScope().ServiceProvider.GetRequiredService<IDatabaseManagementService>();

        private async Task GivenTheBackupHolds(CredentialsInTheBackup credentials)
        {
            switch (credentials)
            {
                case CredentialsInTheBackup.None:
                    GivenThisInstallationRunsOnAnotherKey();
                    break;
                case CredentialsInTheBackup.OneReadableAndOneNot:
                    await GivenAConnectionWithASecretOptionIsStored("Contoso Board");
                    GivenThisInstallationRunsOnAnotherKey();
                    await GivenAConnectionWithASecretOptionIsStored("Fabrikam Board");
                    break;
                case CredentialsInTheBackup.AllReadable:
                    await GivenAConnectionWithASecretOptionIsStored("Contoso Board");
                    await GivenAnOAuthCredentialIsStored();
                    break;
            }
        }

        private async Task GivenABlackoutPeriodIsStored(string description)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.BlackoutPeriods.Add(new BlackoutPeriod
            {
                Start = new DateOnly(2026, 12, 24),
                End = new DateOnly(2026, 12, 26),
                Description = description,
            });
            await context.SaveChangesAsync();
        }

        private async Task GivenAConnectionWithASecretOptionIsStored(string name)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = new WorkTrackingSystemConnection { Name = name, WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = "PersonalAccessToken",
                Value = $"pat-for-{name}-not-a-real-token",
                IsSecret = true,
            });
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();
        }

        private async Task GivenAnOAuthCredentialIsStored()
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = new WorkTrackingSystemConnection { Name = "Contoso Jira", WorkTrackingSystem = WorkTrackingSystems.Jira };
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            context.Set<OAuthCredential>().Add(new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = "access-not-a-real-token",
                RefreshToken = "refresh-not-a-real-token",
                ExpiresAt = TestToday.FrozenNow.AddHours(1),
                UpdatedAt = TestToday.FrozenNow,
            });
            await context.SaveChangesAsync();
        }

        private async Task GivenABackupIsCreated()
        {
            var service = DatabaseManagement();
            var created = await service.CreateBackup(BackupPassword);
            Assert.That(created.State, Is.EqualTo(DatabaseOperationState.Completed),
                $"Set-up: creating the backup failed ({created.FailureReason}), so there is nothing to restore.");

            backup = new MemoryStream();
            await using var artifact = service.GetBackupArtifact(created.OperationId);
            await artifact.CopyToAsync(backup);
            backup.Position = 0;
        }

        private void GivenThisInstallationRunsOnAnotherKey()
        {
            keyInUse.Replace(new EncryptionKeyRing(KeyOfThisInstallation));
        }

        private async Task WhenTheOperatorRestoresTheBackupWatchingTheLiveDatabase()
        {
            writesDuringTheRestore = await harness.Writes.DuringAsync(async () =>
                outcome = await DatabaseManagement().RestoreBackup(backup, BackupPassword));
        }

        private void ThenTheRestoreStoppedBecauseNoCredentialCanBeRead()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.State, Is.EqualTo(DatabaseOperationState.Failed),
                    $"The restore ended {outcome.State}, but this installation's key reads none of the backup's stored credentials. " +
                    "Restored, such a backup leaves a database the next start refuses. Check the backup's credentials with the key in use " +
                    "before restoring, and stop when it holds credentials and none can be read.");
                Assert.That(outcome.FailureCode, Is.EqualTo(DatabaseOperationFailure.CredentialsUnreadable),
                    "The stopped restore carries no failure code CredentialsUnreadable. The settings page opens its " +
                    "unreadable-credentials dialog on that code alone, never on the reason text, so the code must be on the result.");
                Assert.That(outcome.FailureReason, Is.EqualTo(ReasonTheRestoreStopped),
                    "The operator must read why the restore stopped and that nothing was changed. Put exactly this reason on the result.");
            }
        }

        private void ThenTheLiveDatabaseIsUnchanged(List<string> liveBefore)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(LiveRows(), Is.EqualTo(liveBefore),
                    "The live database holds other rows after the stopped restore than before it. A restore that stops must stop " +
                    "before the live database is touched: check the backup first, restore only after the check passed.");
                Assert.That(writesDuringTheRestore.StatementsThatWrote, Is.Empty,
                    "The stopped restore wrote to the live database. The check must read only the backup's copy, never the live database.");
                Assert.That(writesDuringTheRestore.TheFileChanged, Is.False,
                    "The live database file changed during a restore that stopped. Nothing may reach the live file before the check passed.");
            }
        }

        private async Task ThenAnotherDatabaseOperationCanStart()
        {
            var next = await DatabaseManagement().CreateBackup(BackupPassword);
            Assert.That(next.State, Is.EqualTo(DatabaseOperationState.Completed),
                $"After the stopped restore a backup ended {next.State} ({next.FailureReason}). A stopped restore must let go of the " +
                "single database operation it held, or the operator can run no other database operation until a restart.");
        }

        private void ThenTheRestoreCompleted()
        {
            Assert.That(outcome.State, Is.EqualTo(DatabaseOperationState.Completed),
                $"The restore ended {outcome.State} with \"{outcome.FailureReason}\". This backup holds no credentials, or at least one " +
                "this installation's key can read, so the next start does not refuse it. Stop a restore only when the backup holds " +
                "credentials and none of them can be read.");
        }

        private void ThenTheLiveDatabaseHoldsExactlyTheseBlackoutPeriods(string[] expected)
        {
            Assert.That(Read("SELECT Description FROM BlackoutPeriods ORDER BY Description;"), Is.EqualTo(expected),
                "After a completed restore the live database must hold the backup's data and nothing added after the backup was made.");
        }

        private List<string> LiveRows()
            =>
            [
                .. Read("SELECT Description FROM BlackoutPeriods ORDER BY Description;"),
                .. Read("SELECT Name FROM WorkTrackingSystemConnections ORDER BY Name;"),
                .. Read("SELECT Value FROM WorkTrackingSystemConnectionOption ORDER BY Value;"),
                .. Read("SELECT AccessToken || '|' || RefreshToken FROM OAuthCredentials ORDER BY AccessToken;"),
            ];

        private List<string> Read(string query)
        {
            using var connection = new SqliteConnection($"Data Source={harness.DatabasePath};Pooling=False;Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = query;
            using var reader = command.ExecuteReader();

            var values = new List<string>();
            while (reader.Read())
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }
    }
}
