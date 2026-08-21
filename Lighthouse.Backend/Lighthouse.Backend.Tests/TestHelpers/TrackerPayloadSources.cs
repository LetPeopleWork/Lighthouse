using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using TrackedFeature = Lighthouse.Backend.Tests.API.Integration.Dependencies.DependenciesAcceptanceTest.TrackedFeature;
using ITrackerPayloadSource = Lighthouse.Backend.Tests.API.Integration.Dependencies.DependenciesAcceptanceTest.ITrackerPayloadSource;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// One work tracking system each, driven for real: the scenario writes what its Features are called
    /// and what each waits on, this turns that into the payload the tracker would actually return, and
    /// the tracker's own connector maps it. What comes back is what a refresh would have in hand.
    ///
    /// Every tracker names its Features differently - Azure DevOps by a number, Jira by a project key,
    /// Linear by the id of a Project - so a scenario names them by something readable and asks the
    /// tracker what it calls that one.
    /// </summary>
    public static class TrackerPayloadSources
    {
        public static IEnumerable<ITrackerPayloadSource> EveryTrackerThatCanExpressADependency()
        {
            yield return new AzureDevOpsPayload();
            yield return new JiraPayload();
            yield return new LinearPayload();
        }

        /// <summary>
        /// Reference ids handed out in the order a scenario first mentions a Feature, so the same scenario
        /// produces the same ids on every run without anyone writing tracker-shaped ids into it.
        /// </summary>
        private abstract class NamesItsFeaturesItsOwnWay : ITrackerPayloadSource
        {
            private readonly Dictionary<string, string> referenceIdByName = [];

            public string ReferenceIdFor(string logicalName)
            {
                if (!referenceIdByName.TryGetValue(logicalName, out var referenceId))
                {
                    referenceId = TheNextReferenceId(referenceIdByName.Count);
                    referenceIdByName[logicalName] = referenceId;
                }

                return referenceId;
            }

            public abstract Task<List<Feature>> Map(TrackedFeature[] rows);

            protected abstract string TheNextReferenceId(int howManyAlreadyNamed);

            public override string ToString() => GetType().Name.Replace("Payload", string.Empty, StringComparison.Ordinal);
        }

        private sealed class AzureDevOpsPayload : NamesItsFeaturesItsOwnWay
        {
            private const string PredecessorLinkType = "System.LinkTypes.Dependency-Reverse";

            protected override string TheNextReferenceId(int howManyAlreadyNamed) => $"{100 + howManyAlreadyNamed}";

            public override async Task<List<Feature>> Map(TrackedFeature[] rows)
            {
                var byId = rows.ToDictionary(row => int.Parse(row.ReferenceId, System.Globalization.CultureInfo.InvariantCulture));
                var client = AClientAnsweringWith(byId);

                return await new RecordedAzureDevOpsConnector(client).GetFeaturesForProject(APortfolio());
            }

            private static WorkItemTrackingHttpClient AClientAnsweringWith(Dictionary<int, TrackedFeature> byId)
            {
                var clientMock = new Mock<WorkItemTrackingHttpClient>(new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

                clientMock
                    .Setup(client => client.QueryByWiqlAsync(It.IsAny<Wiql>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new WorkItemQueryResult
                    {
                        WorkItems = byId.Keys.Select(id => new WorkItemReference { Id = id }).ToList(),
                    });

                clientMock
                    .Setup(client => client.GetWorkItemFieldsAsync(It.IsAny<GetFieldsExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);

                clientMock
                    .Setup(client => client.GetRevisionsAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);

                clientMock
                    .Setup(client => client.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<WorkItemErrorPolicy?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns((IEnumerable<int> ids, IEnumerable<string> _, DateTime? _, WorkItemExpand? expand, WorkItemErrorPolicy? _, object _, CancellationToken _) =>
                        Task.FromResult(ids
                            .Where(byId.ContainsKey)
                            .Select(id => expand == WorkItemExpand.Relations ? TheRelationsOf(byId[id]) : ThePayloadOf(byId[id]))
                            .ToList()));

                return clientMock.Object;
            }

            private static AdoWorkItem ThePayloadOf(TrackedFeature row)
            {
                var id = int.Parse(row.ReferenceId, System.Globalization.CultureInfo.InvariantCulture);

                var item = new AdoWorkItem
                {
                    Id = id,
                    Links = new ReferenceLinks(),
                    Fields = new Dictionary<string, object>
                    {
                        [AzureDevOpsFieldNames.Id] = id,
                        [AzureDevOpsFieldNames.State] = "Active",
                        [AzureDevOpsFieldNames.Title] = row.Name,
                        [AzureDevOpsFieldNames.WorkItemType] = "Feature",
                        [AzureDevOpsFieldNames.CreatedDate] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        [AzureDevOpsFieldNames.StackRank] = $"{id}",
                    },
                };

                item.Links.AddLink(AzureDevOpsFieldNames.UrlPropertyName, $"https://dev.azure.com/lighthouse-test/_workitems/edit/{id}");

                return item;
            }

            private static AdoWorkItem TheRelationsOf(TrackedFeature row) => new()
            {
                Id = int.Parse(row.ReferenceId, System.Globalization.CultureInfo.InvariantCulture),
                Relations = [.. Array.ConvertAll(row.WaitsOn, waitsOn => new WorkItemRelation
                {
                    Rel = PredecessorLinkType,
                    Url = $"https://dev.azure.com/lighthouse-test/_apis/wit/workItems/{waitsOn}",

                    // The parent is read off the same relations, and it reads every relation's attributes
                    // before looking at its type. A relation without them takes the whole refresh down.
                    Attributes = new Dictionary<string, object>(),
                })],
            };

            private static Portfolio APortfolio()
            {
                var connection = new WorkTrackingSystemConnection
                {
                    Id = 1,
                    Name = "Azure DevOps",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                };

                var portfolio = new Portfolio
                {
                    Id = 1,
                    Name = "TestProject",
                    DataRetrievalValue = "[System.TeamProject] = 'TestProject'",
                    WorkTrackingSystemConnectionId = 1,
                    WorkTrackingSystemConnection = connection,
                };

                ItsTypeAndStates(portfolio, "Feature", "Active");

                return portfolio;
            }
        }

        private sealed class JiraPayload : NamesItsFeaturesItsOwnWay
        {
            protected override string TheNextReferenceId(int howManyAlreadyNamed) => $"PARITY-{1 + howManyAlreadyNamed}";

            public override async Task<List<Feature>> Map(TrackedFeature[] rows)
            {
                var handler = HandlerReturning(request =>
                {
                    var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                    if (path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal))
                    {
                        return "{\"deploymentType\":\"Cloud\"}";
                    }

                    if (path.EndsWith("rest/api/latest/field", StringComparison.Ordinal))
                    {
                        return "[]";
                    }

                    if (path.Contains("/search", StringComparison.Ordinal))
                    {
                        return "{\"issues\":[" + string.Join(",", Array.ConvertAll(rows, AnEpic)) + "],\"isLast\":true}";
                    }

                    return "{}";
                });

                var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
                ItsTypeAndStates(portfolio, "Epic", "In Progress");

                return await JiraConnectorTestSetup.AConnectorOver(handler).GetFeaturesForProject(portfolio);
            }

            private static string AnEpic(TrackedFeature row)
            {
                var links = Array.ConvertAll(
                    row.WaitsOn,
                    waitsOn => "{\"type\": {\"inward\": \"is blocked by\", \"outward\": \"blocks\"}"
                        + ", \"inwardIssue\": {\"key\": \"" + waitsOn + "\"}}");

                var fields = "{\"summary\": \"" + row.Name + "\""
                    + ", \"issuetype\": {\"name\": \"Epic\"}"
                    + ", \"status\": {\"name\": \"In Progress\"}"
                    + ", \"created\": \"2026-01-01T00:00:00.000+0000\""
                    + ", \"labels\": []"
                    + ", \"issuelinks\": [" + string.Join(",", links) + "]}";

                return "{\"key\": \"" + row.ReferenceId + "\", \"fields\": " + fields + "}";
            }
        }

        private sealed class LinearPayload : NamesItsFeaturesItsOwnWay
        {
            protected override string TheNextReferenceId(int howManyAlreadyNamed)
                => $"00000000-0000-0000-0000-{1 + howManyAlreadyNamed:D12}";

            public override async Task<List<Feature>> Map(TrackedFeature[] rows)
            {
                var blockedByEachOther = TheInverseRelationsOf(rows);
                var projects = Array.ConvertAll(rows, row => AProject(row, blockedByEachOther[row.ReferenceId]));
                var body = "{\"data\": {\"projects\": {\"nodes\": [" + string.Join(",", projects) + "]"
                    + ", \"pageInfo\": {\"hasNextPage\": false, \"endCursor\": null}}}}";

                var handler = HandlerReturning(_ => body);
                var portfolio = APortfolio();

                return await new LinearWorkTrackingConnector(
                    Mock.Of<ILogger<LinearWorkTrackingConnector>>(), new FakeCryptoService(), handler)
                    .GetFeaturesForProject(portfolio);
            }

            /// <summary>
            /// Linear hands the relation to the Project that is waiting through its inverse relations, and
            /// names the other end as the relation's source. Building it that way round here is the point:
            /// a payload written the near way would pass a mapper that reads the near side.
            /// </summary>
            private static Dictionary<string, List<string>> TheInverseRelationsOf(TrackedFeature[] rows)
            {
                var blockersOf = rows.ToDictionary(row => row.ReferenceId, row => new List<string>());

                foreach (var row in rows)
                {
                    foreach (var waitsOn in row.WaitsOn)
                    {
                        blockersOf[row.ReferenceId].Add(waitsOn);
                    }
                }

                return blockersOf;
            }

            private static string AProject(TrackedFeature row, List<string> blockedBy)
            {
                var nodes = blockedBy.ConvertAll(
                    blockerId => "{\"type\": \"blocks\", \"project\": {\"id\": \"" + blockerId + "\"}}");

                return "{\"id\": \"" + row.ReferenceId + "\""
                    + ", \"name\": \"" + row.Name + "\""
                    + ", \"status\": {\"id\": \"s1\", \"name\": \"Active\"}"
                    + ", \"url\": \"https://linear.app/" + row.ReferenceId + "\""
                    + ", \"sortOrder\": 1.0"
                    + ", \"createdAt\": \"2026-01-01T00:00:00.000Z\""
                    + ", \"inverseRelations\": {\"nodes\": [" + string.Join(",", nodes) + "]}}";
            }

            private static Portfolio APortfolio()
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

                var portfolio = new Portfolio
                {
                    Name = "Demo Portfolio",
                    WorkTrackingSystemConnection = connection,
                    StateMappings = [new StateMapping { Name = "In Progress", States = ["Active"] }],
                };

                ItsTypeAndStates(portfolio, "Project", "In Progress");

                return portfolio;
            }
        }

        private static void ItsTypeAndStates(Portfolio portfolio, string workItemType, string doingState)
        {
            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add(workItemType);

            portfolio.ToDoStates.Clear();
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add(doingState);
            portfolio.DoneStates.Clear();
        }

        private static HttpMessageHandler HandlerReturning(Func<HttpRequestMessage, string> bodyFor)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(bodyFor(request), Encoding.UTF8, "application/json"),
                    }));

            return mock.Object;
        }
    }
}
