using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// The number an operator who has just upgraded is shown without asking for it. Counted by looking at
    /// the stored values rather than by decrypting them, so it is tested against a real database: the whole
    /// question is what a relational engine makes of the two shapes being counted.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class PublishedKeySecretCountTests
    {
        private const string Contoso = "Contoso Board";

        private static readonly EncryptionKey OwnKey = new("k-2026-08-16-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

        private static readonly EncryptionKey PublishedKey = PublishedKeyOf(
            new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, OwnKey).WithLegacyDefault());

        private string databaseFile = null!;

        private ServiceProvider provider = null!;

        private int connectionId;

        private int credentialId;

        [SetUp]
        public async Task SetUp()
        {
            databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-published-count-{Guid.NewGuid():N}.db");
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
        public async Task AnInstanceHoldingNothingUnderThePublishedKey_CountsZero()
        {
            Assert.That(await CountAsync(), Is.Zero,
                "a fresh install has no such problem and must not be told to fix one");
        }

        [Test]
        public async Task ASecretUnderThePublishedKey_IsCounted()
        {
            await StoreOptionAsync("ClientSecret", Under(PublishedKey, "on-the-published-key"));

            Assert.That(await CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task ASecretInTheFormatThisVersionReplaced_IsCounted()
        {
            await StoreOptionAsync("ClientSecret", InTheFormatThisVersionReplaced("written-before-the-envelope"));

            Assert.That(await CountAsync(), Is.EqualTo(1),
                "an upgraded install carries no key id on its stored values at all, and the published key is the only one that ever read them");
        }

        [Test]
        public async Task ASecretOnTheInstancesOwnKey_IsNotCounted()
        {
            await StoreOptionAsync("ClientSecret", Under(OwnKey, "already-moved"));

            Assert.That(await CountAsync(), Is.Zero);
        }

        [Test]
        public async Task AValueThatIsNotASecret_IsNotCounted()
        {
            await StoreOptionAsync("Url", InTheFormatThisVersionReplaced("not-a-secret"), isSecret: false);

            Assert.That(await CountAsync(), Is.Zero,
                "a Connection's URL is not a credential, so it is not something an operator is asked to move");
        }

        [Test]
        public async Task AnEmptyStoredValue_IsNotCounted()
        {
            await StoreOptionAsync("ClientSecret", string.Empty);

            Assert.That(await CountAsync(), Is.Zero,
                "an empty column holds no credential, so counting it would send an operator after nothing");
        }

        [Test]
        public async Task AnAccessTokenUnderThePublishedKey_IsCounted()
        {
            await StoreCredentialAsync(Under(PublishedKey, "the-access-token"), Under(OwnKey, "the-refresh-token"));

            Assert.That(await CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task ARefreshTokenUnderThePublishedKey_IsCounted()
        {
            await StoreCredentialAsync(Under(OwnKey, "the-access-token"), Under(PublishedKey, "the-refresh-token"));

            Assert.That(await CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task BothTokensOfOneCredential_AreCountedSeparately()
        {
            await StoreCredentialAsync(Under(PublishedKey, "the-access-token"), Under(PublishedKey, "the-refresh-token"));

            Assert.That(await CountAsync(), Is.EqualTo(2),
                "two credentials are two things to move, and a row is not the unit an operator acts on");
        }

        [Test]
        public async Task AnEmptyToken_IsNotCounted()
        {
            await StoreCredentialAsync(string.Empty, string.Empty);

            Assert.That(await CountAsync(), Is.Zero);
        }

        [Test]
        public async Task ATokenInTheFormatThisVersionReplaced_IsCounted()
        {
            await StoreCredentialAsync(
                InTheFormatThisVersionReplaced("the-access-token"),
                InTheFormatThisVersionReplaced("the-refresh-token"));

            Assert.That(await CountAsync(), Is.EqualTo(2));
        }

        [Test]
        public async Task EverythingStoredIsCountedTogether_AcrossOptionsAndTokens()
        {
            await StoreOptionAsync("ClientSecret", Under(PublishedKey, "one"));
            await StoreOptionAsync("PersonalAccessToken", InTheFormatThisVersionReplaced("two"));
            await StoreCredentialAsync(Under(PublishedKey, "three"), Under(PublishedKey, "four"));

            Assert.That(await CountAsync(), Is.EqualTo(4),
                "the count an operator is shown is every stored credential still on that key, wherever it is kept");
        }

        [Test]
        public async Task ASecretInTheFormatThisVersionReplacedWrittenUnderTheInstancesOwnKey_IsNotCounted()
        {
            await StoreOptionAsync("ClientSecret", InTheFormatThisVersionReplaced("never-was-public", OwnKey));

            Assert.That(await CountAsync(), Is.Zero,
                "an install that set a key of its own before this release is told its credentials are public, and they never were");
        }

        [Test]
        public async Task ATokenInTheFormatThisVersionReplacedWrittenUnderTheInstancesOwnKey_IsNotCounted()
        {
            await StoreCredentialAsync(
                InTheFormatThisVersionReplaced("the-access-token", OwnKey),
                InTheFormatThisVersionReplaced("the-refresh-token", OwnKey));

            Assert.That(await CountAsync(), Is.Zero);
        }

        [Test]
        public async Task AValueThatWasNeverEncryptedAtAll_IsNotCounted()
        {
            await StoreOptionAsync("ClientSecret", "not encrypted at all");

            Assert.That(await CountAsync(), Is.Zero,
                "that is a different problem, and the check reports it in its own state rather than as an exposure to this key");
        }

        [Test]
        public async Task AMixtureOfShapes_CountsOnlyWhatThatKeyCanRead()
        {
            await StoreOptionAsync("ClientSecret", InTheFormatThisVersionReplaced("public", PublishedKey));
            await StoreOptionAsync("PersonalAccessToken", InTheFormatThisVersionReplaced("private", OwnKey));
            await StoreOptionAsync("ApiKey", Under(OwnKey, "already-moved"));
            await StoreCredentialAsync(Under(PublishedKey, "still-public"), "not encrypted at all");

            Assert.That(await CountAsync(), Is.EqualTo(2),
                "two of the five are readable with that key, and no shape any of the other three wears changes that");
        }

        [Test]
        public void TheCount_RefusesToBeBuiltWithoutSomewhereToLook()
        {
            Assert.That(() => new PublishedKeySecretCount(null!), Throws.ArgumentNullException);
        }

        private async Task<int> CountAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await new PublishedKeySecretCount(context).CountAsync();
        }

        // Written into the column rather than through a save, because a save encrypts anything that is not
        // already an envelope - which is exactly the shape half of these tests are about.
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
        // and no key id anywhere on the value. The key it was written under is a parameter because that is
        // the whole point - two values written under different keys are indistinguishable by shape.
        private static string InTheFormatThisVersionReplaced(string credential, EncryptionKey? writtenUnder = null)
        {
            using var aes = Aes.Create();
            aes.Key = (writtenUnder ?? PublishedKey).Material.ToArray();

            var iv = RandomNumberGenerator.GetBytes(16);
            var cipherText = aes.EncryptCbc(System.Text.Encoding.UTF8.GetBytes(credential), iv);

            return Convert.ToBase64String([.. iv, .. cipherText]);
        }

        private static EncryptionKey PublishedKeyOf(EncryptionKeyRing ring)
        {
            ring.TryGet(LegacyDefaultEncryptionKey.Id, out var published);

            return published!;
        }

        // Seeded with nothing under the published key and both tokens empty, so an instance counts zero
        // until a test deliberately puts something there.
        private static async Task<(int ConnectionId, int CredentialId)> SeedAsync(LighthouseAppContext context)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = Contoso,
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
            services.AddSingleton<ICryptoService>(new CryptoService(
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, OwnKey).WithLegacyDefault()),
                NullLogger<CryptoService>.Instance));
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }
    }
}
