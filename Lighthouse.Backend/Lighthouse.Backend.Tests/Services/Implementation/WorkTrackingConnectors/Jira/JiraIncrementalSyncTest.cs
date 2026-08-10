using System.Net;
using System.Text;
using System.Text.Json;
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
    /// <summary>
    /// Epic #5687 AC-2.1: the whole-query path carries Jira's <c>updated</c> onto
    /// <see cref="WorkItemBase.LastChangedRemote"/>. Without it a full cycle stores nothing to compare
    /// against, so every later cycle resolves to a full download (D8) and the delta never engages.
    /// Cloud and Data Center are the same connector class, so both deployments are exercised.
    /// </summary>
    [TestFixture]
    public class JiraIncrementalSyncTest
    {
        private const string Cloud = "Cloud";
        private const string DataCenter = "Server";

        private static int connectionIdSeed = 9500;

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_RemembersWhenTheIssueLastChangedRemotely(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: "2026-08-05T14:30:00.000+0200");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.LastChangedRemote, Is.EqualTo(new DateTime(2026, 8, 5, 12, 30, 0, DateTimeKind.Utc)));
                Assert.That(workItem.LastChangedRemote?.Kind, Is.EqualTo(DateTimeKind.Utc),
                    "An instant has no time zone - storing it in anything but UTC makes the next cycle's comparison wrong.");
            }
        }

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_LeavesTheStampEmptyWhenTheTrackerDoesNotReportOne(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: null);

            Assert.That(workItem.LastChangedRemote, Is.Null,
                "No stamp means 'never swept', which resolves the next update to a full fetch (D8). "
                + "A sentinel date would claim knowledge the tracker never gave.");
        }

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_LeavesTheStampEmptyWhenTheTrackerReportsSomethingUnreadable(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: "not a date");

            Assert.That(workItem.LastChangedRemote, Is.Null,
                "An unreadable stamp is no knowledge at all - falling back to a full fetch is the safe resolution (D8).");
        }

        private static async Task<WorkItem> TheSingleWorkItemFetchedFrom(string deploymentType, string? updated)
        {
            var handler = CreateHandler(deploymentType, updated);
            var subject = CreateSubject(handler);

            var workItems = await subject.GetWorkItemsForTeam(CreateTeam());

            Assert.That(workItems.Count(), Is.EqualTo(1), "positive control: the canned response was not read at all.");

            return workItems.Single();
        }

        private static HttpMessageHandler CreateHandler(string deploymentType, string? updated)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                    Task.FromResult(BuildResponse(request, deploymentType, updated)));

            return mock.Object;
        }

        private static HttpResponseMessage BuildResponse(HttpRequestMessage request, string deploymentType, string? updated)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            var body = path switch
            {
                _ when path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal)
                    => $"{{\"deploymentType\":\"{deploymentType}\"}}",
                _ when path.EndsWith("rest/api/latest/field", StringComparison.Ordinal)
                    => "[]",
                _ when path.Contains("rest/api/latest/search", StringComparison.Ordinal)
                    => $"{{\"startAt\":0,\"maxResults\":50,\"total\":1,\"issues\":[{IssueJson(updated)}]}}",
                _ when path.Contains("rest/api/3/search/jql", StringComparison.Ordinal)
                    => $"{{\"issues\":[{IssueJson(updated)}]}}",
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static string IssueJson(string? updated)
        {
            var updatedField = updated is null
                ? string.Empty
                : $"\"updated\":{JsonSerializer.Serialize(updated)},";

            return "{\"key\":\"PROJ-1\",\"fields\":{"
                + "\"summary\":\"An issue\","
                + "\"created\":\"2026-08-01T09:00:00.000+0000\","
                + updatedField
                + "\"status\":{\"name\":\"In Progress\"},"
                + "\"issuetype\":{\"name\":\"Story\"}"
                + "}}";
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

        private static Team CreateTeam()
        {
            var connectionId = Interlocked.Increment(ref connectionIdSeed);
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
