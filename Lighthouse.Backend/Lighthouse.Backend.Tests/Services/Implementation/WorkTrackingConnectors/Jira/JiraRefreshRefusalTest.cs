using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// A refusal met on the background refresh path. Every other rejection fixture drives a Validate method,
    /// which only the connection screen ever calls - so none of them would notice a refresh that stopped
    /// saying what Jira refused.
    /// </summary>
    [TestFixture]
    public class JiraRefreshRefusalTest
    {
        private const string JiraAnswer =
            "{\"errorMessages\":[\"Field 'sprnt' does not exist or you do not have permission to view it.\"],\"errors\":{}}";

        private const string TheFilterTheOperatorConfigured = "project = PROJ";

        [Test]
        public void GetFeaturesForProject_DataCenterRefusesTheQuery_WarnsWithTheQueryAndJirasWholeAnswer()
        {
            var loggerMock = new Mock<ILogger<JiraWorkTrackingConnector>>();
            var subject = JiraConnectorTestSetup.AConnectorOver(ARefusingJiraDataCenter(), loggerMock.Object);
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();

            Assert.ThrowsAsync<JiraQueryRejectedException>(
                () => subject.GetFeaturesForProject(portfolio, CancellationToken.None));

            var warning = ReadWarning(loggerMock);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(warning, Does.Contain(TheFilterTheOperatorConfigured),
                    "A refusal nobody can tie back to a query cannot be diagnosed from anything else.");
                Assert.That(warning, Does.Contain(JiraAnswer),
                    "Jira puts the sentence a user can act on inside its answer, so the answer goes to the log whole.");
            }
        }

        [Test]
        public void JiraQueryRejectedException_IsBothAWorkTrackingRefusalAndAnHttpRequestFailure()
        {
            var rejection = new JiraQueryRejectedException("refused", "project = PROJ", HttpStatusCode.BadRequest);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rejection, Is.InstanceOf<WorkTrackingRefusedException>(),
                    "A refresh recognises a refusal by the port type, because an updater must not know which "
                    + "work tracking system it is talking to.");
                Assert.That(rejection, Is.InstanceOf<HttpRequestException>(),
                    "Losing this turns the delivery-source degradation, which answers 'Jira could not be asked' "
                    + "by catching exactly this type, into a hard refresh failure.");
            }
        }

        private static HttpMessageHandler ARefusingJiraDataCenter()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(Answer(request)));

            return mock.Object;
        }

        private static HttpResponseMessage Answer(HttpRequestMessage request)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal))
            {
                return Responding(HttpStatusCode.OK, "{\"deploymentType\":\"Server\"}");
            }

            if (path.EndsWith("rest/api/latest/field", StringComparison.Ordinal))
            {
                return Responding(HttpStatusCode.OK, "[]");
            }

            if (path.Contains("rest/api/latest/search", StringComparison.Ordinal))
            {
                return Responding(HttpStatusCode.BadRequest, JiraAnswer);
            }

            return Responding(HttpStatusCode.OK, "{}");
        }

        private static HttpResponseMessage Responding(HttpStatusCode statusCode, string body)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static string ReadWarning(Mock<ILogger<JiraWorkTrackingConnector>> loggerMock)
        {
            var warning = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Warning)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .FirstOrDefault(message => message.Contains("refused the query", StringComparison.Ordinal));

            Assert.That(warning, Is.Not.Null,
                "The refusal is the only record of what stopped the refresh at the connector.");

            return warning!;
        }
    }
}
