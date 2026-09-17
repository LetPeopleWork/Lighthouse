using System.Net;
using System.Text;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    [TestFixture]
    public class JiraIssuesPerRequestTest
    {
        private static readonly string[] HowAPageSizeIsWritten = ["maxResults=", "\"maxResults\":"];

        [Test]
        public async Task GetWorkItemsForTeam_DataCenter_DefaultsToMaxResults1000WhenOptionAbsent()
        {
            var pageSizeAskedFor = await ThePageSizeAskedFor(deploymentType: "Server", issuesPerRequestOption: null);

            Assert.That(pageSizeAskedFor, Is.EqualTo("1000"));
        }

        [Test]
        public async Task GetWorkItemsForTeam_DataCenter_UsesConfiguredIssuesPerRequest()
        {
            var pageSizeAskedFor = await ThePageSizeAskedFor(deploymentType: "Server", issuesPerRequestOption: "250");

            Assert.That(pageSizeAskedFor, Is.EqualTo("250"));
        }

        [Test]
        public async Task GetWorkItemsForTeam_Cloud_UsesConfiguredIssuesPerRequest()
        {
            var pageSizeAskedFor = await ThePageSizeAskedFor(deploymentType: "Cloud", issuesPerRequestOption: "250");

            Assert.That(pageSizeAskedFor, Is.EqualTo("250"));
        }

        /// <summary>
        /// How large a page the connector asked Jira for, read out of whichever half of the request carried it:
        /// Cloud asks in the query string, Data Center asks in a json body.
        /// </summary>
        private static async Task<string> ThePageSizeAskedFor(string deploymentType, string? issuesPerRequestOption)
        {
            var pageSizeAskedFor = string.Empty;

            var handler = CreateRecordingHandler(deploymentType, request =>
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                if (!path.Contains("/search", StringComparison.Ordinal))
                {
                    return;
                }

                var inTheRequestLine = PageSizeIn(request.RequestUri?.Query ?? string.Empty);

                pageSizeAskedFor = inTheRequestLine.Length > 0
                    ? inTheRequestLine
                    : PageSizeIn(JiraConnectorTestSetup.BodyOf(request));
            });

            var subject = CreateSubject(handler);
            var team = CreateTeam(issuesPerRequestOption);

            await subject.GetWorkItemsForTeam(team, CancellationToken.None);

            return pageSizeAskedFor;
        }

        private static string PageSizeIn(string requestPart)
        {
            foreach (var marker in HowAPageSizeIsWritten)
            {
                var namedAt = requestPart.IndexOf(marker, StringComparison.Ordinal);

                if (namedAt < 0)
                {
                    continue;
                }

                var digits = requestPart[(namedAt + marker.Length)..].TakeWhile(char.IsAsciiDigit).ToArray();

                if (digits.Length > 0)
                {
                    return new string(digits);
                }
            }

            return string.Empty;
        }

        private static HttpMessageHandler CreateRecordingHandler(string deploymentType, Action<HttpRequestMessage> record)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    record(request);
                    return Task.FromResult(BuildResponse(request, deploymentType));
                });
            return mock.Object;
        }

        private static HttpResponseMessage BuildResponse(HttpRequestMessage request, string deploymentType)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            string body = path switch
            {
                _ when path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal)
                    => $"{{\"deploymentType\":\"{deploymentType}\"}}",
                _ when path.EndsWith("rest/api/latest/field", StringComparison.Ordinal)
                    => "[]",
                _ when path.Contains("rest/api/latest/search", StringComparison.Ordinal)
                    => "{\"startAt\":0,\"maxResults\":1000,\"total\":0,\"issues\":[]}",
                _ when path.Contains("rest/api/3/search/jql", StringComparison.Ordinal)
                    => "{\"issues\":[]}",
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static JiraWorkTrackingConnector CreateSubject(HttpMessageHandler handler)
            => JiraConnectorTestSetup.AConnectorOver(handler);

        private static Team CreateTeam(string? issuesPerRequestOption)
            => JiraConnectorTestSetup.ATeamOnJiraCloud(issuesPerRequestOption);
    }
}
