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
using System.Security.Cryptography;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// The number that decides whether the settings page offers to move anything. Tested against a real
    /// database because the narrowing happens there, and against real cryptography because the whole point
    /// is that the shape of a stored value cannot answer the question on its own.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class ReadableSecretsNotOnTheActiveKeyTests
    {
        private static readonly EncryptionKey KeyInForce = new("k-2026-08-17-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

        private static readonly EncryptionKey RetiredKey = new("k-2025-11-02-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

        private static readonly EncryptionKey KeyNobodyHolds = new("k-lost-2024-01-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

        private static readonly EncryptionKeyRing Ring = new EncryptionKeyRing(
            KeyCustody.GeneratedForThisInstance, KeyInForce, RetiredKey).WithLegacyDefault();

        private static readonly EncryptionKey PublishedKey = PublishedKeyOf(Ring);

        private string databaseFile = null!;

        private ServiceProvider provider = null!;

        private int connectionId;

        private int credentialId;

        [SetUp]
        public async Task SetUp()
        {
            databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-movable-count-{Guid.NewGuid():N}.db");
            provider = BuildProvider($"Data Source={databaseFile}");

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
        public async Task AnInstanceWithEverythingOnTheKeyInForce_CountsZero()
        {
            await StoreOptionAsync("ClientSecret", Under(KeyInForce, "already-moved"));

            Assert.That(await CountAsync(), Is.Zero,
                "an instance with nothing to move must not be offered a button that would change nothing");
        }

        // The defect this class exists for, observed on a real deployment on 2026-08-17: a credential written
        // before the envelope format names no key, so nothing about the stored value says it is on another one.
        [Test]
        public async Task ASecretWrittenBeforeTheEnvelopeFormat_IsCounted()
        {
            await StoreOptionAsync("ClientSecret", InTheFormatThisVersionReplaced("written-before-the-envelope"));

            Assert.That(await CountAsync(), Is.EqualTo(1),
                "it is readable and it is not on the key in force, which is the whole of the question");
        }

        [Test]
        public async Task ASecretUnderAKeyTheRingStillHolds_IsCounted()
        {
            await StoreOptionAsync("ClientSecret", Under(RetiredKey, "on-an-earlier-key"));

            Assert.That(await CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task ASecretUnderAKeyNobodyHolds_IsNotCounted()
        {
            await StoreOptionAsync("ClientSecret", Under(KeyNobodyHolds, "gone"));

            Assert.That(await CountAsync(), Is.Zero,
                "nothing can move a value nobody can read, and the check reports it as unreadable instead");
        }

        [Test]
        public async Task AValueThatWasNeverEncrypted_IsNotCounted()
        {
            await StoreOptionAsync("ClientSecret", "not encrypted at all");

            Assert.That(await CountAsync(), Is.Zero,
                "the pass leaves those where they are and names them for re-entry, so a move would promise something it does not do");
        }

        [Test]
        public async Task AValueThatIsNotASecret_IsNotCounted()
        {
            await StoreOptionAsync("Url", InTheFormatThisVersionReplaced("not-a-secret"), isSecret: false);

            Assert.That(await CountAsync(), Is.Zero);
        }

        [Test]
        public async Task AnEmptyStoredValue_IsNotCounted()
        {
            await StoreOptionAsync("ClientSecret", string.Empty);

            Assert.That(await CountAsync(), Is.Zero);
        }

        [Test]
        public async Task BothTokensOfOneCredential_AreCountedSeparately()
        {
            await StoreCredentialAsync(
                InTheFormatThisVersionReplaced("the-access-token"),
                Under(RetiredKey, "the-refresh-token"));

            Assert.That(await CountAsync(), Is.EqualTo(2),
                "two credentials are two things to move, whichever shape each of them wears");
        }

        [Test]
        public async Task AMixtureOfShapes_CountsOnlyWhatCanBeMoved()
        {
            await StoreOptionAsync("ClientSecret", InTheFormatThisVersionReplaced("legacy"));
            await StoreOptionAsync("PersonalAccessToken", Under(RetiredKey, "earlier-key"));
            await StoreOptionAsync("ApiKey", Under(KeyInForce, "already-moved"));
            await StoreCredentialAsync(Under(KeyNobodyHolds, "unreadable"), "not encrypted at all");

            Assert.That(await CountAsync(), Is.EqualTo(2),
                "two of the five could be moved; the other three are already there, gone, or a different problem");
        }

        [Test]
        public async Task ASecretWrittenUnderThePublishedKeyBeforeTheEnvelope_IsCounted()
        {
            await StoreOptionAsync("ClientSecret", InTheFormatThisVersionReplaced("public", PublishedKey));

            Assert.That(await CountAsync(), Is.EqualTo(1),
                "this is the population the whole feature exists to move, and it must be offered the move");
        }

        [Test]
        public void TheCount_RefusesToBeBuiltWithoutEverythingItNeeds()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => new ReadableSecretsNotOnTheActiveKey(null!, CryptoFor(Ring), HolderFor(Ring)), Throws.ArgumentNullException);
                Assert.That(() => new ReadableSecretsNotOnTheActiveKey(ContextForGuardTest(), null!, HolderFor(Ring)), Throws.ArgumentNullException);
                Assert.That(() => new ReadableSecretsNotOnTheActiveKey(ContextForGuardTest(), CryptoFor(Ring), null!), Throws.ArgumentNullException);
            }
        }

        private LighthouseAppContext ContextForGuardTest()
        {
            var scope = provider.CreateScope();

            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
        }

        private async Task<int> CountAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await new ReadableSecretsNotOnTheActiveKey(context, CryptoFor(Ring), HolderFor(Ring)).CountAsync();
        }

        // Written into the column rather than through a save, because a save encrypts anything that is not
        // already an envelope - which is exactly the shape most of these tests are about.
        private async Task StoreOptionAsync(string field, string storedValue, bool isSecret = true)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.Set<WorkTrackingSystemConnectionOption>().Add(new WorkTrackingSystemConnectionOption
            {
                WorkTrackingSystemConnectionId = connectionId,
                Key = field,
                Value = "placeholder",
                IsSecret = isSecret,
            });

            await context.SaveChangesAsync();

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

        private static string Under(EncryptionKey key, string credential)
        {
            return SecretEnvelope.Protect(credential, key.Id, key.Material.Span).Format();
        }

        // What every install written before this release holds: AES-CBC, an initialisation vector in front,
        // and no key id anywhere on the value.
        private static string InTheFormatThisVersionReplaced(string credential, EncryptionKey? writtenUnder = null)
        {
            using var aes = Aes.Create();
            aes.Key = (writtenUnder ?? RetiredKey).Material.ToArray();

            var iv = RandomNumberGenerator.GetBytes(16);
            var cipherText = aes.EncryptCbc(System.Text.Encoding.UTF8.GetBytes(credential), iv);

            return Convert.ToBase64String([.. iv, .. cipherText]);
        }

        private static CryptoService CryptoFor(EncryptionKeyRing ring)
        {
            return new CryptoService(HolderFor(ring), NullLogger<CryptoService>.Instance);
        }

        private static EncryptionKeyRingHolder HolderFor(EncryptionKeyRing ring)
        {
            return new EncryptionKeyRingHolder(ring);
        }

        private static EncryptionKey PublishedKeyOf(EncryptionKeyRing ring)
        {
            ring.TryGet(LegacyDefaultEncryptionKey.Id, out var published);

            return published!;
        }

        private static async Task<(int ConnectionId, int CredentialId)> SeedAsync(LighthouseAppContext context)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Contoso Board",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            context.Set<OAuthCredential>().Add(credential);
            await context.SaveChangesAsync();

            return (connection.Id, credential.Id);
        }

        private static ServiceProvider BuildProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService>(CryptoFor(Ring));
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }
    }
}
