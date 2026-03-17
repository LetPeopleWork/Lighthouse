using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    [Category("Integration")]
    public class JiraScopedTokenIntegrationTest
    {
        private const string EpicId = "LGHTHSDMO-1";
        private const string DescriptionField = "description";

        [Test]
        public async Task ValidateConnection_GivenValidSettings_ReturnsTrue()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var isValid = await subject.ValidateConnection(connection);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public async Task ValidateTeamSettings_ValidConnectionSettings_ReturnsTrueIfTeamHasThroughput()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO AND issueKey = LGHTHSDMO-11");

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public async Task ValidatePortfolioSettings_ValidConnectionSettings_ReturnsTrueIfFeaturesAreFound()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio("project = LGHTHSDMO");

            var isValid = await subject.ValidatePortfolioSettings(portfolio);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public async Task GetBoards_ReturnsCorrectBoards()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var boards = (await subject.GetBoards(connection)).ToList();

            Assert.That(boards, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsForTeam_GetsAllItemsThatMatchQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = PROJ AND labels = ExistingLabel");
            team.ResetUpdateTime();

            var matchingItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(matchingItems.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task WriteFieldsToWorkItems_SingleUpdate_SucceedsAndReturnsResult()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = EpicId, TargetFieldReference = DescriptionField, Value = "42" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(1));
                Assert.That(result.AllSucceeded, Is.True);
                Assert.That(result.ItemResults[0].WorkItemId, Is.EqualTo(EpicId));
            }
        }

        private Team CreateTeam(string query)
        {
            var team = new Team
            {
                Name = "TestTeam",
                DataRetrievalValue = query
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story");
            team.WorkItemTypes.Add("Bug");
            team.WorkItemTypes.Add("Task");

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");

            team.DoingStates.Clear();
            team.DoingStates.Add("In Progress");

            team.ToDoStates.Clear();
            team.ToDoStates.Add("To Do");

            team.WorkTrackingSystemConnection = CreateWorkTrackingSystemConnection();

            return team;
        }

        private Portfolio CreatePortfolio(string query)
        {
            var portfolio = new Portfolio
            {
                Name = "TestProject",
                DataRetrievalValue = query,
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Epic");

            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Done");

            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");

            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("To Do");

            portfolio.WorkTrackingSystemConnection = CreateWorkTrackingSystemConnection();

            return portfolio;
        }

        private static WorkTrackingSystemConnection CreateWorkTrackingSystemConnection()
        {
            var jiraUrl = "https://letpeoplework.atlassian.net";
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraScopedTokenIntegrationTestToken")
                ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraScopedTokenIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Scoped Token Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraScopedToken
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = jiraUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            return connectionSetting;
        }

        private static JiraWorkTrackingConnector CreateSubject()
        {
            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()), Mock.Of<ILogger<JiraWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}
