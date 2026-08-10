using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// A Jira connector wired to a stub transport, and a team for it to fetch. Shared by the fixtures that
    /// assert on the request the connector issues, which is the only way to see what a JQL or a field list
    /// actually contains.
    ///
    /// The connector keeps per-connection static state - the discovered deployment, keyed by base url, and
    /// the resolved field names, keyed by connection id - so every team has to carry an id and a url nobody
    /// else used. One counter for all callers is what guarantees that; two fixtures counting on their own
    /// only stay disjoint until one of them issues enough teams to reach the other's range.
    /// </summary>
    public static class JiraConnectorTestSetup
    {
        private static int connectionIdSeed = 9000;

        public static JiraWorkTrackingConnector AConnectorOver(HttpMessageHandler handler)
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

        public static Team ATeamOnJiraCloud(string? issuesPerRequestOption = null, int doneItemsCutoffDays = 0)
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
                DoneItemsCutoffDays = doneItemsCutoffDays,
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
