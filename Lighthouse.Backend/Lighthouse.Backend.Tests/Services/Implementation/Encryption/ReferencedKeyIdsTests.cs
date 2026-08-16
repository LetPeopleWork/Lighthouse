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
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// Which keys the stored secrets say wrote them. Asked against a real database because the whole
    /// question is what a relational engine makes of the shapes an install can be holding, and answered
    /// by reading the front of each value rather than by decrypting any of them.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class ReferencedKeyIdsTests
    {
        private const string Contoso = "Contoso Board";

        private static readonly EncryptionKey KeyInForce = new("k-2026-08-16-02", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

        private static readonly EncryptionKey AnEarlierKey = new("k-2025-11-02-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

        private string databaseFile = null!;

        private ServiceProvider provider = null!;

        private int connectionId;

        private int credentialId;

        [SetUp]
        public async Task SetUp()
        {
            databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-referenced-keys-{Guid.NewGuid():N}.db");
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
        public async Task AnInstanceHoldingNothing_NamesNoKeyAtAll()
        {
            Assert.That(await ReadAsync(), Is.Empty,
                "a first install has written nothing, so there is no key it can point at as one that matters");
        }

        [Test]
        public async Task SecretsUnderOneKey_NameThatKey()
        {
            await StoreOptionAsync("ClientSecret", Under(KeyInForce, "one"));
            await StoreOptionAsync("PersonalAccessToken", Under(KeyInForce, "two"));

            Assert.That(await ReadAsync(), Is.EquivalentTo(new[] { KeyInForce.Id }),
                "two secrets under one key are one key, not two");
        }

        [Test]
        public async Task SecretsUnderTwoKeys_NameBoth()
        {
            await StoreOptionAsync("ClientSecret", Under(KeyInForce, "current"));
            await StoreOptionAsync("PersonalAccessToken", Under(AnEarlierKey, "older"));

            Assert.That(await ReadAsync(), Is.EquivalentTo(new[] { KeyInForce.Id, AnEarlierKey.Id }));
        }

        [Test]
        public async Task AValueInTheFormatThisVersionReplaced_NamesNoKey()
        {
            await StoreOptionAsync("ClientSecret", InTheFormatThisVersionReplaced("written-before-the-envelope"));

            Assert.That(await ReadAsync(), Is.Empty,
                "nothing on that value says which key wrote it, and naming one would send an operator after a key that may never have existed");
        }

        [Test]
        public async Task AValueThatWasNeverASecret_IsNotConsulted()
        {
            await StoreOptionAsync("Url", Under(AnEarlierKey, "not-a-credential"), isSecret: false);

            Assert.That(await ReadAsync(), Is.Empty,
                "a Connection's URL is not a credential, so whatever it holds says nothing about which keys matter");
        }

        [Test]
        public async Task AnEmptyStoredValue_NamesNoKey()
        {
            await StoreOptionAsync("ClientSecret", string.Empty);

            Assert.That(await ReadAsync(), Is.Empty);
        }

        [Test]
        public async Task BothTokenColumns_AreConsulted()
        {
            await StoreCredentialAsync(Under(KeyInForce, "the-access-token"), Under(AnEarlierKey, "the-refresh-token"));

            Assert.That(await ReadAsync(), Is.EquivalentTo(new[] { KeyInForce.Id, AnEarlierKey.Id }),
                "a credential is two stored secrets, and either one can be the last thing holding a key in use");
        }

        [Test]
        public async Task TheSameKeyAcrossOptionsAndTokens_IsNamedOnce()
        {
            await StoreOptionAsync("ClientSecret", Under(KeyInForce, "one"));
            await StoreCredentialAsync(Under(KeyInForce, "two"), Under(KeyInForce, "three"));

            Assert.That(await ReadAsync(), Is.EquivalentTo(new[] { KeyInForce.Id }));
        }

        [Test]
        public void TheReader_RefusesToBeBuiltWithoutSomewhereToLook()
        {
            Assert.That(() => new ReferencedKeyIds(null!), Throws.ArgumentNullException);
        }

        private async Task<IReadOnlyCollection<string>> ReadAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return await new ReferencedKeyIds(context).ReadAsync();
        }

        // Written into the column rather than through a save, because a save encrypts anything that is
        // not already an envelope - which is exactly the shape half of these tests are about.
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

        private static string InTheFormatThisVersionReplaced(string credential)
        {
            using var aes = Aes.Create();
            aes.Key = KeyInForce.Material.ToArray();

            var iv = RandomNumberGenerator.GetBytes(16);

            return Convert.ToBase64String([.. iv, .. aes.EncryptCbc(Encoding.UTF8.GetBytes(credential), iv)]);
        }

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

            // Blanked after the save, not before it. A save encrypts anything that is not already an
            // envelope, and an empty token is not one - so seeding "nothing stored" through SaveChanges
            // leaves two real envelopes under the key in force, and every test that asserts an instance
            // holds nothing would be asserting against a fixture that had quietly stored something.
            await context.Set<OAuthCredential>()
                .Where(seeded => seeded.Id == credential.Id)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(seeded => seeded.AccessToken, string.Empty)
                    .SetProperty(seeded => seeded.RefreshToken, string.Empty));

            return (connection.Id, credential.Id);
        }

        private static ServiceProvider BuildProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService>(new CryptoService(
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, KeyInForce, AnEarlierKey)),
                NullLogger<CryptoService>.Instance));
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }
    }
}
