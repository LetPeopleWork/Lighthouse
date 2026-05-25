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
    [TestFixture]
    public class LinearWorkTrackingConnectorHistoryParsingTest
    {
        private const string TeamId = "team-uuid";
        private const string TeamName = "Demo";
        private const string EnteredActiveAtIso = "2026-05-20T09:00:00.000Z";

        [Test]
        public void SupportsTransitionHistory_ReturnsTrue()
        {
            var subject = CreateSubject(new Mock<HttpMessageHandler>().Object);

            Assert.That(subject.SupportsTransitionHistory, Is.True);
        }

        [Test]
        public async Task GetWorkItemsForTeam_HistorySupplied_MapsRealTransitionsToMappedStates()
        {
            var handler = HandlerReturning(_ => IssuesResponseWithHistory());

            var subject = CreateSubject(handler);
            var team = CreateTeam();

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            var issue = workItems.Single(w => w.ReferenceId == "lig-1");
            var transition = issue.SyncedTransitions.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.SyncedTransitions, Has.Count.EqualTo(1));
                Assert.That(transition.FromState, Is.EqualTo("To Do"));
                Assert.That(transition.ToState, Is.EqualTo("In Progress"));
                Assert.That(transition.TransitionedAt, Is.EqualTo(new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        [TestCaseSource(nameof(HistoryRejectionErrors))]
        public async Task GetWorkItemsForTeam_HistoryQueryRejected_ReQueriesWithoutHistoryAndYieldsEmptySyncedTransitions(string historyErrorResponse)
        {
            var requestBodies = new List<string>();
            var handler = HandlerReturning(body =>
            {
                requestBodies.Add(body);
                return body.Contains("history", StringComparison.Ordinal)
                    ? historyErrorResponse
                    : IssuesResponseWithoutHistory();
            });

            var subject = CreateSubject(handler);
            var team = CreateTeam();

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            var issue = workItems.Single(w => w.ReferenceId == "lig-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Has.Count.EqualTo(1),
                    "When the history-carrying query is rejected the connector must re-query without history so work items still sync.");
                Assert.That(issue.SyncedTransitions, Is.Empty,
                    "After the per-connection downgrade, issues must yield no synced transitions so RefreshWorkItems falls through to the sync-delta path.");
                Assert.That(requestBodies.Any(b => !b.Contains("history", StringComparison.Ordinal)), Is.True,
                    "A rejected history query fails the whole request, so the connector must re-issue it without the history connection.");
            }
        }

        private static IEnumerable<string> HistoryRejectionErrors()
        {
            yield return HistoryFieldValidationError();
            yield return QueryTooComplexError();
        }

        private static HttpMessageHandler HandlerReturning(Func<string, string> bodyForRequest)
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
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(ResponseFor(requestBody, bodyForRequest), Encoding.UTF8, "application/json"),
                    };
                });
            return mock.Object;
        }

        private static string ResponseFor(string requestBody, Func<string, string> bodyForRequest)
        {
            if (requestBody.Contains("teams", StringComparison.Ordinal) && !requestBody.Contains("issues", StringComparison.Ordinal))
            {
                return TeamsResponse();
            }

            return bodyForRequest(requestBody);
        }

        private static string TeamsResponse()
        {
            return $@"{{ ""data"": {{ ""teams"": {{ ""nodes"": [ {{ ""id"": ""{TeamId}"", ""name"": ""{TeamName}"" }} ], ""pageInfo"": {{ ""hasNextPage"": false, ""endCursor"": null }} }} }} }}";
        }

        private static string IssuesResponseWithHistory()
        {
            return $@"{{ ""data"": {{ ""team"": {{ ""id"": ""{TeamId}"", ""name"": ""{TeamName}"", ""issues"": {{ ""nodes"": [ {{
                ""id"": ""issue-1"",
                ""title"": ""First Issue"",
                ""identifier"": ""LIG-1"",
                ""url"": ""https://linear.app/demo/issue/LIG-1"",
                ""number"": ""1"",
                ""sortOrder"": 1.0,
                ""createdAt"": ""2026-05-19T00:00:00.000Z"",
                ""startedAt"": ""{EnteredActiveAtIso}"",
                ""completedAt"": null,
                ""state"": {{ ""id"": ""s-active"", ""name"": ""Active"" }},
                ""history"": {{ ""nodes"": [
                    {{ ""createdAt"": ""{EnteredActiveAtIso}"", ""fromState"": {{ ""name"": ""New"" }}, ""toState"": {{ ""name"": ""Active"" }} }}
                ] }}
            }} ], ""pageInfo"": {{ ""hasNextPage"": false, ""endCursor"": null }} }} }} }} }}";
        }

        private static string IssuesResponseWithoutHistory()
        {
            return $@"{{ ""data"": {{ ""team"": {{ ""id"": ""{TeamId}"", ""name"": ""{TeamName}"", ""issues"": {{ ""nodes"": [ {{
                ""id"": ""issue-1"",
                ""title"": ""First Issue"",
                ""identifier"": ""LIG-1"",
                ""url"": ""https://linear.app/demo/issue/LIG-1"",
                ""number"": ""1"",
                ""sortOrder"": 1.0,
                ""createdAt"": ""2026-05-19T00:00:00.000Z"",
                ""startedAt"": ""{EnteredActiveAtIso}"",
                ""completedAt"": null,
                ""state"": {{ ""id"": ""s-active"", ""name"": ""Active"" }}
            }} ], ""pageInfo"": {{ ""hasNextPage"": false, ""endCursor"": null }} }} }} }} }}";
        }

        private static string HistoryFieldValidationError()
        {
            return @"{ ""errors"": [ { ""message"": ""Cannot query field \""history\"" on type \""Issue\""."", ""extensions"": { ""code"": ""GRAPHQL_VALIDATION_FAILED"" } } ], ""data"": null }";
        }

        private static string QueryTooComplexError()
        {
            return @"{ ""errors"": [ { ""message"": ""Query too complex"", ""extensions"": { ""type"": ""invalid input"", ""code"": ""INPUT_ERROR"", ""userPresentableMessage"": ""The query is too complex. Complexity: 17201.2. Maximum allowed complexity: 10000."" } } ], ""data"": null }";
        }

        private static Team CreateTeam()
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

            var team = new Team
            {
                Name = "Demo Team",
                DataRetrievalValue = TeamName,
                WorkTrackingSystemConnection = connection,
                StateMappings =
                [
                    new StateMapping { Name = "To Do", States = ["New"] },
                    new StateMapping { Name = "In Progress", States = ["Active"] },
                ],
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

        private static LinearWorkTrackingConnector CreateSubject(HttpMessageHandler handler)
        {
            return new LinearWorkTrackingConnector(
                Mock.Of<ILogger<LinearWorkTrackingConnector>>(),
                new FakeCryptoService(),
                handler);
        }
    }
}
