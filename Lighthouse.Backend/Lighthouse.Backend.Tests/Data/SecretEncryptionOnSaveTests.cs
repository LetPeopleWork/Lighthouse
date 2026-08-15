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

        // The case that separates asking the classifier from testing for the stored value's leading text:
        // this one looks encrypted but names a key nobody holds, so nothing can ever unwrap it. Matching on
        // the leading text would leave it in place and report a clean save, hiding a secret already lost.
        [Test]
        public async Task SecretOptionShapedLikeAnEnvelopeThatNoKeyCanRead_IsNotSkipped()
        {
            var connectionId = await SeedConnectionWithSecret(Credential, CryptoUnder(StrangerKey));
            var beforeSave = await StoredSecret(connectionId);

            await ResaveSecretOption(connectionId, reEnteredValue: null);

            var afterSave = await StoredSecret(connectionId);

            Assert.That(afterSave, Is.Not.EqualTo(beforeSave));
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
            return new CryptoService(new EncryptionKeyRingHolder(new EncryptionKeyRing(keys)));
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
