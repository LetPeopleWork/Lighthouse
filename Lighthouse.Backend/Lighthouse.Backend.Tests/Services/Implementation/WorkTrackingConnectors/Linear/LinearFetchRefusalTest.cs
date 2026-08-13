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
        [Test]
        public void GetWorkItemsForTeam_RefusesWhenLinearWillNotAnswer()
        {
            var subject = ALinearThat(WillNotAnswer());

            Assert.That(async () => await subject.GetWorkItemsForTeam(ATeamOnLinear()),
                Throws.Exception,
                "A workspace that cannot be reached is not a workspace with no issues. Answering with no "
                + "records hands removal an empty query, which deletes every Work Item the team has.");
        }

        [Test]
        public void GetFeaturesForProject_RefusesWhenLinearWillNotAnswer()
        {
            var subject = ALinearThat(WillNotAnswer());

            Assert.That(async () => await subject.GetFeaturesForProject(APortfolioOnLinear()),
                Throws.Exception,
                "On the portfolio half an empty answer strips every Feature's portfolio claim, and the "
                + "orphaned-Feature cleanup then deletes outright whatever no portfolio still claims.");
        }

        [Test]
        public async Task GetFeaturesForProject_StillAnswersWithNoRecordsWhenTheWorkspaceGenuinelyHasNoProjects()
        {
            var subject = ALinearThat(AnsweringWith(NoProjects()));

            var features = await subject.GetFeaturesForProject(APortfolioOnLinear());

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

        private static string NoProjects()
        {
            return @"{ ""data"": { ""projects"": { ""nodes"": [], ""pageInfo"": { ""hasNextPage"": false, ""endCursor"": null } } } }";
        }

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

        private static LinearWorkTrackingConnector ALinearThat(HttpMessageHandler handler)
        {
            return new LinearWorkTrackingConnector(
                Mock.Of<ILogger<LinearWorkTrackingConnector>>(),
                new FakeCryptoService(),
                handler);
        }
    }
}
