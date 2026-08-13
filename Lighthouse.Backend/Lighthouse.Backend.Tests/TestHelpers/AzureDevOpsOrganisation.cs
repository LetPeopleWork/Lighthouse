using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    internal sealed record PayloadRead(List<int> Ids, List<string> Fields, WorkItemExpand? Expand);

    /// <summary>
    /// The real connector over a recording client. Only the client is substituted: the queries, the field
    /// lists, the chunking and the conversion are all the production code path.
    /// </summary>
    internal sealed class RecordedAzureDevOpsConnector(WorkItemTrackingHttpClient client) : AzureDevOpsWorkTrackingConnector(
        Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(),
        Mock.Of<IWorkTrackingAuthStrategyFactory>())
    {
        internal override Task<WorkItemTrackingHttpClient> GetWorkItemTrackingHttpClientAsync(WorkTrackingSystemConnection workTrackingSystemConnection)
            => Task.FromResult(client);
    }

    /// <summary>
    /// An Azure DevOps organisation that holds a fixed set of ids: every WIQL answers with all of them,
    /// every payload read answers for the ids it was given, and every revision read answers with the same
    /// three state changes. What is worth asserting is not what comes back but what was asked for, so every
    /// request is recorded.
    ///
    /// Each round trip can also be told to fail, because the whole-query fetch has to tell "the query
    /// matched nothing" apart from "we could not ask" - reading the second as the first removes every
    /// record the team has.
    /// </summary>
    internal sealed class AzureDevOpsOrganisation
    {
        internal const string TheTeamsFilter = "[System.TeamProject] = 'TestProject'";

        internal static readonly DateTime WhenTheTrackerSaysItLastChanged = new(2026, 8, 5, 14, 30, 0, DateTimeKind.Utc);

        public AzureDevOpsOrganisation(int[] itemIds)
        {
            var clientMock = new Mock<WorkItemTrackingHttpClient>(new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

            clientMock
                .Setup(client => client.QueryByWiqlAsync(It.IsAny<Wiql>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns((Wiql wiql, bool? _, int? _, object _, CancellationToken _) =>
                {
                    WiqlQueries.Add(wiql.Query);

                    if (RejectTheQuery)
                    {
                        throw new VssServiceException("The query could not be run.");
                    }

                    return Task.FromResult(new WorkItemQueryResult
                    {
                        WorkItems = AnswerTheQueryWithoutAResultSet
                            ? null
                            : WhatTheQueryAnswersWith(wiql.Query, itemIds),
                    });
                });

            clientMock
                .Setup(client => client.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<WorkItemErrorPolicy?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns((IEnumerable<int> ids, IEnumerable<string> fields, DateTime? _, WorkItemExpand? expand, WorkItemErrorPolicy? _, object _, CancellationToken _) =>
                {
                    var askedFor = ids.ToList();
                    PayloadReads.Add(new PayloadRead(askedFor, fields?.ToList() ?? [], expand));

                    if (RejectPayloadReads)
                    {
                        throw new VssServiceException("The batch contains an id that no longer exists.");
                    }

                    return Task.FromResult(AnswerPayloadReadsWithNothing
                        ? []
                        : askedFor.ConvertAll(AnItem));
                });

            clientMock
                .Setup(client => client.GetWorkItemFieldsAsync(It.IsAny<GetFieldsExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    FieldLookups++;

                    if (RejectTheFieldLookup)
                    {
                        throw new VssServiceException("The field definitions could not be read.");
                    }

                    return Task.FromResult(new List<WorkItemField2>());
                });

            clientMock
                .Setup(client => client.GetRevisionsAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns((int id, int? _, int? _, WorkItemExpand? _, object _, CancellationToken _) =>
                {
                    RevisionReads.Add(id);
                    return Task.FromResult(TheRevisionsOf(id));
                });

            Client = clientMock.Object;
        }

        public WorkItemTrackingHttpClient Client { get; }

        public List<string> WiqlQueries { get; } = [];

        public List<PayloadRead> PayloadReads { get; } = [];

        public List<int> RevisionReads { get; } = [];

        /// <summary>The stamp every item carries. Null stands for a tracker that reports no stamp at all.</summary>
        public DateTime? ChangedDate { get; set; } = WhenTheTrackerSaysItLastChanged;

        /// <summary>A payload read that answers for none of the ids it was given - a rejected batch.</summary>
        public bool AnswerPayloadReadsWithNothing { get; set; }

        /// <summary>A payload read the tracker refuses outright - a batch naming an id deleted since the query ran.</summary>
        public bool RejectPayloadReads { get; set; }

        /// <summary>A tracker that will not run the query at all - an expired token, a timeout, a blip.</summary>
        public bool RejectTheQuery { get; set; }

        /// <summary>A tracker that answers the query with no result set rather than with an empty one.</summary>
        public bool AnswerTheQueryWithoutAResultSet { get; set; }

        /// <summary>
        /// A tracker that will not answer for its field definitions. The lookup precedes every payload read,
        /// so it fails after the query already succeeded - the shape that reads as "the query matched nothing".
        /// </summary>
        public bool RejectTheFieldLookup { get; set; }

        /// <summary>
        /// How often the connector asked the organisation for its field definitions. Counted because it is
        /// the request that hides: it precedes every payload read, and a test counting only payload reads
        /// reports "nothing was fetched" for a cycle that still went to the tracker.
        /// </summary>
        public int FieldLookups { get; private set; }

        /// <summary>Every request of any kind, so a test can say that none was made.</summary>
        public List<string> EveryRequestMade =>
        [
            .. WiqlQueries,
            .. PayloadReads.Select(read => $"payload:{read.Ids.Count}"),
            .. RevisionReads.Select(id => $"revisions:{id}"),
            .. Enumerable.Range(0, FieldLookups).Select(_ => "fields"),
        ];

        /// <summary>
        /// A full download reads twice: once for the fields it maps, and once more with the relations
        /// expanded to find each item's parent. Only the first one carries a field list.
        /// </summary>
        public List<string> FieldsOfTheItemRead => PayloadReads.Find(read => read.Fields.Count > 0)?.Fields ?? [];

        public List<int> EveryIdRead => [.. PayloadReads.SelectMany(read => read.Ids).Distinct()];

        /// <summary>The connector, the team it fetches for, and the organisation both are pointed at.</summary>
        internal static (AzureDevOpsWorkTrackingConnector Subject, Team Team, AzureDevOpsOrganisation Ado) AnAzureDevOpsThatHolds(params int[] itemIds)
        {
            var ado = new AzureDevOpsOrganisation(itemIds);

            return (new RecordedAzureDevOpsConnector(ado.Client), ATeamOnAzureDevOps(), ado);
        }

        internal static (AzureDevOpsWorkTrackingConnector Subject, Portfolio Portfolio, AzureDevOpsOrganisation Ado) AnAzureDevOpsPortfolioThatHolds(params int[] itemIds)
        {
            var ado = new AzureDevOpsOrganisation(itemIds);

            return (new RecordedAzureDevOpsConnector(ado.Client), APortfolioOnAzureDevOps(), ado);
        }

        private static Team ATeamOnAzureDevOps()
        {
            var team = new Team
            {
                Id = 1,
                Name = "TestTeam",
                DataRetrievalValue = TheTeamsFilter,
                WorkTrackingSystemConnectionId = 1,
                WorkTrackingSystemConnection = AConnection(),
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("User Story");

            team.ToDoStates.Clear();
            team.ToDoStates.Add("New");
            team.DoingStates.Clear();
            team.DoingStates.Add("Active");
            team.DoneStates.Clear();
            team.DoneStates.Add("Closed");

            return team;
        }

        private static Portfolio APortfolioOnAzureDevOps()
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "TestProject",
                DataRetrievalValue = TheTeamsFilter,
                WorkTrackingSystemConnectionId = 1,
                WorkTrackingSystemConnection = AConnection(),
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Feature");

            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("New");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("Active");
            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Closed");

            return portfolio;
        }

        private static WorkTrackingSystemConnection AConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = 1,
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = "https://dev.azure.com/lighthouse-test", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = "encrypted-token", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "1", IsSecret = false },
            ]);

            return connection;
        }

        /// <summary>
        /// Every query answers with everything the organisation holds, except one that names ids: a query
        /// keyed on <c>[System.Id] = n</c> answers with those ids only. Without that, a keyed fetch and a
        /// whole-query fetch would be indistinguishable here, and a fetch that quietly re-enumerates the
        /// team would look correct.
        /// </summary>
        private static List<WorkItemReference> WhatTheQueryAnswersWith(string query, int[] held)
        {
            var namedIds = IdsNamedIn(query);

            var answering = namedIds.Count == 0 ? held : [.. held.Where(namedIds.Contains)];

            return [.. answering.Select(id => new WorkItemReference { Id = id })];
        }

        private static List<int> IdsNamedIn(string query)
        {
            var named = new List<int>();

            foreach (var clause in query.Split($"[{AzureDevOpsFieldNames.Id}] =").Skip(1))
            {
                var value = new string([.. clause.TrimStart().TakeWhile(char.IsDigit)]);

                if (int.TryParse(value, out var id))
                {
                    named.Add(id);
                }
            }

            return named;
        }

        private AdoWorkItem AnItem(int id)
        {
            var fields = new Dictionary<string, object>
            {
                [AzureDevOpsFieldNames.Id] = id,
                [AzureDevOpsFieldNames.State] = "Active",
                [AzureDevOpsFieldNames.Title] = $"Item {id}",
                [AzureDevOpsFieldNames.WorkItemType] = "User Story",
                [AzureDevOpsFieldNames.CreatedDate] = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
                [AzureDevOpsFieldNames.StackRank] = $"{id}",
            };

            if (ChangedDate.HasValue)
            {
                fields[AzureDevOpsFieldNames.ChangedDate] = ChangedDate.Value;
            }

            var item = new AdoWorkItem { Id = id, Fields = fields, Links = new ReferenceLinks() };
            item.Links.AddLink(AzureDevOpsFieldNames.UrlPropertyName, $"https://dev.azure.com/lighthouse-test/_workitems/edit/{id}");

            return item;
        }

        private static List<AdoWorkItem> TheRevisionsOf(int id) =>
        [
            ARevision(id, "New", new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc)),
            ARevision(id, "Active", new DateTime(2026, 8, 5, 14, 30, 0, DateTimeKind.Utc)),
        ];

        private static AdoWorkItem ARevision(int id, string state, DateTime changedDate) => new()
        {
            Id = id,
            Fields = new Dictionary<string, object>
            {
                [AzureDevOpsFieldNames.State] = state,
                [AzureDevOpsFieldNames.ChangedDate] = changedDate,
            },
        };
    }
}
