using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

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

        private static WorkTrackingSystemConnection CreateConnection(string authMethodKey, string url)
        {
            var connection = new WorkTrackingSystemConnection
            {
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
                factory);
        }
    }
}
