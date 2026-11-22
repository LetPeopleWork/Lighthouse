using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Lighthouse.Backend.Tests.Acceptance.DatabaseManagement
{
    /// <summary>
    /// A backup whose stored credentials this installation's key cannot read would leave a database the next start
    /// refuses. The operator may still restore it on purpose: the restore then removes the credentials it cannot read,
    /// keeps everything else, and the application starts; the operator enters each token again in its Connection.
    /// A backup with at least one readable credential, or none at all, is restored exactly as it was.
    /// </summary>
    [TestFixture]
    public class RestoreWithoutUnreadableCredentialsTest
    {
        private const string BackupPassword = "restore-without-credentials";

        private const string PeriodInTheBackup = "Company holiday";

        private const string PeriodAddedAfterTheBackup = "Team offsite";

        private const string BoardConnection = "Contoso Board";

        private const string OAuthConnection = "Contoso Jira";

        private const string OtherBoardConnection = "Fabrikam Board";

        private const string OtherOAuthConnection = "Fabrikam Jira";

        private const string ScratchConnection = "Northwind Jira";

        private const string AnotherOAuthConnection = "Woodgrove Jira";

        private const string OrganisationInTheBackup = "contoso";

        private static readonly EncryptionKey KeyOfTheInstallationThatMadeTheBackup =
            new("key-backup-origin", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly EncryptionKey KeyOfThisInstallation =
            new("key-this-installation", Convert.FromBase64String("2gMy+eBfMpbvIUlN9fyFHwpBNlNBpw+SVuAOmMVXsaE="));

        private ServicePortHarness harness = null!;

        private EncryptionKeyRingHolder keyInUse = null!;

        private MemoryStream backup = null!;

        private List<string> rowsInTheBackup = null!;

        private DatabaseOperationStatus outcome = null!;

        public enum CredentialsInTheBackup
        {
            None,
            OneReadableAndOneNot,
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
        public async Task Restoring_without_credentials_completes_removes_the_unreadable_credentials_and_lets_the_application_start()
        {
            await GivenABlackoutPeriodIsStored(PeriodInTheBackup);
            await GivenAConnectionWithASecretAndAPlainOptionIsStored(BoardConnection);
            await GivenAnOAuthCredentialIsStored(OAuthConnection);
            await GivenABackupIsCreated();
            GivenThisInstallationRunsOnAnotherKey();
            await GivenABlackoutPeriodIsStored(PeriodAddedAfterTheBackup);

            await WhenTheOperatorRestoresTheBackupWithoutItsUnreadableCredentials();

            ThenTheRestoreCompleted();
            ThenTheNextStartFindsNoStoredCredentialItWouldRefuse();
            ThenEveryConnectionAndItsPlainOptionsAreKeptWithTheTokenEmpty();
            ThenNoOAuthCredentialIsLeft();
            ThenTheLiveDatabaseHoldsExactlyTheseBlackoutPeriods([PeriodInTheBackup]);
        }

        [TestCase(CredentialsInTheBackup.None)]
        [TestCase(CredentialsInTheBackup.OneReadableAndOneNot)]
        public async Task Restoring_without_credentials_a_backup_with_no_credentials_or_one_this_key_can_read_restores_it_unchanged(
            CredentialsInTheBackup credentials)
        {
            await GivenABlackoutPeriodIsStored(PeriodInTheBackup);
            await GivenTheBackupHolds(credentials);
            await GivenABackupIsCreated();
            await GivenABlackoutPeriodIsStored(PeriodAddedAfterTheBackup);

            await WhenTheOperatorRestoresTheBackupWithoutItsUnreadableCredentials();

            ThenTheRestoreCompleted();
            ThenTheLiveDatabaseEqualsTheBackup();
        }

        [Test]
        public async Task Restoring_without_credentials_a_backup_whose_credentials_cannot_be_checked_restores_it_without_removing_any()
        {
            await GivenAConnectionWithASecretAndAPlainOptionIsStored(BoardConnection);
            GivenThisInstallationRunsOnAnotherKey();
            GivenTheDatabaseComesFromAVersionThatStoredNoOAuthCredentials();
            await GivenABackupOfItIsCreated();

            await WhenTheOperatorRestoresTheBackupWithoutItsUnreadableCredentials();

            ThenTheRestoreCompleted();
            ThenTheSecretOptionsAre([$"{BoardConnection}|PersonalAccessToken|unreadable"]);
        }

        [Test]
        public async Task Removing_unreadable_credentials_blanks_or_deletes_only_what_this_key_cannot_read()
        {
            await GivenAConnectionWithASecretAndAPlainOptionIsStored(BoardConnection);
            await GivenAnOAuthCredentialIsStored(ScratchConnection);
            GivenThisInstallationRunsOnAnotherKey();
            await GivenAConnectionWithASecretAndAPlainOptionIsStored(OtherBoardConnection);
            GivenAnEmptySecretOptionIsStoredOn(OtherBoardConnection);
            await GivenAnOAuthCredentialIsStored(OtherOAuthConnection);
            await GivenAnOAuthCredentialIsStored(OAuthConnection);
            GivenItsTokenWasStoredUnderAnotherKey(OAuthConnection, "RefreshToken", takenFrom: ScratchConnection);
            await GivenAnOAuthCredentialIsStored(AnotherOAuthConnection);
            GivenItsTokenWasStoredUnderAnotherKey(AnotherOAuthConnection, "AccessToken", takenFrom: ScratchConnection);

            WhenTheUnreadableCredentialsAreRemovedFromTheLiveDatabase();

            ThenTheSecretOptionsAre(
            [
                $"{BoardConnection}|PersonalAccessToken|",
                $"{OtherBoardConnection}|EmptyToken|",
                $"{OtherBoardConnection}|PersonalAccessToken|readable",
            ]);
            ThenThePlainOptionsAreKept([BoardConnection, OtherBoardConnection]);
            ThenOAuthCredentialsAreLeftOnlyFor([OtherOAuthConnection]);
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
                    await GivenAConnectionWithASecretAndAPlainOptionIsStored(BoardConnection);
                    GivenThisInstallationRunsOnAnotherKey();
                    await GivenAConnectionWithASecretAndAPlainOptionIsStored(OtherBoardConnection);
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

        private async Task GivenAConnectionWithASecretAndAPlainOptionIsStored(string name)
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
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = "Organization",
                Value = OrganisationInTheBackup,
                IsSecret = false,
            });
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();
        }

        // Written past the application, because saving through it would encrypt the empty value and store
        // something readable instead of nothing.
        private void GivenAnEmptySecretOptionIsStoredOn(string connectionName)
        {
            Write(
                """
                INSERT INTO WorkTrackingSystemConnectionOption (Key, Value, IsSecret, IsOptional, WorkTrackingSystemConnectionId)
                VALUES ('EmptyToken', '', 1, 0, (SELECT Id FROM WorkTrackingSystemConnections WHERE Name = $name));
                """,
                ("$name", connectionName));
        }

        private async Task GivenAnOAuthCredentialIsStored(string connectionName)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = new WorkTrackingSystemConnection { Name = connectionName, WorkTrackingSystem = WorkTrackingSystems.Jira };
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            context.Set<OAuthCredential>().Add(new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = $"access-for-{connectionName}",
                RefreshToken = $"refresh-for-{connectionName}",
                ExpiresAt = TestToday.FrozenNow.AddHours(1),
                UpdatedAt = TestToday.FrozenNow,
            });
            await context.SaveChangesAsync();
        }

        // The stored token is copied over as it lies in the database, so this row ends up with one token this key
        // reads and one that only the other key can read.
        private void GivenItsTokenWasStoredUnderAnotherKey(string connectionName, string tokenColumn, string takenFrom)
        {
            Assert.That(tokenColumn, Is.AnyOf("AccessToken", "RefreshToken"), "Set-up: only an OAuth token column can be copied.");
            Write(
                $"""
                UPDATE OAuthCredentials SET {tokenColumn} =
                    (SELECT o.{tokenColumn} FROM OAuthCredentials o JOIN WorkTrackingSystemConnections c ON c.Id = o.WorkTrackingSystemConnectionId WHERE c.Name = $from)
                WHERE WorkTrackingSystemConnectionId = (SELECT Id FROM WorkTrackingSystemConnections WHERE Name = $to);
                """,
                ("$from", takenFrom),
                ("$to", connectionName));
        }

        // An older version had no table for OAuth credentials, so a backup it made cannot be asked about them: the check
        // cannot tell whether the credentials are readable, and the restore must go ahead without removing anything.
        private void GivenTheDatabaseComesFromAVersionThatStoredNoOAuthCredentials()
        {
            Write("DROP TABLE OAuthCredentials;");
        }

        private async Task GivenABackupIsCreated()
        {
            await GivenABackupOfItIsCreated();
            rowsInTheBackup = LiveRows();
        }

        private async Task GivenABackupOfItIsCreated()
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

        private async Task WhenTheOperatorRestoresTheBackupWithoutItsUnreadableCredentials()
        {
            outcome = await DatabaseManagement().RestoreBackup(backup, BackupPassword, UnreadableCredentials.Remove);
        }

        private void WhenTheUnreadableCredentialsAreRemovedFromTheLiveDatabase()
        {
            using DbConnection live = new SqliteConnection($"Data Source={harness.DatabasePath};Pooling=False");
            new UnreadableStoredSecrets(keyInUse.Current).RemoveFrom(live);
        }

        private void ThenTheRestoreCompleted()
        {
            Assert.That(outcome.State, Is.EqualTo(DatabaseOperationState.Completed),
                $"The restore without credentials ended {outcome.State} with \"{outcome.FailureReason}\". The operator chose to restore " +
                "anyway, so the restore must complete: remove what this key cannot read when nothing is readable, otherwise restore as is.");
        }

        private void ThenTheNextStartFindsNoStoredCredentialItWouldRefuse()
        {
            var finding = new DatabaseSecretReadabilityProbe(LiveConnection).Look(keyInUse.Current);
            Assert.That(finding.Readability, Is.EqualTo(StoredSecretReadability.NothingStored),
                $"After the restore without credentials the start-up check answers {finding.Readability}. Only NothingReadable makes " +
                "the next start refuse; the restore must remove every credential this key cannot read so the check finds nothing stored.");
        }

        private void ThenEveryConnectionAndItsPlainOptionsAreKeptWithTheTokenEmpty()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Read("SELECT Name FROM WorkTrackingSystemConnections ORDER BY Name;"), Is.EqualTo(new[] { BoardConnection, OAuthConnection }),
                    "A Connection of the backup is missing after the restore without credentials. Only the credentials are removed; " +
                    "the Connections stay so the operator can enter each token again in its Connection.");
                Assert.That(Read("SELECT Value FROM WorkTrackingSystemConnectionOption WHERE IsSecret = 0;"), Is.EqualTo(new[] { OrganisationInTheBackup }),
                    "A plain option of the backup is lost or changed. Only secret values this key cannot read are blanked.");
                Assert.That(Read("SELECT Value FROM WorkTrackingSystemConnectionOption WHERE IsSecret = 1;"), Is.EqualTo(new[] { string.Empty }),
                    "The secret option the key cannot read must be kept, with its value empty, so its Connection asks for the token again.");
            }
        }

        private void ThenNoOAuthCredentialIsLeft()
        {
            Assert.That(Read("SELECT CAST(Id AS TEXT) FROM OAuthCredentials;"), Is.Empty,
                "An OAuth credential this key cannot read is still stored after the restore without credentials. Delete it, so its " +
                "Connection shows it is no longer authorised and the next start does not refuse.");
        }

        private void ThenTheLiveDatabaseHoldsExactlyTheseBlackoutPeriods(string[] expected)
        {
            Assert.That(Read("SELECT Description FROM BlackoutPeriods ORDER BY Description;"), Is.EqualTo(expected),
                "After a completed restore the live database must hold the backup's data and nothing added after the backup was made.");
        }

        private void ThenTheLiveDatabaseEqualsTheBackup()
        {
            Assert.That(LiveRows(), Is.EqualTo(rowsInTheBackup),
                "The live database differs from the backup after a restore without credentials. When the backup holds no credential, " +
                "or at least one this key can read, nothing may be removed: restore it exactly as it was.");
        }

        private void ThenTheSecretOptionsAre(string[] expected)
        {
            var readable = new SecretStateClassifier(keyInUse);
            var seen = ReadRows(
                """
                SELECT c.Name, o.Key, o.Value FROM WorkTrackingSystemConnectionOption o
                JOIN WorkTrackingSystemConnections c ON c.Id = o.WorkTrackingSystemConnectionId
                WHERE o.IsSecret = 1 ORDER BY c.Name, o.Key;
                """)
                .Select(row => $"{row[0]}|{row[1]}|{Describe(readable, row[2])}");

            Assert.That(seen, Is.EqualTo(expected),
                "The secret options after removal are not as expected (connection|key|value: empty, readable or unreadable). A value " +
                "this key cannot read must be blanked; a readable value and an empty value must be left exactly as they were.");
        }

        private void ThenThePlainOptionsAreKept(string[] connections)
        {
            Assert.That(Read(
                """
                SELECT c.Name FROM WorkTrackingSystemConnectionOption o
                JOIN WorkTrackingSystemConnections c ON c.Id = o.WorkTrackingSystemConnectionId
                WHERE o.IsSecret = 0 AND o.Value = 'contoso' ORDER BY c.Name;
                """), Is.EqualTo(connections),
                "A plain option was removed or changed. Removal touches only secret values this key cannot read.");
        }

        private void ThenOAuthCredentialsAreLeftOnlyFor(string[] connections)
        {
            Assert.That(Read(
                """
                SELECT c.Name FROM OAuthCredentials o
                JOIN WorkTrackingSystemConnections c ON c.Id = o.WorkTrackingSystemConnectionId ORDER BY c.Name;
                """), Is.EqualTo(connections),
                "The OAuth credentials left are not as expected. Delete a credential when its access token or its refresh token cannot " +
                "be read with this key, and keep one whose tokens are both readable.");
        }

        private static string Describe(SecretStateClassifier readable, string value)
        {
            if (value.Length == 0)
            {
                return string.Empty;
            }

            return readable.Classify(value).State == SecretState.Unreadable ? "unreadable" : "readable";
        }

        private SqliteConnection LiveConnection() => new SqliteConnection($"Data Source={harness.DatabasePath};Pooling=False;Mode=ReadOnly");

        private List<string> LiveRows()
            =>
            [
                .. Read("SELECT Description FROM BlackoutPeriods ORDER BY Description;"),
                .. Read("SELECT Name FROM WorkTrackingSystemConnections ORDER BY Name;"),
                .. Read("SELECT Key || '=' || Value FROM WorkTrackingSystemConnectionOption ORDER BY Key, Value;"),
                .. Read("SELECT AccessToken || '|' || RefreshToken FROM OAuthCredentials ORDER BY AccessToken;"),
            ];

        private List<string> Read(string query) => [.. ReadRows(query).Select(row => row[0])];

        private List<string[]> ReadRows(string query)
        {
            using var connection = LiveConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = query;
            using var reader = command.ExecuteReader();

            var rows = new List<string[]>();
            while (reader.Read())
            {
                rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(reader.GetString)]);
            }

            return rows;
        }

        private void Write(string statement, params (string Name, string Value)[] parameters)
        {
            using var connection = new SqliteConnection($"Data Source={harness.DatabasePath};Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = statement;
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            command.ExecuteNonQuery();
        }
    }
}
