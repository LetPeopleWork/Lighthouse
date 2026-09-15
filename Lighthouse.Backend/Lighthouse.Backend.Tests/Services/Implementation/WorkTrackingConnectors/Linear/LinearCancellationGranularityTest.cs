using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Linear
{
    /// <summary>
    /// Epic #5511 slice 04, AC-04.2 - the number, for Linear. The acceptance scenarios cancel a refresh and
    /// watch it stop, but they stop a <em>mock</em> written to honour the token; that proves the ask arrives,
    /// not that a real connector acts on it. These count the GraphQL round trips a real
    /// <c>LinearWorkTrackingConnector</c> makes against a workspace that keeps offering another page.
    ///
    /// Linear has one place where paging happens - <c>GetWithPagination</c> - and both halves of the fetch
    /// walk through it, so it is the one checkpoint that has to hold. It also has no identity sweep at all,
    /// which means every Linear cycle takes the whole-query path and there is no cheaper walk to fall back
    /// on when an operator wants it stopped.
    /// </summary>
    [TestFixture]
    [Category("epic-5511-task-manager")]
    [Category("slice-04")]
    public class LinearCancellationGranularityTest
    {
        private const int ThePageTheCancelArrivesOn = 3;

        /// <summary>
        /// The workspace stops offering pages here rather than never. An endless supply would hang a
        /// connector that ignores the token instead of failing it, and a test that hangs teaches nothing.
        /// </summary>
        private const int PagesTheWorkspaceWillOffer = 40;

        private static readonly string[] TenInitiatives =
            [.. Enumerable.Range(1, 10).Select(initiative => $"INITIATIVE-{initiative}")];

        [Test]
        public async Task GetFeaturesForProject_CancelledBeforeItStarts_NeverAsksTheWorkspaceAnything()
        {
            var workspace = new AWorkspaceThatAlwaysHasAnotherPage();
            var subject = ALinearReading(workspace);

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetFeaturesForProject(APortfolioOnLinear(), alreadyCancelled.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(workspace.Queries, Is.Zero,
                    "A refresh cancelled before it began must not spend the rate limit an operator cancelled "
                    + "to protect. Linear's is shared with CI, and the first query is already too many.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_CancelledBeforeItStarts_NeverAsksTheWorkspaceAnything()
        {
            var workspace = new AWorkspaceThatAlwaysHasAnotherPage();
            var subject = ALinearReading(workspace);

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(ATeamOnLinear(), alreadyCancelled.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(workspace.Queries, Is.Zero,
                    "The team half resolves the team by name before it pages, so it can spend a round trip "
                    + "before reaching the loop that checks anything.");
            }
        }

        [Test]
        public void GetFeaturesForProject_CancelledWhileItPages_StopsWithinOnePageRoundTrip()
        {
            using var stopAfterTheThirdPage = new CancellationTokenSource();

            var workspace = new AWorkspaceThatAlwaysHasAnotherPage();
            workspace.OnQuery = () =>
            {
                if (workspace.Queries == ThePageTheCancelArrivesOn)
                {
                    stopAfterTheThirdPage.Cancel();
                }
            };

            var subject = ALinearReading(workspace);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetFeaturesForProject(APortfolioOnLinear(), stopAfterTheThirdPage.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(workspace.Queries, Is.EqualTo(3),
                    "This is the number AC-04.2 asserts against: a refresh told to stop mid-walk stops after "
                    + "the round trip it is already in, not after the whole result set. A fourth page means "
                    + "the token never reached the request, with thirty-seven pages still on offer.");
            }
        }

        [Test]
        public void GetWorkItemsForTeam_CancelledWhileItPages_StopsWithinOnePageRoundTrip()
        {
            using var stopAfterTheThirdPage = new CancellationTokenSource();

            var workspace = new AWorkspaceThatAlwaysHasAnotherPage();
            workspace.OnQuery = () =>
            {
                if (workspace.Queries == ThePageTheCancelArrivesOn)
                {
                    stopAfterTheThirdPage.Cancel();
                }
            };

            var subject = ALinearReading(workspace);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(ATeamOnLinear(), stopAfterTheThirdPage.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(workspace.Queries, Is.EqualTo(3),
                    "The team half walks twice - once to resolve the team by name, then again through the "
                    + "issues - and the two walks are separate calls into the same loop. A test that only "
                    + "stops the portfolio half proves the checkpoint holds once, not that both halves reach "
                    + "it.");
            }
        }

        [Test]
        public void GetParentFeaturesDetails_CancelledWhileItWalksInitiatives_StopsInsteadOfCountingFailures()
        {
            using var stopAfterTheThirdInitiative = new CancellationTokenSource();

            var workspace = new AWorkspaceThatAlwaysHasAnotherPage();
            workspace.OnQuery = () =>
            {
                if (workspace.Queries == ThePageTheCancelArrivesOn)
                {
                    stopAfterTheThirdInitiative.Cancel();
                }
            };

            var subject = ALinearReading(workspace);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetParentFeaturesDetails(APortfolioOnLinear(), TenInitiatives, stopAfterTheThirdInitiative.Token),
                    Throws.InstanceOf<OperationCanceledException>(),
                    "This walk fetches one initiative per round trip inside a catch-all. Caught there, a "
                    + "cancel is counted as an initiative that could not be fetched and the walk moves on to "
                    + "the next one - a refresh told to stop that keeps going, and says nothing about it.");

                Assert.That(workspace.Queries, Is.EqualTo(3),
                    "Seven initiatives still to ask about when the operator said stop.");
            }
        }

        private static LinearWorkTrackingConnector ALinearReading(AWorkspaceThatAlwaysHasAnotherPage workspace)
            => new(Mock.Of<ILogger<LinearWorkTrackingConnector>>(), new FakeCryptoService(), workspace.Handler);

        private static Team ATeamOnLinear()
        {
            var team = new Team
            {
                Name = "Demo Team",
                DataRetrievalValue = "Demo",
                WorkTrackingSystemConnection = ALinearConnection(),
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

        private static WorkTrackingSystemConnection ALinearConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Linear,
                Name = "Linear Connection",
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = LinearWorkTrackingOptionNames.ApiKey,
                Value = "key",
                IsSecret = true,
            });

            return connection;
        }

        /// <summary>
        /// A Linear workspace that answers every query with an empty page and a cursor pointing at another
        /// one. Every GraphQL request goes to the same endpoint, so one envelope carries every shape the
        /// fetch asks for - the deserialiser takes the branch it wants and ignores the rest.
        /// </summary>
        private sealed class AWorkspaceThatAlwaysHasAnotherPage
        {
            public AWorkspaceThatAlwaysHasAnotherPage()
            {
                var mock = new Mock<HttpMessageHandler>();
                mock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>((_, token) =>
                    {
                        // The query is counted when it is asked for and only then refused, exactly as a
                        // socket would. Counting the attempt is the point: a walk that issues its next
                        // request and has it refused has still spent the rate limit an operator cancelled to
                        // protect, and a count taken after the refusal cannot tell that apart from stopping.
                        Queries++;
                        token.ThrowIfCancellationRequested();
                        OnQuery?.Invoke();

                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(AnotherPage(Queries), Encoding.UTF8, "application/json"),
                        });
                    });

                Handler = mock.Object;
            }

            public HttpMessageHandler Handler { get; }

            public int Queries { get; private set; }

            public Action? OnQuery { get; set; }

            private static string AnotherPage(int pagesSoFar)
            {
                var hasNextPage = pagesSoFar < PagesTheWorkspaceWillOffer ? "true" : "false";
                var pageInfo = $@"{{ ""hasNextPage"": {hasNextPage}, ""endCursor"": ""there-is-always-more"" }}";

                return $@"{{ ""data"": {{
                    ""projects"": {{ ""nodes"": [], ""pageInfo"": {pageInfo} }},
                    ""teams"": {{ ""nodes"": [ {{ ""id"": ""team-1"", ""name"": ""Demo"" }} ] }},
                    ""team"": {{ ""id"": ""team-1"", ""issues"": {{ ""nodes"": [], ""pageInfo"": {pageInfo} }} }}
                }} }}";
            }
        }
    }
}
