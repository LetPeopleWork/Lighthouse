using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Auth
{
    // Basic authentication is the only method here because it is the one the maintainer could verify
    // against a real on-prem instance; OAuth on ServiceNow needs admin setup on the instance itself
    // that nobody involved in this work has access to.
    [TestFixture]
    public class ServiceNowBasicAuthStrategyTest
    {
        private const string Username = "lighthouse.integration";
        private const string StoredPassword = "encrypted-secret";

        private const string AuthorizationHeaderName = "Authorization";

        private const string ActiveKeyId = "key-active";

        private const string OffRingKeyId = "key-not-on-the-ring";

        private static readonly byte[] ActiveKeyMaterial = Convert.FromBase64String("aXhZdXd5+OeT8kjKP2gB7UdqMEB3RY4LQMI2yffxDEw=");

        private static readonly byte[] OffRingKeyMaterial = Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg=");

        [Test]
        public async Task AServiceNowCredential_IsPresentedToTheInstanceAsBasicAuthentication()
        {
            var subject = CreateSubject();
            using var request = new HttpRequestMessage();

            await subject.ApplyAsync(request, CreateConnection(), CancellationToken.None);

            Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Basic"));
        }

        [Test]
        public async Task AServiceNowCredential_CarriesTheUsernameAndTheDecryptedPassword()
        {
            var subject = CreateSubject();
            using var request = new HttpRequestMessage();

            await subject.ApplyAsync(request, CreateConnection(), CancellationToken.None);

            var encoded = request.Headers.Authorization?.Parameter ?? string.Empty;
            var decoded = Encoding.ASCII.GetString(Convert.FromBase64String(encoded));

            Assert.That(decoded, Is.EqualTo($"{Username}:{StoredPassword}"));
        }

        // The password is stored encrypted and only ever decrypted at the moment it goes on the wire, so
        // the strategy reads the stored value through the crypto service rather than treating it as
        // plaintext.
        [Test]
        public async Task TheStoredPassword_ReachesTheInstanceThroughTheCryptoService()
        {
            var cryptoService = new RecordingCryptoService();
            var subject = new ServiceNowBasicAuthStrategy(cryptoService);
            using var request = new HttpRequestMessage();

            await subject.ApplyAsync(request, CreateConnection(), CancellationToken.None);

            Assert.That(cryptoService.Decrypted, Does.Contain(StoredPassword));
        }

        // Throwing and sending nothing are two different facts. A strategy that put the header on the
        // request and only then failed would satisfy a test that asked no more than "did it throw", and it
        // would still have handed ServiceNow a password nobody here could read.
        [Test]
        public void AStoredPasswordNobodyCanRead_StopsWithTheRequestStillCarryingNoCredential()
        {
            var subject = new ServiceNowBasicAuthStrategy(ACryptoServiceHoldingOnlyTheActiveKey());
            using var request = new HttpRequestMessage();

            Assert.ThrowsAsync<UnreadableSecretException>(
                () => subject.ApplyAsync(request, CreateConnection(ACredentialTheInstanceCannotRead()), CancellationToken.None));

            Assert.That(request.Headers.Contains(AuthorizationHeaderName), Is.False,
                "The request must carry no Authorization header at all, not merely a failed call afterwards.");
        }

        // The strategy is handed its request and its connection by whichever connector resolved it,
        // so a missing one is a wiring fault in the caller. It says so at the argument that is
        // actually missing rather than dereferencing it and reporting a null somewhere downstream.
        [Test]
        public void AStrategyGivenNoRequestToDecorate_SaysWhichArgumentIsMissing()
        {
            var subject = CreateSubject();

            Assert.That(async () => await subject.ApplyAsync(null!, CreateConnection(), CancellationToken.None),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void AStrategyGivenNoConnectionToReadTheCredentialFrom_SaysWhichArgumentIsMissing()
        {
            var subject = CreateSubject();
            using var request = new HttpRequestMessage();

            Assert.That(async () => await subject.ApplyAsync(request, null!, CancellationToken.None),
                Throws.InstanceOf<ArgumentNullException>());
        }

        private sealed class RecordingCryptoService : Lighthouse.Backend.Services.Interfaces.ICryptoService
        {
            public List<string> Decrypted { get; } = [];

            public string Decrypt(string cipherText)
            {
                Decrypted.Add(cipherText);
                return cipherText;
            }

            public string Encrypt(string plainText) => plainText;

            public SecretReadResult Read(string storedValue) => new(SecretState.LegacyPlaintext, storedValue, null);
        }

        private static ServiceNowBasicAuthStrategy CreateSubject()
        {
            return new ServiceNowBasicAuthStrategy(new FakeCryptoService());
        }

        // A well-formed envelope naming a key the ring does not hold cannot be read on any run. Random
        // bytes would not do: garbage clears the padding and printability checks by chance roughly once in
        // a thousand tries, and a test that fails one run in a thousand teaches a reader to ignore it.
        private static string ACredentialTheInstanceCannotRead()
        {
            return SecretEnvelope.Protect("whatever-was-stored-here", OffRingKeyId, OffRingKeyMaterial).Format();
        }

        private static CryptoService ACryptoServiceHoldingOnlyTheActiveKey()
        {
            var ring = new EncryptionKeyRing(new EncryptionKey(ActiveKeyId, ActiveKeyMaterial));

            return new CryptoService(new EncryptionKeyRingHolder(ring), NullLogger<CryptoService>.Instance);
        }

        private static WorkTrackingSystemConnection CreateConnection(string password = StoredPassword)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "ServiceNow Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = "https://dev12345.service-now.com/" },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = Username },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = password, IsSecret = true },
            ]);

            return connection;
        }
    }
}
