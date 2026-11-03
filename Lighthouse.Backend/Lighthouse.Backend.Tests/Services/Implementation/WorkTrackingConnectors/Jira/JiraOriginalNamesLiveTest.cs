using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The one thing no recorded payload can establish: that the same boards and work items read through an
    /// account whose profile language is German come out exactly as through an English account. The German
    /// account is the restricted test account, switched to de_DE by the maintainer; when its token is not
    /// available the fixture ignores itself rather than failing.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("JiraIntegration")]
    public class JiraOriginalNamesLiveTest
    {
        private const string OrganizationUrl = "https://letpeoplework.atlassian.net";

        private const string EnglishTokenVariable = "JiraLighthouseIntegrationTestToken";

        private const string EnglishUsernameVariable = "JiraLighthouseIntegrationTestUsername";

        private const string GermanTokenVariable = "JiraLighthouseRestrictedIntegrationTestToken";

        private const string GermanUsernameVariable = "JiraLighthouseRestrictedIntegrationTestUsername";

        private const string DefaultEnglishUsername = "atlassian.pushchair@huser-berta.com";

        private const string DefaultGermanUsername = "benjamin@letpeople.work";

        private const string ScrumBoard = "1";

        private const string DemoBoard = "6";

        private const string DuplicateNamesBoard = "107";

        private static readonly string[] ScrumBoardDoingStates = ["In Progress", "In Prüfung"];

        private JiraWorkTrackingConnector english = null!;

        private JiraWorkTrackingConnector german = null!;

        private WorkTrackingSystemConnection englishConnection = null!;

        private WorkTrackingSystemConnection germanConnection = null!;

        [OneTimeSetUp]
        public void BothAccountsAreAvailable()
        {
            var englishToken = Environment.GetEnvironmentVariable(EnglishTokenVariable);
            var germanToken = Environment.GetEnvironmentVariable(GermanTokenVariable);

            if (string.IsNullOrEmpty(englishToken) || string.IsNullOrEmpty(germanToken))
            {
                Assert.Ignore($"Needs both '{EnglishTokenVariable}' and '{GermanTokenVariable}'.");
            }

            englishConnection = AConnection(
                9101, Environment.GetEnvironmentVariable(EnglishUsernameVariable) ?? DefaultEnglishUsername, englishToken);
            germanConnection = AConnection(
                9102, Environment.GetEnvironmentVariable(GermanUsernameVariable) ?? DefaultGermanUsername, germanToken);

            english = ASubject();
            german = ASubject();
        }

        [Test]
        public async Task The_demo_board_lists_the_same_states_and_types_through_both_accounts()
        {
            var inEnglish = await english.GetBoardInformation(englishConnection, DemoBoard);
            var inGerman = await german.GetBoardInformation(germanConnection, DemoBoard);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inEnglish.DoingStates, Is.Not.Empty, "The comparison means nothing if the English read finds no states.");
                Assert.That(inGerman.ToDoStates, Is.EqualTo(inEnglish.ToDoStates));
                Assert.That(inGerman.DoingStates, Is.EqualTo(inEnglish.DoingStates));
                Assert.That(inGerman.DoneStates, Is.EqualTo(inEnglish.DoneStates));
                Assert.That(inGerman.WorkItemTypes, Is.EqualTo(inEnglish.WorkItemTypes));
            }
        }

        [Test]
        public async Task The_scrum_board_lists_the_custom_German_status_next_to_the_English_ones_through_both_accounts()
        {
            var inEnglish = await english.GetBoardInformation(englishConnection, ScrumBoard);
            var inGerman = await german.GetBoardInformation(germanConnection, ScrumBoard);

            foreach (var board in new[] { inEnglish, inGerman })
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(board.ToDoStates, Is.EqualTo(new List<string> { "To Do" }));
                    Assert.That(board.DoingStates, Is.EquivalentTo(ScrumBoardDoingStates));
                    Assert.That(board.DoneStates, Is.EqualTo(new List<string> { "Done" }));
                }
            }
        }

        [Test]
        public async Task A_board_naming_one_state_under_several_status_ids_lists_it_once_through_both_accounts()
        {
            var inEnglish = await english.GetBoardInformation(englishConnection, DuplicateNamesBoard);
            var inGerman = await german.GetBoardInformation(germanConnection, DuplicateNamesBoard);

            foreach (var states in new[] { inGerman.ToDoStates, inGerman.DoingStates, inGerman.DoneStates })
            {
                Assert.That(states, Is.Unique.IgnoreCase);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inGerman.ToDoStates, Is.EqualTo(inEnglish.ToDoStates));
                Assert.That(inGerman.DoingStates, Is.EqualTo(inEnglish.DoingStates));
                Assert.That(inGerman.DoneStates, Is.EqualTo(inEnglish.DoneStates));
            }
        }

        [Test]
        public async Task The_demo_projects_work_items_have_the_same_states_types_and_dates_through_both_accounts()
        {
            var board = await english.GetBoardInformation(englishConnection, DemoBoard);

            // Other live tests create short-lived scratch items in this project and delete them again, so an
            // item can exist for the first read and be gone for the second. Only items that have been there a while are compared.
            const string settledDemoItems = "project = LGHTHSDMO AND created <= \"-15m\"";
            var inEnglish = await Refreshing(english, englishConnection, board, settledDemoItems);
            var inGerman = await Refreshing(german, germanConnection, board, settledDemoItems);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inEnglish, Is.Not.Empty);
                Assert.That(inGerman.Select(item => item.ReferenceId), Is.EquivalentTo(inEnglish.Select(item => item.ReferenceId)));

                foreach (var item in inGerman)
                {
                    var same = inEnglish.Single(other => other.ReferenceId == item.ReferenceId);

                    Assert.That(item.State, Is.EqualTo(same.State), item.ReferenceId);
                    Assert.That(item.Type, Is.EqualTo(same.Type), item.ReferenceId);
                    Assert.That(item.StartedDate, Is.EqualTo(same.StartedDate), item.ReferenceId);
                    Assert.That(item.ClosedDate, Is.EqualTo(same.ClosedDate), item.ReferenceId);
                }
            }
        }

        [Test]
        public async Task The_item_in_the_custom_German_status_has_that_state_through_both_accounts()
        {
            var board = await english.GetBoardInformation(englishConnection, ScrumBoard);

            var inEnglish = await Refreshing(english, englishConnection, board, "project = JUSTATEST");
            var inGerman = await Refreshing(german, germanConnection, board, "project = JUSTATEST");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inEnglish.Single(item => item.ReferenceId == "JUSTATEST-14").State, Is.EqualTo("In Prüfung"));
                Assert.That(inGerman.Single(item => item.ReferenceId == "JUSTATEST-14").State, Is.EqualTo("In Prüfung"));
                Assert.That(inGerman.Select(item => (item.ReferenceId, item.State)), Is.EquivalentTo(inEnglish.Select(item => (item.ReferenceId, item.State))));
            }
        }

        private static async Task<List<WorkItem>> Refreshing(
            JiraWorkTrackingConnector connector, WorkTrackingSystemConnection connection, BoardInformation board, string query)
        {
            var team = new Team
            {
                Name = "Original names",
                DataRetrievalValue = query,
                WorkTrackingSystemConnection = connection,
                WorkTrackingSystemConnectionId = connection.Id,
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.AddRange(board.WorkItemTypes);
            team.ToDoStates.Clear();
            team.ToDoStates.AddRange(board.ToDoStates);
            team.DoingStates.Clear();
            team.DoingStates.AddRange(board.DoingStates);
            team.DoneStates.Clear();
            team.DoneStates.AddRange(board.DoneStates);

            return (await connector.GetWorkItemsForTeam(team, CancellationToken.None)).ToList();
        }

        private static JiraWorkTrackingConnector ASubject()
            => new(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                TestAuthStrategyFactory.CreateRealFactory(new FakeCryptoService()),
                new Lighthouse.Backend.Cache.Cache<string, object>(),
                new DeliveryForecastBlockRenderer(),
                TimeProvider.System);

        private static WorkTrackingSystemConnection AConnection(int id, string username, string apiToken)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = id,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = $"Original names {id}",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = OrganizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            return connection;
        }
    }
}
