using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Auth
{
    // Story #5574 / D3. Basic auth is the v1 method because it is what the maintainer verified
    // works on the target on-prem instance; OAuth there needs instance-side admin setup nobody
    // involved has. See ADR-115 for why the successor is named and detection is not built.
    [TestFixture]
    public class ServiceNowBasicAuthStrategyTest
    {
        private const string Username = "lighthouse.integration";
        private const string StoredPassword = "encrypted-secret";

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

        // The secret is stored encrypted (AC5) and only ever decrypted at the moment it goes on
        // the wire, so the strategy reads the stored value through the crypto service rather than
        // treating it as plaintext.
        [Test]
        public async Task TheStoredPassword_ReachesTheInstanceThroughTheCryptoService()
        {
            var cryptoService = new RecordingCryptoService();
            var subject = new ServiceNowBasicAuthStrategy(cryptoService);
            using var request = new HttpRequestMessage();

            await subject.ApplyAsync(request, CreateConnection(), CancellationToken.None);

            Assert.That(cryptoService.Decrypted, Does.Contain(StoredPassword));
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
        }

        private static ServiceNowBasicAuthStrategy CreateSubject()
        {
            return new ServiceNowBasicAuthStrategy(new FakeCryptoService());
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
