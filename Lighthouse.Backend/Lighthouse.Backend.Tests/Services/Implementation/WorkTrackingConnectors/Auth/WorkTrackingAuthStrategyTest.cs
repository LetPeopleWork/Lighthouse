using System.Net.Http.Headers;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Auth
{
    [TestFixture]
    public class WorkTrackingAuthStrategyTest
    {
        private const string PlaintextPat = "my-pat-token";
        private const string PlaintextJiraToken = "secret-jira-token";
        private const string PlaintextLinearKey = "lin_api_xyz123";
        private const string JiraUsername = "alice@example.com";

        private const string EncryptedPat = "encrypted-pat";
        private const string EncryptedJiraToken = "encrypted-jira-token";
        private const string EncryptedLinearKey = "encrypted-linear-key";

        private Mock<ICryptoService> cryptoServiceMock = null!;

        [SetUp]
        public void SetUp()
        {
            cryptoServiceMock = new Mock<ICryptoService>();
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedPat)).Returns(PlaintextPat);
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedJiraToken)).Returns(PlaintextJiraToken);
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedLinearKey)).Returns(PlaintextLinearKey);
        }

        [Test]
        public async Task PatAuthStrategy_ApplyAsync_SetsBasicHeaderWithEmptyUsernameAndDecryptedPat()
        {
            var strategy = new PatAuthStrategy(cryptoServiceMock.Object);
            var connection = CreateAdoConnection();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.azure.com/org");

            await strategy.ApplyAsync(request, connection, CancellationToken.None);

            var expected = ExpectedBasicHeader($":{PlaintextPat}");
            Assert.That(request.Headers.Authorization, Is.EqualTo(expected));
        }

        [Test]
        public async Task JiraCloudBasicAuthStrategy_ApplyAsync_JiraCloud_SetsBasicHeaderWithUsernameAndApiToken()
        {
            var strategy = new JiraCloudBasicAuthStrategy(cryptoServiceMock.Object);
            var connection = CreateJiraConnection(AuthenticationMethodKeys.JiraCloud, includeUsername: true);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.atlassian.net");

            await strategy.ApplyAsync(request, connection, CancellationToken.None);

            var expected = ExpectedBasicHeader($"{JiraUsername}:{PlaintextJiraToken}");
            Assert.That(request.Headers.Authorization, Is.EqualTo(expected));
        }

        [Test]
        public async Task JiraCloudBasicAuthStrategy_ApplyAsync_JiraScopedToken_SetsBasicHeaderWithUsernameAndApiToken()
        {
            var strategy = new JiraCloudBasicAuthStrategy(cryptoServiceMock.Object);
            var connection = CreateJiraConnection(AuthenticationMethodKeys.JiraScopedToken, includeUsername: true);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.atlassian.net");

            await strategy.ApplyAsync(request, connection, CancellationToken.None);

            var expected = ExpectedBasicHeader($"{JiraUsername}:{PlaintextJiraToken}");
            Assert.That(request.Headers.Authorization, Is.EqualTo(expected));
        }

        [Test]
        public async Task JiraCloudBasicAuthStrategy_ApplyAsync_JiraDataCenter_SetsBearerHeaderWithApiToken()
        {
            var strategy = new JiraCloudBasicAuthStrategy(cryptoServiceMock.Object);
            var connection = CreateJiraConnection(AuthenticationMethodKeys.JiraDataCenter, includeUsername: false);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://jira.internal.example.com");

            await strategy.ApplyAsync(request, connection, CancellationToken.None);

            Assert.That(request.Headers.Authorization, Is.EqualTo(new AuthenticationHeaderValue("Bearer", PlaintextJiraToken)));
        }

        [Test]
        public async Task LinearApiKeyAuthStrategy_ApplyAsync_SetsRawAuthorizationHeaderWithoutScheme()
        {
            var strategy = new LinearApiKeyAuthStrategy(cryptoServiceMock.Object);
            var connection = CreateLinearConnection();
            var request = new HttpRequestMessage(HttpMethod.Post, LinearWorkTrackingOptionNames.ApiUrl);

            await strategy.ApplyAsync(request, connection, CancellationToken.None);

            Assert.That(request.Headers.TryGetValues("Authorization", out var values), Is.True);
            Assert.That(values!.Single(), Is.EqualTo(PlaintextLinearKey));
        }

        [Test]
        public async Task NoOpAuthStrategy_ApplyAsync_LeavesAuthorizationHeaderUnset()
        {
            var strategy = new NoOpAuthStrategy();
            var connection = new WorkTrackingSystemConnection
            {
                AuthenticationMethodKey = AuthenticationMethodKeys.None
            };
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            await strategy.ApplyAsync(request, connection, CancellationToken.None);

            Assert.That(request.Headers.Authorization, Is.Null);
        }

        [Test]
        [TestCase(AuthenticationMethodKeys.AzureDevOpsPat, typeof(PatAuthStrategy))]
        [TestCase(AuthenticationMethodKeys.JiraCloud, typeof(JiraCloudBasicAuthStrategy))]
        [TestCase(AuthenticationMethodKeys.JiraDataCenter, typeof(JiraCloudBasicAuthStrategy))]
        [TestCase(AuthenticationMethodKeys.JiraScopedToken, typeof(JiraCloudBasicAuthStrategy))]
        [TestCase(AuthenticationMethodKeys.LinearApiKey, typeof(LinearApiKeyAuthStrategy))]
        [TestCase(AuthenticationMethodKeys.None, typeof(NoOpAuthStrategy))]
        public void Resolve_KnownKey_ReturnsExpectedStrategyType(string authenticationMethodKey, Type expectedStrategyType)
        {
            var factory = CreateFactory();

            var strategy = factory.Resolve(authenticationMethodKey);

            Assert.That(strategy, Is.InstanceOf(expectedStrategyType));
        }

        [Test]
        public void Resolve_UnknownKey_ThrowsAndNamesTheKey()
        {
            var factory = CreateFactory();

            var ex = Assert.Throws<WorkTrackingAuthStrategyNotFoundException>(
                () => factory.Resolve("nonexistent.method"));

            Assert.That(ex!.Message, Does.Contain("nonexistent.method"));
        }

        private IWorkTrackingAuthStrategyFactory CreateFactory()
        {
            return new WorkTrackingAuthStrategyFactory(
                new PatAuthStrategy(cryptoServiceMock.Object),
                new JiraCloudBasicAuthStrategy(cryptoServiceMock.Object),
                new LinearApiKeyAuthStrategy(cryptoServiceMock.Object),
                new NoOpAuthStrategy());
        }

        private static WorkTrackingSystemConnection CreateAdoConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "ADO Connection",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = AzureDevOpsWorkTrackingOptionNames.Url,
                Value = "https://dev.azure.com/org",
                IsSecret = false,
            });
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken,
                Value = EncryptedPat,
                IsSecret = true,
            });
            return connection;
        }

        private static WorkTrackingSystemConnection CreateJiraConnection(string authMethodKey, bool includeUsername)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Jira Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = authMethodKey,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = JiraWorkTrackingOptionNames.Url,
                Value = "https://example.atlassian.net",
                IsSecret = false,
            });
            if (includeUsername)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption
                {
                    Key = JiraWorkTrackingOptionNames.Username,
                    Value = JiraUsername,
                    IsSecret = false,
                });
            }
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = JiraWorkTrackingOptionNames.ApiToken,
                Value = EncryptedJiraToken,
                IsSecret = true,
            });
            return connection;
        }

        private static WorkTrackingSystemConnection CreateLinearConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Linear Connection",
                WorkTrackingSystem = WorkTrackingSystems.Linear,
                AuthenticationMethodKey = AuthenticationMethodKeys.LinearApiKey,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = LinearWorkTrackingOptionNames.ApiKey,
                Value = EncryptedLinearKey,
                IsSecret = true,
            });
            return connection;
        }

        private static AuthenticationHeaderValue ExpectedBasicHeader(string raw)
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            return new AuthenticationHeaderValue("Basic", encoded);
        }
    }
}
