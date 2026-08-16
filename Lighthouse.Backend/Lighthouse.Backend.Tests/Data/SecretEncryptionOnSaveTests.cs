using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class SecretEncryptionOnSaveTests
    {
        private const string Credential = "pat-7f3c9a2e-not-a-real-token";

        private const string ReEnteredCredential = "pat-0b41d8c6-also-not-a-real-token";

        private const string AccessToken = "access-2d5e91b4-not-a-real-token";

        private const string RefreshToken = "refresh-6c1a83f0-not-a-real-token";

        private const string ReEnteredRefreshToken = "refresh-9e7b25da-also-not-a-real-token";

        private const string SecretOptionKey = "PersonalAccessToken";

        private static readonly EncryptionKey ActiveKey = new("key-active", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly EncryptionKey RetiredKey = new("key-retired", Convert.FromBase64String("BdurmHjAsvICR2wy2rjw3ao+2NW/s0TOIf85FOdjx+c="));

        private static readonly EncryptionKey StrangerKey = new("key-stranger", Convert.FromBase64String("2gMy+eBfMpbvIUlN9fyFHwpBNlNBpw+SVuAOmMVXsaE="));

        private readonly Mock<ILogger<LighthouseAppContext>> logger = new();

        private DbContextOptions<LighthouseAppContext> options = null!;

        [SetUp]
        public void Setup()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = NewContext(CryptoUnder(ActiveKey, RetiredKey));
            context.Database.EnsureCreated();
        }

        [Test]
        public async Task AddedSecretOption_IsEncryptedExactlyOnce()
        {
            var connectionId = await SeedConnectionWithSecret(Credential);

            var stored = await StoredSecret(connectionId);
            var read = CryptoUnder(ActiveKey, RetiredKey).Read(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(read.PlainText, Is.EqualTo(Credential));
            }
        }

        [Test]
        public async Task ModifiedSecretOption_WhoseValueWasNotReEntered_IsLeftByteIdentical()
        {
            var connectionId = await SeedConnectionWithSecret(Credential);
            var beforeSave = await StoredSecret(connectionId);

            await ResaveSecretOption(connectionId, reEnteredValue: null);

            var afterSave = await StoredSecret(connectionId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterSave, Is.EqualTo(beforeSave));
                Assert.That(CryptoUnder(ActiveKey, RetiredKey).Read(afterSave).PlainText, Is.EqualTo(Credential));
            }
        }

        [Test]
        public async Task ModifiedSecretOption_WhoseValueWasReEntered_IsEncryptedExactlyOnce()
        {
            var connectionId = await SeedConnectionWithSecret(Credential);
            var beforeSave = await StoredSecret(connectionId);

            await ResaveSecretOption(connectionId, ReEnteredCredential);

            var afterSave = await StoredSecret(connectionId);
            var read = CryptoUnder(ActiveKey, RetiredKey).Read(afterSave);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterSave, Is.Not.EqualTo(beforeSave));
                Assert.That(read.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(read.PlainText, Is.EqualTo(ReEnteredCredential));
            }
        }

        [Test]
        public async Task SecretOptionWrittenUnderARetiredKey_IsLeftByteIdenticalRatherThanWrappedAgain()
        {
            var connectionId = await SeedConnectionWithSecret(Credential, CryptoUnder(RetiredKey));
            var beforeSave = await StoredSecret(connectionId);

            await ResaveSecretOption(connectionId, reEnteredValue: null);

            var afterSave = await StoredSecret(connectionId);
            var read = CryptoUnder(ActiveKey, RetiredKey).Read(afterSave);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterSave, Is.EqualTo(beforeSave));
                Assert.That(read.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(read.PlainText, Is.EqualTo(Credential));
            }
        }

        /// <summary>
        /// This value names a key this instance does not hold, so nothing here can unwrap it - but it is
        /// still the credential, and restoring the key store it belongs to brings it back. That is exactly
        /// what a start refusing on an unreadable database tells the operator to do. Encrypting it here
        /// would wrap ciphertext nobody can read inside ciphertext they can, destroy the only copy, and
        /// report the row as healthy from then on, so an ordinary rename of a Connection would be enough to
        /// lose a credential for good.
        /// </summary>
        [Test]
        public async Task SecretOptionNamingAKeyThisInstanceDoesNotHold_IsLeftByteIdentical()
        {
            var connectionId = await SeedConnectionWithSecret(Credential, CryptoUnder(StrangerKey));
            var beforeSave = await StoredSecret(connectionId);

            await ResaveSecretOption(connectionId, reEnteredValue: null);

            var afterSave = await StoredSecret(connectionId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterSave, Is.EqualTo(beforeSave));
                Assert.That(CryptoUnder(ActiveKey, StrangerKey).Read(afterSave).PlainText, Is.EqualTo(Credential),
                    "whoever restores the key store that belongs to this database gets the credential back, and could not if the save had wrapped it");
            }
        }

        /// <summary>
        /// The other half of the same rule, and the reason it is stated as "an envelope naming a key we do
        /// not hold" rather than "anything unreadable". A credential somebody typed in can happen to have
        /// the shape of the format this version replaced, and refusing to encrypt that would store a live
        /// token in the clear - the worse of the two mistakes by far.
        /// </summary>
        [Test]
        public async Task ACredentialShapedLikeOldCiphertext_IsStillEncrypted()
        {
            var credentialThatLooksLikeCiphertext = Convert.ToBase64String(new byte[48]);

            var connectionId = await SeedConnectionWithSecret(credentialThatLooksLikeCiphertext);

            var stored = await StoredSecret(connectionId);
            var read = CryptoUnder(ActiveKey, RetiredKey).Read(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored, Is.Not.EqualTo(credentialThatLooksLikeCiphertext),
                    "a live token stored in the clear because it happened to look like ciphertext is the worse of the two mistakes by far");
                Assert.That(read.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(read.PlainText, Is.EqualTo(credentialThatLooksLikeCiphertext));
            }
        }

        [Test]
        public async Task ModifiedOAuthCredential_WhoseAccessTokenWasNotReEntered_IsLeftByteIdentical()
        {
            var credentialId = await SeedOAuthCredential();
            var beforeSave = await StoredOAuthTokens(credentialId);

            await ResaveOAuthCredential(credentialId, reEnteredRefreshToken: null);

            var afterSave = await StoredOAuthTokens(credentialId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterSave.AccessToken, Is.EqualTo(beforeSave.AccessToken));
                Assert.That(CryptoUnder(ActiveKey, RetiredKey).Read(afterSave.AccessToken).PlainText, Is.EqualTo(AccessToken));
            }
        }

        [Test]
        public async Task ModifiedOAuthCredential_WhoseRefreshTokenWasReEntered_IsEncryptedExactlyOnce()
        {
            var credentialId = await SeedOAuthCredential();
            var beforeSave = await StoredOAuthTokens(credentialId);

            await ResaveOAuthCredential(credentialId, ReEnteredRefreshToken);

            var afterSave = await StoredOAuthTokens(credentialId);
            var read = CryptoUnder(ActiveKey, RetiredKey).Read(afterSave.RefreshToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterSave.RefreshToken, Is.Not.EqualTo(beforeSave.RefreshToken));
                Assert.That(read.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(read.PlainText, Is.EqualTo(ReEnteredRefreshToken));
            }
        }

        private static CryptoService CryptoUnder(params EncryptionKey[] keys)
        {
            return new CryptoService(new EncryptionKeyRingHolder(new EncryptionKeyRing(keys)), Mock.Of<ILogger<CryptoService>>());
        }

        private LighthouseAppContext NewContext(CryptoService cryptoService)
        {
            return new LighthouseAppContext(options, cryptoService, logger.Object);
        }

        private async Task<int> SeedConnectionWithSecret(string secret, CryptoService? writtenBy = null)
        {
            using var context = NewContext(writtenBy ?? CryptoUnder(ActiveKey, RetiredKey));

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Contoso Board",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = SecretOptionKey,
                Value = secret,
                IsSecret = true,
            });

            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            return connection.Id;
        }

        // Mirrors what saving an edited connection does to a secret the operator never retyped: the whole
        // option comes back attached and marked modified, value and all, even though the value is the one
        // that was already there.
        private async Task ResaveSecretOption(int connectionId, string? reEnteredValue)
        {
            using var context = NewContext(CryptoUnder(ActiveKey, RetiredKey));

            var option = await context.Set<WorkTrackingSystemConnectionOption>()
                .SingleAsync(o => o.WorkTrackingSystemConnectionId == connectionId);

            if (reEnteredValue is not null)
            {
                option.Value = reEnteredValue;
            }

            context.Entry(option).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        private async Task<string> StoredSecret(int connectionId)
        {
            using var context = NewContext(CryptoUnder(ActiveKey, RetiredKey));

            var option = await context.Set<WorkTrackingSystemConnectionOption>()
                .AsNoTracking()
                .SingleAsync(o => o.WorkTrackingSystemConnectionId == connectionId);

            return option.Value;
        }

        private async Task<int> SeedOAuthCredential()
        {
            using var context = NewContext(CryptoUnder(ActiveKey, RetiredKey));

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Contoso Jira",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = AccessToken,
                RefreshToken = RefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            context.Set<OAuthCredential>().Add(credential);
            await context.SaveChangesAsync();

            return credential.Id;
        }

        private async Task ResaveOAuthCredential(int credentialId, string? reEnteredRefreshToken)
        {
            using var context = NewContext(CryptoUnder(ActiveKey, RetiredKey));

            var credential = await context.Set<OAuthCredential>().SingleAsync(c => c.Id == credentialId);

            if (reEnteredRefreshToken is not null)
            {
                credential.RefreshToken = reEnteredRefreshToken;
            }

            credential.UpdatedAt = DateTimeOffset.UtcNow;
            context.Entry(credential).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        private async Task<OAuthCredential> StoredOAuthTokens(int credentialId)
        {
            using var context = NewContext(CryptoUnder(ActiveKey, RetiredKey));

            return await context.Set<OAuthCredential>()
                .AsNoTracking()
                .SingleAsync(c => c.Id == credentialId);
        }
    }
}
