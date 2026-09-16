using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Linear
{
    /// <summary>
    /// The Linear half of the same hazard the Azure DevOps refusal tests describe: a fetch that answers with
    /// no records is read as a query that matches nothing, and removal then deletes every record the team or
    /// portfolio holds.
    ///
    /// Linear is the more exposed of the two, because it does not support the identity sweep at all - every
    /// Linear cycle takes the whole-query path, so there is no cheaper path to be on when the workspace is
    /// unreachable.
    /// </summary>
    [TestFixture]
    public class LinearFetchRefusalTest
    {
        private const string ActiveKeyId = "key-active";

        private const string OffRingKeyId = "key-not-on-the-ring";

        private const string StoredApiKey = "key";

        private static readonly byte[] ActiveKeyMaterial = Convert.FromBase64String("aXhZdXd5+OeT8kjKP2gB7UdqMEB3RY4LQMI2yffxDEw=");

        private static readonly byte[] OffRingKeyMaterial = Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg=");

        private static readonly string[] BothIssues = ["lig-1", "lig-2"];

        private static readonly string[] OnlyTheInProgressIssue = ["lig-1"];

        private static readonly string[] BothProjects = ["project-active", "project-backlog"];

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenLinearWillNotAnswer()
        {
            var subject = ALinearThat(WillNotAnswer());

            Assert.That(async () => await subject.GetWorkItemsForTeam(ATeamOnLinear(), CancellationToken.None),
                Throws.Exception,
                "A workspace that cannot be reached is not a workspace with no issues. Answering with no "
                + "records hands removal an empty query, which deletes every Work Item the team has.");
        }

        [Test]
        public void GetFeaturesForProject_RefusesWhenLinearWillNotAnswer()
        {
            var subject = ALinearThat(WillNotAnswer());

            Assert.That(async () => await subject.GetFeaturesForProject(APortfolioOnLinear(), CancellationToken.None),
                Throws.Exception,
                "On the portfolio half an empty answer strips every Feature's portfolio claim, and the "
                + "orphaned-Feature cleanup then deletes outright whatever no portfolio still claims.");
        }

        [Test]
        public async Task GetFeaturesForProject_StillAnswersWithNoRecordsWhenTheWorkspaceGenuinelyHasNoProjects()
        {
            var subject = ALinearThat(AnsweringWith(NoProjects()));

            var features = await subject.GetFeaturesForProject(APortfolioOnLinear(), CancellationToken.None);

            Assert.That(features, Is.Empty,
                "The distinction is the whole point. A portfolio whose workspace really holds no projects has "
                + "to keep answering with nothing, or removal never runs and departed Features live forever.");
        }

        [Test]
        public async Task ValidateTeamSettings_ReportsAFetchItCouldNotMakeAsAFailureRatherThanAsAnEmptyBoard()
        {
            var subject = ALinearThat(WillNotAnswer());

            var result = await subject.ValidateTeamSettings(ATeamOnLinear());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("validation_failed"),
                    "Validation reads the workspace on its own path rather than through the team fetch, so it "
                    + "has to make the same distinction independently: an unreachable workspace is not an "
                    + "empty one, and telling an operator to go and check their states hides the outage.");
            }
        }

        [Test]
        public async Task ValidatePortfolioSettings_ReportsAFetchItCouldNotMakeAsAFailureRatherThanAsAnEmptyBoard()
        {
            var subject = ALinearThat(WillNotAnswer());

            var result = await subject.ValidatePortfolioSettings(APortfolioOnLinear());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("validation_failed"),
                    "Same on the portfolio half: 'no features found' names a configuration problem, and a "
                    + "failed round trip is not one.");
            }
        }

        // The Linear client is built with the key already on it as a default header, so "it threw" and
        // "nothing went out" are two different facts here as much as they are on the strategies. A client
        // built from a key nobody could read would carry that unreadable value on every request it makes.
        [Test]
        public void GetWorkItemsForTeam_AStoredApiKeyNobodyCanRead_StopsWithoutSendingAnythingToLinear()
        {
            var requestsSentToLinear = new List<HttpRequestMessage>();
            var subject = ALinearWhoseCryptoHoldsOnlyTheActiveKey(AHandlerRecordingInto(requestsSentToLinear));

            Assert.ThrowsAsync<UnreadableSecretException>(
                () => subject.GetWorkItemsForTeam(ATeamOnLinear(ACredentialTheInstanceCannotRead()), CancellationToken.None));

            Assert.That(requestsSentToLinear, Is.Empty,
                "No request may reach Linear at all, because every one the client makes would carry the key it could not read.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_NoStatesMapped_ReadsTheTeamsIssuesRatherThanNoneOfThem()
        {
            var subject = ALinearThat(AnsweringWith(TwoIssuesAndTwoProjects()));

            var workItems = await subject.GetWorkItemsForTeam(ATeamOnLinearWithNoStatesMapped(), CancellationToken.None);

            Assert.That(workItems.Select(w => w.ReferenceId), Is.EquivalentTo(BothIssues),
                "A team that has mapped no states has said nothing about which issues it wants, which is not "
                + "the same as wanting none. Matching against an empty list matches nothing, and removal then "
                + "deletes every Work Item the team has.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_StatesMapped_KeepsOnlyTheIssuesInThoseStates()
        {
            var subject = ALinearThat(AnsweringWith(TwoIssuesAndTwoProjects()));

            var workItems = await subject.GetWorkItemsForTeam(ATeamOnLinear(), CancellationToken.None);

            Assert.That(workItems.Select(w => w.ReferenceId), Is.EquivalentTo(OnlyTheInProgressIssue),
                "Letting an unmapped state list through must not let a mapped one through too. The backlog "
                + "issue is outside the states this team mapped and has to stay out.");
        }

        [Test]
        public async Task GetFeaturesForProject_NoStatesMapped_ReadsThePortfoliosProjectsRatherThanNoneOfThem()
        {
            var subject = ALinearThat(AnsweringWith(TwoIssuesAndTwoProjects()));

            var features = await subject.GetFeaturesForProject(APortfolioOnLinearWithNoStatesMapped(), CancellationToken.None);

            Assert.That(features.Select(f => f.ReferenceId), Is.EquivalentTo(BothProjects),
                "The portfolio half already reads an unmapped state list as 'no opinion'. Both halves share "
                + "the same settings screen, so an operator who leaves the states empty must not get one "
                + "answer for their features and the opposite one for their work items.");
        }

        private static HttpMessageHandler WillNotAnswer()
        {
            return HandlerReturning(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("upstream is having a bad day", Encoding.UTF8, "text/plain"),
            });
        }

        private static HttpMessageHandler AnsweringWith(string body)
        {
            return HandlerReturning(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }

        private static HttpMessageHandler HandlerReturning(Func<string, HttpResponseMessage> responseForRequest)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    var requestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                    return responseForRequest(requestBody);
                });

            return mock.Object;
        }

        private static HttpMessageHandler AHandlerRecordingInto(List<HttpRequestMessage> requests)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    requests.Add(request);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(NoProjects(), Encoding.UTF8, "application/json"),
                    });
                });

            return mock.Object;
        }

        // A well-formed envelope naming a key the ring does not hold cannot be read on any run. Random
        // bytes would not do: garbage clears the padding and printability checks by chance roughly once in
        // a thousand tries, and a test that fails one run in a thousand teaches a reader to ignore it.
        private static string ACredentialTheInstanceCannotRead()
        {
            return SecretEnvelope.Protect("whatever-was-stored-here", OffRingKeyId, OffRingKeyMaterial).Format();
        }

        private static CryptoService ACryptoServiceHoldingOnlyTheActiveKey()
        {
            var ring = new EncryptionKeyRing(new EncryptionKey(ActiveKeyId, ActiveKeyMaterial));

            return new CryptoService(new EncryptionKeyRingHolder(ring), NullLogger<CryptoService>.Instance);
        }

        private static string NoProjects()
        {
            return @"{ ""data"": { ""projects"": { ""nodes"": [], ""pageInfo"": { ""hasNextPage"": false, ""endCursor"": null } } } }";
        }

        /// <summary>
        /// Every GraphQL request goes to the same endpoint, so one envelope can carry every shape the fetch
        /// asks for and let the deserialiser take the branch it wants. One issue and one project sit in a
        /// state the fixtures below map; the other two sit in a state they do not.
        /// </summary>
        private static string TwoIssuesAndTwoProjects()
        {
            const string LastPage = @"{ ""hasNextPage"": false, ""endCursor"": null }";

            return $@"{{ ""data"": {{
                ""teams"": {{ ""nodes"": [ {{ ""id"": ""team-1"", ""name"": ""Demo"" }} ], ""pageInfo"": {LastPage} }},
                ""team"": {{ ""id"": ""team-1"", ""name"": ""Demo"", ""issues"": {{ ""nodes"": [
                    {AnIssue("issue-1", "LIG-1", "In Progress")},
                    {AnIssue("issue-2", "LIG-2", "Backlog")}
                ], ""pageInfo"": {LastPage} }} }},
                ""projects"": {{ ""nodes"": [
                    {AProject("project-active", "In Progress")},
                    {AProject("project-backlog", "Backlog")}
                ], ""pageInfo"": {LastPage} }}
            }} }}";
        }

        private static string AnIssue(string id, string identifier, string state)
        {
            return $@"{{
                ""id"": ""{id}"",
                ""title"": ""{identifier}"",
                ""identifier"": ""{identifier}"",
                ""url"": ""https://linear.app/demo/issue/{identifier}"",
                ""number"": ""1"",
                ""sortOrder"": 1.0,
                ""createdAt"": ""2026-05-19T00:00:00.000Z"",
                ""startedAt"": null,
                ""completedAt"": null,
                ""state"": {{ ""id"": ""s-{id}"", ""name"": ""{state}"" }}
            }}";
        }

        private static string AProject(string id, string status)
        {
            return $@"{{
                ""id"": ""{id}"",
                ""name"": ""{id}"",
                ""status"": {{ ""id"": ""s-{id}"", ""name"": ""{status}"", ""type"": ""started"", ""color"": ""#000"" }},
                ""url"": ""https://linear.app/demo/project/{id}"",
                ""sortOrder"": 1.0,
                ""createdAt"": ""2026-05-19T00:00:00.000Z"",
                ""startDate"": ""2026-05-19T00:00:00.000Z"",
                ""completedAt"": null,
                ""initiatives"": {{ ""nodes"": [] }}
            }}";
        }

        private static Team ATeamOnLinearWithNoStatesMapped()
        {
            var team = ATeamOnLinear();

            team.ToDoStates.Clear();
            team.DoingStates.Clear();
            team.DoneStates.Clear();

            return team;
        }

        private static Portfolio APortfolioOnLinearWithNoStatesMapped()
        {
            var portfolio = APortfolioOnLinear();

            portfolio.ToDoStates.Clear();
            portfolio.DoingStates.Clear();
            portfolio.DoneStates.Clear();

            return portfolio;
        }

        private static Team ATeamOnLinear(string storedApiKey = StoredApiKey)
        {
            var team = new Team
            {
                Name = "Demo Team",
                DataRetrievalValue = "Demo",
                WorkTrackingSystemConnection = ALinearConnection(storedApiKey),
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Issue");
            team.ToDoStates.Clear();
            team.ToDoStates.Add("To Do");
            team.DoingStates.Clear();
            team.DoingStates.Add("In Progress");
            team.DoneStates.Clear();

            return team;
        }

        private static Portfolio APortfolioOnLinear()
        {
            var portfolio = new Portfolio
            {
                Name = "Demo Portfolio",
                WorkTrackingSystemConnection = ALinearConnection(),
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Project");
            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("To Do");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");
            portfolio.DoneStates.Clear();

            return portfolio;
        }

        private static WorkTrackingSystemConnection ALinearConnection(string storedApiKey = StoredApiKey)
        {
            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Linear,
                Name = "Linear Connection",
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = LinearWorkTrackingOptionNames.ApiKey,
                Value = storedApiKey,
                IsSecret = true,
            });

            return connection;
        }

        private static LinearWorkTrackingConnector ALinearThat(HttpMessageHandler handler)
        {
            return new LinearWorkTrackingConnector(
                Mock.Of<ILogger<LinearWorkTrackingConnector>>(),
                new FakeCryptoService(),
                handler);
        }

        private static LinearWorkTrackingConnector ALinearWhoseCryptoHoldsOnlyTheActiveKey(HttpMessageHandler handler)
        {
            return new LinearWorkTrackingConnector(
                Mock.Of<ILogger<LinearWorkTrackingConnector>>(),
                ACryptoServiceHoldingOnlyTheActiveKey(),
                handler);
        }
    }
}
