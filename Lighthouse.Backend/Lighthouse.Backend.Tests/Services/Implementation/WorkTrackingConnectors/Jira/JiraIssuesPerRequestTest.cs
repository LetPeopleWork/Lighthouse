using System.Net;
using System.Text;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    [TestFixture]
    public class JiraIssuesPerRequestTest
    {
        private static int _connectionIdSeed = 9000;

        [Test]
        public async Task GetWorkItemsForTeam_DataCenter_DefaultsToMaxResults1000WhenOptionAbsent()
        {
            var capturedSearchUrl = await CaptureSearchUrl(deploymentType: "Server", issuesPerRequestOption: null);

            Assert.That(capturedSearchUrl, Does.Contain("maxResults=1000"));
        }

        [Test]
        public async Task GetWorkItemsForTeam_DataCenter_UsesConfiguredIssuesPerRequest()
        {
            var capturedSearchUrl = await CaptureSearchUrl(deploymentType: "Server", issuesPerRequestOption: "250");

            Assert.That(capturedSearchUrl, Does.Contain("maxResults=250"));
        }

        [Test]
        public async Task GetWorkItemsForTeam_Cloud_UsesConfiguredIssuesPerRequest()
        {
            var capturedSearchUrl = await CaptureSearchUrl(deploymentType: "Cloud", issuesPerRequestOption: "250");

            Assert.That(capturedSearchUrl, Does.Contain("maxResults=250"));
        }

        private static async Task<string> CaptureSearchUrl(string deploymentType, string? issuesPerRequestOption)
        {
            var capturedSearchUrl = string.Empty;

            var handler = CreateRecordingHandler(deploymentType, request =>
            {
                if (request.RequestUri is null)
                {
                    return;
                }

                var path = request.RequestUri.AbsoluteUri;
                if (path.Contains("/search?jql=", StringComparison.Ordinal)
                    || path.Contains("/search/jql?", StringComparison.Ordinal))
                {
                    capturedSearchUrl = path;
                }
            });

            var subject = CreateSubject(handler);
            var team = CreateTeam(issuesPerRequestOption);

            await subject.GetWorkItemsForTeam(team);

            return capturedSearchUrl;
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
        {
            var strategyMock = new Mock<IWorkTrackingAuthStrategy>();
            strategyMock
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factoryMock = new Mock<IWorkTrackingAuthStrategyFactory>();
            factoryMock
                .Setup(f => f.Resolve(It.IsAny<string>()))
                .Returns(strategyMock.Object);

            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                factoryMock.Object,
                handler);
        }

        private static Team CreateTeam(string? issuesPerRequestOption)
        {
            var connectionId = Interlocked.Increment(ref _connectionIdSeed);
            var url = $"https://jira-{connectionId}.example.invalid";

            var connection = new WorkTrackingSystemConnection
            {
                Id = connectionId,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = $"Test Setting {connectionId}",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = url, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "user@example.com", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "token", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "10", IsSecret = false },
            ]);

            if (issuesPerRequestOption is not null)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption
                {
                    Key = JiraWorkTrackingOptionNames.IssuesPerRequest,
                    Value = issuesPerRequestOption,
                    IsSecret = false,
                });
            }

            var team = new Team
            {
                Id = connectionId,
                Name = $"Team {connectionId}",
                DataRetrievalValue = "project = PROJ",
                WorkTrackingSystemConnectionId = connectionId,
                WorkTrackingSystemConnection = connection,
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story");

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");
            team.DoingStates.Clear();
            team.DoingStates.Add("In Progress");
            team.ToDoStates.Clear();
            team.ToDoStates.Add("To Do");

            return team;
        }
    }
}
