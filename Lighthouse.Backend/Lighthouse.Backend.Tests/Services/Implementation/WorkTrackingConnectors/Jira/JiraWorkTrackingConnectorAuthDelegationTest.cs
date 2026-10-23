using System.Net;
using System.Text;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    [TestFixture]
    public class JiraWorkTrackingConnectorAuthDelegationTest
    {
        [Test]
        public async Task ValidateConnection_DelegatesAuthorizationToResolvedStrategy()
        {
            var strategyMock = new Mock<IWorkTrackingAuthStrategy>();
            strategyMock
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factoryMock = new Mock<IWorkTrackingAuthStrategyFactory>();
            factoryMock
                .Setup(f => f.Resolve(AuthenticationMethodKeys.JiraCloud))
                .Returns(strategyMock.Object);

            var connection = CreateConnection(AuthenticationMethodKeys.JiraCloud, "http://127.0.0.1:1/");
            var subject = CreateSubject(factoryMock.Object);

            await subject.ValidateConnection(connection);

            factoryMock.Verify(f => f.Resolve(AuthenticationMethodKeys.JiraCloud), Times.AtLeastOnce);
            strategyMock.Verify(
                s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.Is<WorkTrackingSystemConnection>(c => c == connection), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [TestCase(AuthenticationMethodKeys.JiraOAuth, ExpectedResult = true)]
        [TestCase(AuthenticationMethodKeys.JiraScopedToken, ExpectedResult = true)]
        [TestCase(AuthenticationMethodKeys.JiraCloud, ExpectedResult = false)]
        [TestCase(AuthenticationMethodKeys.JiraDataCenter, ExpectedResult = false)]
        public bool RoutesViaAtlassianCloudGateway_TrueForBearerTokenMethods(string authMethodKey)
        {
            return JiraWorkTrackingConnector.RoutesViaAtlassianCloudGateway(authMethodKey);
        }

        [TestCase(AuthenticationMethodKeys.JiraCloud, "https://api-token-site.atlassian.net/jira/", "https://api-token-site.atlassian.net/")]
        [TestCase(AuthenticationMethodKeys.JiraCloud, "https://Mixed-Case-Site.Atlassian.net/jira/software/projects", "https://mixed-case-site.atlassian.net/")]
        [TestCase(AuthenticationMethodKeys.JiraDataCenter, "https://context-path.example.invalid/jira", "https://context-path.example.invalid/jira/")]
        [TestCase(AuthenticationMethodKeys.JiraDataCenter, "https://context-path-slash.example.invalid/jira/", "https://context-path-slash.example.invalid/jira/")]
        [TestCase(AuthenticationMethodKeys.JiraDataCenter, "https://no-context-path.example.invalid/", "https://no-context-path.example.invalid/")]
        public async Task ValidateConnection_SendsEveryRequestUnderTheSiteAddress(string authMethodKey, string enteredUrl, string siteAddress)
        {
            var requestedUrls = new List<string>();
            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraSite(TenantInfoAsJson, requestedUrls));

            await connector.ValidateConnection(CreateConnection(authMethodKey, enteredUrl));

            Assert.That(requestedUrls, Has.Member(siteAddress + JiraRestRoutes.Myself).And.All.StartsWith(siteAddress));
        }

        [Test]
        public async Task ValidateConnection_OAuthOnACloudUrlWithAPath_LooksTheCloudIdUpAtTheSiteRoot()
        {
            var requestedUrls = new List<string>();
            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraSite(TenantInfoAsJson, requestedUrls));

            await connector.ValidateConnection(CreateConnection(AuthenticationMethodKeys.JiraOAuth, "https://oauth-site.atlassian.net/jira/"));

            Assert.That(requestedUrls, Has.Member("https://oauth-site.atlassian.net/_edge/tenant_info")
                .And.Member($"https://api.atlassian.com/ex/jira/{TheCloudId}/{JiraRestRoutes.Myself}"));
        }

        [Test]
        public async Task ValidateConnection_OAuthOnAnAddressAnsweringTheCloudIdLookupWithAPage_AsksForTheSiteAddress()
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraSite(TenantInfoAsHtmlPage, []));

            var verdict = await connector.ValidateConnection(
                CreateConnection(AuthenticationMethodKeys.JiraOAuth, "https://jira.custom-domain.example.invalid/jira"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("invalid_url"));
                Assert.That(verdict.Message, Is.EqualTo("Use your site address, e.g. https://yourcompany.atlassian.net"));
                Assert.That(verdict.FieldName, Is.EqualTo(JiraWorkTrackingOptionNames.Url));
            }
        }

        private const string TheCloudId = "cloud-6401";

        private static HttpResponseMessage TenantInfoAsJson()
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"cloudId\":\"{TheCloudId}\"}}", Encoding.UTF8, "application/json"),
            };

        private static HttpResponseMessage TenantInfoAsHtmlPage()
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent("<!DOCTYPE html><html><body>Jira</body></html>", Encoding.UTF8, "text/html"),
            };

        private static HttpMessageHandler AJiraSite(Func<HttpResponseMessage> tenantInfo, List<string> requestedUrls)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    var url = request.RequestUri?.ToString() ?? string.Empty;
                    requestedUrls.Add(url);

                    return Task.FromResult(url.EndsWith("/_edge/tenant_info", StringComparison.Ordinal)
                        ? tenantInfo()
                        : AnswerFromJira(url));
                });

            return mock.Object;
        }

        private static HttpResponseMessage AnswerFromJira(string url)
        {
            var body = url switch
            {
                _ when url.EndsWith(JiraRestRoutes.Myself, StringComparison.Ordinal) => "{}",
                _ when url.EndsWith(JiraRestRoutes.ServerInfo, StringComparison.Ordinal) => "{\"deploymentType\":\"Server\"}",
                _ => "[]",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static int connectionIdSeed = 64010;

        private static WorkTrackingSystemConnection CreateConnection(string authMethodKey, string url)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = Interlocked.Increment(ref connectionIdSeed),
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Delegation Test",
                AuthenticationMethodKey = authMethodKey
            };
            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = url, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "user@example.com", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "encrypted-token", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "1", IsSecret = false },
            ]);
            return connection;
        }

        private static JiraWorkTrackingConnector CreateSubject(IWorkTrackingAuthStrategyFactory factory)
        {
            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                factory,
                new Lighthouse.Backend.Cache.Cache<string, object>(),
                new DeliveryForecastBlockRenderer(), TimeProvider.System);
        }
    }
}
