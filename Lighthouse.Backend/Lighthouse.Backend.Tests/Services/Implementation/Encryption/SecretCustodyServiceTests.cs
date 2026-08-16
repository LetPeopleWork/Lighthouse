using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// The only thing in this feature that writes a stored credential, so it is tested against a real
    /// database rather than an in-memory stand-in: the guarded write is the entire mechanism, and a
    /// simulated one would prove the simulation. The in-memory provider does not implement it at all.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class SecretCustodyServiceTests
    {
        private const string PersonalAccessToken = "PersonalAccessToken";

        private const string ClientSecret = "ClientSecret";

        private const string Contoso = "Contoso Board";

        private static readonly EncryptionKey OldKey = new("k-2025-11-02-01", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly EncryptionKey NewKey = new("k-2026-08-16-01", Convert.FromBase64String("Zm9vYmFyYmF6cXV4MTIzNDU2Nzg5MGFiY2RlZmdoaWo="));

        private static readonly EncryptionKey KeyNobodyHolds = new("k-lost-forever", Convert.FromBase64String("bG9zdGtleWxvc3RrZXlsb3N0a2V5bG9zdGtleWxvc3Q="));

        private string databaseFile = null!;

        private ServiceProvider provider = null!;

        private CryptoService crypto = null!;

        private int connectionId;

        private int credentialId;

        [SetUp]
        public async Task SetUp()
        {
            databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-custody-{Guid.NewGuid():N}.db");
            crypto = CryptoOver(NewKey, OldKey);
            provider = BuildProvider(databaseFile, crypto);

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            (connectionId, credentialId) = await SeedAsync(context);
        }

        [TearDown]
        public async Task TearDown()
        {
            await provider.DisposeAsync();
            SqliteConnection.ClearAllPools();

            if (File.Exists(databaseFile))
            {
                File.Delete(databaseFile);
            }
        }

        [Test]
        public async Task ASecretUnderARetiredKey_IsMovedOntoTheKeyInForce_AndStillReadsAsTheSameCredential()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var report = await CustodyService().ReEncryptAsync();

            var stored = await StoredOptionAsync(PersonalAccessToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(1));
                Assert.That(crypto.Read(stored).KeyId, Is.EqualTo(NewKey.Id));
                Assert.That(crypto.Read(stored).PlainText, Is.EqualTo("contoso-pat"));
            }
        }

        [Test]
        public async Task ASecretAlreadyUnderTheKeyInForce_IsNotACandidate()
        {
            await StoreAsync(PersonalAccessToken, Under(NewKey, "contoso-pat"));
            var before = await StoredOptionAsync(PersonalAccessToken);

            var report = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Is.Empty,
                    "asking the database for what is left to do is the whole of the resumability, so a finished instance has nothing to walk");
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(before));
            }
        }

        [Test]
        public async Task ASecretNobodyCanRead_IsLeftByteForByte_AndIsNamedByConnectionAndField()
        {
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));

            var before = await StoredOptionAsync(ClientSecret);
            var report = await CustodyService().ReEncryptAsync();

            var named = report.Secrets.Single(secret => secret.Outcome == SecretMoveOutcome.CouldNotBeRead);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await StoredOptionAsync(ClientSecret), Is.EqualTo(before),
                    "a value nobody can decrypt is a value nobody can re-encrypt, and writing over it destroys the only copy");
                Assert.That(named.ConnectionName, Is.EqualTo(Contoso));
                Assert.That(named.Field, Is.EqualTo(ClientSecret));
                Assert.That(report.MovedCount, Is.EqualTo(1), "the rest of the secrets are still moved");
                Assert.That(report.UnreadableCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task AValueThatWasNeverEncrypted_IsLeftAlone_AndReportedRatherThanEncrypted()
        {
            await StoreAsync(ClientSecret, "a-secret-somebody-typed-in-before-any-of-this");

            var before = await StoredOptionAsync(ClientSecret);
            var report = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await StoredOptionAsync(ClientSecret), Is.EqualTo(before));
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Outcome == SecretMoveOutcome.NotEncrypted));
                Assert.That(report.MovedCount, Is.Zero);
                Assert.That(report.UnreadableCount, Is.Zero);
            }
        }

        [Test]
        public async Task ARowSomebodyElseRewroteInBetween_IsCountedAsAlreadyMoved_AndThePassCarriesOn()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            var refreshed = Under(NewKey, "written-by-somebody-else");
            var interfering = new WriterThatGetsThereFirst(crypto, async () => await StoreAsync(PersonalAccessToken, refreshed));

            var report = await CustodyService(interfering).ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(refreshed),
                    "the write the pass would have made is exactly the one that must not happen");
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Outcome == SecretMoveOutcome.MovedByAnotherWriter));
                Assert.That(report.UnreadableCount, Is.Zero, "a row somebody else wrote is not a failure");
                Assert.That(report.Secrets, Has.Count.EqualTo(2), "the pass carries on to the rest");
            }
        }

        [Test]
        public async Task RunningItAgain_MovesNothing_AndReportsTheSameTotals()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));

            await CustodyService().ReEncryptAsync();

            var second = await CustodyService().ReEncryptAsync();
            var storedAfterSecond = await StoredOptionAsync(PersonalAccessToken);
            var third = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(second.MovedCount, Is.Zero);
                Assert.That(third.MovedCount, Is.Zero);
                Assert.That(third.UnreadableCount, Is.EqualTo(second.UnreadableCount));
                Assert.That(await StoredOptionAsync(PersonalAccessToken), Is.EqualTo(storedAfterSecond));
            }
        }

        [Test]
        public async Task ACancelledRun_LeavesEverySecretReadable_AndARerunCompletesTheRemainder()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(OldKey, "contoso-secret"));

            using var stopAfterTheFirst = new CancellationTokenSource();
            var interfering = new WriterThatGetsThereFirst(
                crypto,
                () =>
                {
                    stopAfterTheFirst.Cancel();
                    return Task.CompletedTask;
                },
                letThrough: 1);

            Assert.That(
                async () => await CustodyService(interfering).ReEncryptAsync(stopAfterTheFirst.Token),
                Throws.InstanceOf<OperationCanceledException>());

            var everyStoredSecret = new[]
            {
                await StoredOptionAsync(PersonalAccessToken),
                await StoredOptionAsync(ClientSecret),
            };

            var completing = await CustodyService().ReEncryptAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(everyStoredSecret.Select(stored => crypto.Read(stored).State),
                    Is.All.EqualTo(SecretState.Envelope),
                    "both keys are held, so an interrupted pass leaves every credential readable whichever key it is under");
                Assert.That(completing.MovedCount, Is.EqualTo(1), "the second run moves exactly what the first one did not");
                Assert.That(crypto.Read(await StoredOptionAsync(ClientSecret)).PlainText, Is.EqualTo("contoso-secret"));
            }
        }

        [Test]
        public async Task Inspecting_LeavesEveryStoredValueExactlyAsItFoundIt()
        {
            await StoreAsync(PersonalAccessToken, Under(OldKey, "contoso-pat"));
            await StoreAsync(ClientSecret, Under(KeyNobodyHolds, "unrecoverable"));

            var before = new[]
            {
                await StoredOptionAsync(PersonalAccessToken),
                await StoredOptionAsync(ClientSecret),
            };

            var report = await CustodyService().InspectAsync();

            var after = new[]
            {
                await StoredOptionAsync(PersonalAccessToken),
                await StoredOptionAsync(ClientSecret),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after, Is.EqualTo(before).AsCollection);
                Assert.That(report.MovedCount, Is.Zero);
                Assert.That(report.UnreadableCount, Is.EqualTo(1), "looking still says what cannot be read");
            }
        }

        [Test]
        public async Task TheStoredOAuthTokens_AreMovedToo_AndNamedByTheirOwnFields()
        {
            await StoreCredentialAsync(Under(OldKey, "the-access-token"), Under(OldKey, "the-refresh-token"));

            var report = await CustodyService().ReEncryptAsync();

            var credential = await StoredCredentialAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(2));
                Assert.That(crypto.Read(credential.AccessToken).PlainText, Is.EqualTo("the-access-token"));
                Assert.That(crypto.Read(credential.RefreshToken).PlainText, Is.EqualTo("the-refresh-token"));
                Assert.That(report.Secrets.Select(secret => secret.Field),
                    Is.EquivalentTo(new[] { SecretCustodyService.AccessTokenField, SecretCustodyService.RefreshTokenField }));
            }
        }

        private SecretCustodyService CustodyService(ICryptoService? cryptoService = null)
        {
            var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return new SecretCustodyService(
                context,
                cryptoService ?? crypto,
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, NewKey, OldKey)));
        }

        // Written straight into the column rather than through a save, because a save encrypts anything it
        // cannot already read - which is exactly the shapes these tests are about.
        private async Task StoreAsync(string field, string storedValue)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            await context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.WorkTrackingSystemConnectionId == connectionId && option.Key == field)
                .ExecuteUpdateAsync(set => set.SetProperty(option => option.Value, storedValue));
        }

        private async Task StoreCredentialAsync(string accessToken, string refreshToken)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            await context.Set<OAuthCredential>()
                .Where(credential => credential.Id == credentialId)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(credential => credential.AccessToken, accessToken)
                    .SetProperty(credential => credential.RefreshToken, refreshToken));
        }

        private async Task<string> StoredOptionAsync(string field)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await context.Set<WorkTrackingSystemConnectionOption>()
                .AsNoTracking()
                .Where(option => option.WorkTrackingSystemConnectionId == connectionId && option.Key == field)
                .Select(option => option.Value)
                .SingleAsync();
        }

        private async Task<OAuthCredential> StoredCredentialAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await context.Set<OAuthCredential>().AsNoTracking().SingleAsync(credential => credential.Id == credentialId);
        }

        private static string Under(EncryptionKey key, string credential)
        {
            return SecretEnvelope.Protect(credential, key.Id, key.Material.Span).Format();
        }

        private static CryptoService CryptoOver(params EncryptionKey[] keys)
        {
            return new CryptoService(
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, keys)),
                NullLogger<CryptoService>.Instance);
        }

        // Seeded with values already under the key in force, so nothing is a candidate until a test
        // deliberately puts one there.
        private async Task<(int ConnectionId, int CredentialId)> SeedAsync(LighthouseAppContext context)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = Contoso,
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = PersonalAccessToken, Value = Under(NewKey, "seed"), IsSecret = true });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = ClientSecret, Value = Under(NewKey, "seed"), IsSecret = true });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Url", Value = "https://dev.azure.com/contoso", IsSecret = false });

            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = Under(NewKey, "seed"),
                RefreshToken = Under(NewKey, "seed"),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            context.Set<OAuthCredential>().Add(credential);
            await context.SaveChangesAsync();

            return (connection.Id, credential.Id);
        }

        private static ServiceProvider BuildProvider(string databaseFile, ICryptoService cryptoService)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(cryptoService);
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite($"Data Source={databaseFile}", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Stands where a token refresh would: the pass encrypts the value it read immediately before it
        /// writes, so this is the one moment at which somebody else can get to the row first.
        /// </summary>
        private sealed class WriterThatGetsThereFirst : ICryptoService
        {
            private readonly ICryptoService inner;

            private readonly Func<Task> getThereFirst;

            private readonly int letThrough;

            private int reached;

            public WriterThatGetsThereFirst(ICryptoService inner, Func<Task> getThereFirst, int letThrough = 0)
            {
                this.inner = inner;
                this.getThereFirst = getThereFirst;
                this.letThrough = letThrough;
            }

            public string Encrypt(string plainText)
            {
                if (reached++ == letThrough)
                {
                    getThereFirst().GetAwaiter().GetResult();
                }

                return inner.Encrypt(plainText);
            }

            public string Decrypt(string cipherText)
            {
                return inner.Decrypt(cipherText);
            }

            public SecretReadResult Read(string storedValue)
            {
                return inner.Read(storedValue);
            }
        }
    }
}
