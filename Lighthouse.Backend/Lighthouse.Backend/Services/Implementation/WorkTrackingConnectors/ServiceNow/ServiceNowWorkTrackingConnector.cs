using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // The imperative shell around the two pure cores (ADR-114): this class talks HTTP and nothing
    // else decides anything. ValidateConnection hands ServiceNowValidationVerdict three scalars,
    // ValidateTeamSettings hands ServiceNowTeamQueryVerdict two counts, and GetWorkItemsForTeam
    // hands ServiceNowWorkItemMapper one record at a time. Portfolios and write-back refuse out
    // loud (DoD 5) and permanently — ITSM has no rollup to forecast over (SPIKE Q5).
    public class ServiceNowWorkTrackingConnector(
        ILogger<ServiceNowWorkTrackingConnector> logger,
        IWorkTrackingAuthStrategyFactory authStrategyFactory,
        HttpMessageHandler? httpMessageHandlerForTesting = null)
        : IServiceNowWorkTrackingConnector
    {
        private readonly ILogger<ServiceNowWorkTrackingConnector> logger = logger;

        private readonly IWorkTrackingAuthStrategyFactory authStrategyFactory = authStrategyFactory;

        private readonly HttpMessageHandler? httpMessageHandlerForTesting = httpMessageHandlerForTesting;

        private const string WorkItemReadingUnavailableMessage =
            "Reading portfolio work from ServiceNow is not supported. ServiceNow's ITSM tables carry no parent " +
            "record Lighthouse could roll a portfolio up over, so this direction is declined rather than " +
            "half-built. Team-level work is read normally.";

        private const string WriteBackUnsupportedMessage =
            "Lighthouse does not write back to ServiceNow, and will not. Reading work data is the only " +
            "supported direction for this connection.";

        /// <summary>The header the Table API reports the size of the whole result set in.</summary>
        private const string TotalCountHeader = "X-Total-Count";

        private const string ResultProperty = "result";

        /// <summary>Everything a count probe needs: one row, and the header that comes with it.</summary>
        private const string SingleRowParameter = "sysparm_limit=1";

        /// <summary>
        /// <c>sysparm_display_value=all</c> is what makes the slice possible on a read-only account:
        /// it returns every field as <c>{ display_value, value }</c>, so the state label a flow coach
        /// maps arrives with the record and no <c>sys_choice</c> access is needed (the Q10
        /// correction). The limit is a request, not a promise — the instance caps its own pages.
        /// </summary>
        private const string RecordPageParameters = "sysparm_display_value=all&sysparm_limit=100";

        /// <summary>The header the Table API names its paging relations in (SPIKE, findings.md).</summary>
        private const string PagingLinkHeader = "Link";

        /// <summary>Encoded-query operator that fixes the order the Table API returns rows in.</summary>
        private const string OrderByClause = "^ORDERBY";

        /// <summary>
        /// Row creation is monotonic and never rewritten, so ordering by it keeps the rows already
        /// read at the front of the result set even while the table is being written to.
        /// </summary>
        private const string StableOrderField = ServiceNowWorkItemMapper.CreatedField;

        /// <summary>
        /// The tie-breaker that makes the sort total. <c>sys_created_on</c> has one-second resolution
        /// and a bulk write lands many rows on the same second, so on its own it orders ties
        /// arbitrarily and offset paging drops whatever the next page shuffled past the boundary.
        /// </summary>
        private const string TieBreakerField = ServiceNowWorkItemMapper.RecordIdField;

        /// <summary>
        /// The last brake, for an instance that reports no result-set size at all. At the requested
        /// page size this is a hundred thousand records — far past any ITSM table Lighthouse is
        /// pointed at, and still finite.
        /// </summary>
        private const int PageCeiling = 1000;

        /// <summary>The table that says what an instance measures.</summary>
        private const string MetricDefinitionTable = "metric_definition";

        /// <summary>The table the measured spans themselves live in.</summary>
        private const string MetricInstanceTable = "metric_instance";

        // Per instance, and only ever on evidence (ADR-118): "not observed" until a definition read
        // actually succeeded. Linear can afford an optimistic default and downgrade on rejection;
        // here the optimistic assumption would be that a customer paid for a role nobody checked.
        private ServiceNowHistoryAvailability? observedAvailability;

        public bool SupportsTransitionHistory(WorkTrackingSystemConnection connection)
        {
            return observedAvailability == ServiceNowHistoryAvailability.Available;
        }

        public IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection)
        {
            return [];
        }

        public async Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            var instanceUrl = GetOptionValue(connection, ServiceNowWorkTrackingOptionNames.InstanceUrl);

            if (!TryCreateProbeUri(instanceUrl, ServiceNowReadScope.RootTable, out var probeUri))
            {
                return ServiceNowValidationVerdict.FromInvalidInstanceAddress(instanceUrl);
            }

            try
            {
                var answer = await Read(probeUri, connection);
                var body = ParseRecords(answer.Body);

                var verdict = ServiceNowValidationVerdict.FromResponse(
                    answer.StatusCode, body.ResponseIsJson, body.Records.Count, ServiceNowReadScope.RootTable);

                // The capability question is only worth answering for a connection that already works
                // — every failure rung above returns before it, so a closed port stays
                // connection_failed.
                return verdict.IsValid
                    ? ServiceNowHistoryVerdict.HistoryIsDecidedPerTeam()
                    : verdict;
            }
            catch (HttpRequestException exception)
            {
                return UnreachableInstance(exception, instanceUrl);
            }
            catch (TaskCanceledException exception)
            {
                return UnreachableInstance(exception, instanceUrl);
            }
        }

        // AC1. The query the flow coach wrote is the query that gets asked, and a team that has not
        // written one reads nothing rather than everything: asking the Table API with no query at
        // all returns the whole table, which is how a team ends up reporting the whole instance.
        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var teamsOwnQuery = team.DataRetrievalValue;

            if (string.IsNullOrWhiteSpace(teamsOwnQuery))
            {
                logger.LogWarning(
                    "Team {TeamName} has no ServiceNow query, so no work was read. Asking the table without one would return every record in it.",
                    team.Name);

                return [];
            }

            var connection = team.WorkTrackingSystemConnection;
            var scope = ServiceNowReadScope.For(team.WorkItemTypes);

            // ADR-123 decision 4: the missing-query rule above, on the kind-of-work dimension.
            // Unfiltered, the same team read 579 records of 13 kinds where it wanted 159 of 2.
            if (scope.NamesNoKindsOfWork)
            {
                logger.LogWarning(
                    "Team {TeamName} has not said which kinds of work are its own, so no work was read from the ServiceNow table {Table}. Asking that table without naming them would return every kind in it.",
                    team.Name, ServiceNowReadScope.RootTable);

                return [];
            }

            var records = (await ReadEveryPage(
                connection, ServiceNowReadScope.RootTable, scope.ScopedQuery(teamsOwnQuery), WhenRefused.Fail)).Records;

            var mapped = records
                .Select(record => new MappedRecord(
                    ServiceNowWorkItemMapper.ReadStateLabel(record),
                    ServiceNowWorkItemMapper.ReadRecordId(record),
                    ServiceNowWorkItemMapper.MapRecord(record, team, ServiceNowReadScope.RootTable)))
                .ToList();

            ReportStatesTheTeamNeverMapped(mapped, team);

            var history = await ReadHistory(connection, scope, mapped, team);

            // Linear's precedent: a team only sees work in the states it has mapped. An unmapped
            // label is work the flow coach never told Lighthouse how to interpret.
            return mapped
                .Where(entry => entry.Item.StateCategory != StateCategories.Unknown)
                .Select(entry => AsWorkItem(entry, history, team))
                .ToList();
        }

        // DoD 5 forbids the silent no-op. A flow coach types state labels by hand against a choice
        // list a read-only account cannot query, so a near-miss is the likely case — and dropping
        // those records without a word reads as low Throughput with the settings page still saying
        // the team is valid.
        private void ReportStatesTheTeamNeverMapped(List<MappedRecord> mapped, Team team)
        {
            var leftOut = mapped
                .Where(entry => entry.Item.StateCategory == StateCategories.Unknown)
                .ToList();

            if (leftOut.Count < 1)
            {
                return;
            }

            // Stryker disable once Linq: the order only exists so a support log reads the same twice;
            // descending is equally canonical and names the same labels.
            var labels = leftOut
                .Select(entry => string.IsNullOrWhiteSpace(entry.Label) ? "(no state)" : entry.Label)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);

            logger.LogWarning(
                "Left {LeftOutCount} ServiceNow records out of team {TeamName} because their states are not mapped on the team: {UnmappedStates}. Add these labels to the team's state mapping, or its metrics will read low.",
                leftOut.Count, team.Name, string.Join(", ", labels));
        }

        // ADR-118 D2. The measurement is resolved before any span is asked for: without it there is
        // nothing to restrict the span read to, and an unrestricted read turns assignment changes
        // into state transitions. What the instance answers here is also the only evidence
        // SupportsTransitionHistory ever answers from.
        private async Task<Dictionary<string, List<ServiceNowStateSpan>>> ReadHistory(
            WorkTrackingSystemConnection connection, ServiceNowReadScope scope, List<MappedRecord> mapped, Team team)
        {
            var recordIds = mapped
                .Select(entry => entry.RecordId)
                .Where(recordId => !string.IsNullOrWhiteSpace(recordId))
                .ToList();

            if (recordIds.Count < 1)
            {
                // An idIN list with no ids is an unfiltered read of every span in the instance.
                return [];
            }

            var definitions = await ReadStateSpanDefinitions(connection, scope.DefinitionTables());
            observedAvailability = definitions.Availability;

            if (definitions.Availability != ServiceNowHistoryAvailability.Available)
            {
                ReportHistoryUnavailable(team, definitions.Availability);

                return [];
            }

            return await ReadSpans(connection, recordIds, definitions.Ids, team);
        }

        // One request per batch of records rather than one per record: at ~600ms per call with no
        // rate limiting (SPIKE Q7) the constraint is wall clock, and one call per work item would
        // turn a 500-item sync into five minutes.
        private async Task<Dictionary<string, List<ServiceNowStateSpan>>> ReadSpans(
            WorkTrackingSystemConnection connection, List<string> recordIds, List<string> stateSpanDefinitions, Team team)
        {
            var spansByRecord = new Dictionary<string, List<ServiceNowStateSpan>>(StringComparer.Ordinal);

            foreach (var batch in ServiceNowHistoryQuery.IntoBatches(recordIds))
            {
                var read = await ReadEveryPage(
                    connection,
                    MetricInstanceTable,
                    ServiceNowHistoryQuery.SpanQueryFor(batch, stateSpanDefinitions),
                    WhenRefused.Downgrade);

                if (!read.CarriesRecords)
                {
                    observedAvailability = ServiceNowHistoryVerdict.From(
                        read.StatusCode, read.CarriesRecords, stateSpanDefinitions.Count);
                    ReportHistoryUnavailable(team, observedAvailability.Value);

                    return [];
                }

                foreach (var span in ServiceNowHistoryQuery.SpansFrom(read.Records, stateSpanDefinitions))
                {
                    KeepAgainstItsRecord(spansByRecord, span, team);
                }
            }

            return spansByRecord;
        }

        // Bug #5621 F1. A field_value_duration definition does not have to be one on the state
        // field: the stock incident table measures assignment_group, assigned_to and active the same
        // way, and DefinitionQueryFor cannot exclude them, because the state field is named
        // differently on each record class. So the span read brings back spans that are not state
        // spans, and a record that has only those must be ABSENT here rather than present and
        // unreadable -- absence is what the date lookups answer with the record's own opened_at and
        // closed_at, while presence made them report null for a record whose dates were sitting in
        // the answer the connector already held.
        private static void KeepAgainstItsRecord(
            Dictionary<string, List<ServiceNowStateSpan>> spansByRecord, ServiceNowStateSpan span, Team team)
        {
            if (team.MapStateToStateCategory(span.Label) == StateCategories.Unknown)
            {
                return;
            }

            if (!spansByRecord.TryGetValue(span.RecordId, out var recordsSpans))
            {
                recordsSpans = [];
                spansByRecord[span.RecordId] = recordsSpans;
            }

            recordsSpans.Add(span);
        }

        // ADR-118 D5: capability is read from the instance, never inferred — the administrator's
        // validation and the sync ask this same question of the same table.
        private async Task<StateSpanDefinitions> ReadStateSpanDefinitions(
            WorkTrackingSystemConnection connection, List<string> definitionTables)
        {
            var read = await ReadEveryPage(
                connection, MetricDefinitionTable, ServiceNowHistoryQuery.DefinitionQueryFor(definitionTables), WhenRefused.Downgrade);

            var definitionIds = read.Records
                .Select(ServiceNowWorkItemMapper.ReadRecordId)
                .Where(definitionId => !string.IsNullOrWhiteSpace(definitionId))
                .ToList();

            return new StateSpanDefinitions(
                ServiceNowHistoryVerdict.From(read.StatusCode, read.CarriesRecords, definitionIds.Count), definitionIds);
        }

        // The moves a record made, or none where the instance measured none. Set as the WorkItem is
        // constructed because SyncedTransitions is init-only.
        private static IReadOnlyList<WorkItemStateTransition> MovesMadeBy(
            MappedRecord entry, Dictionary<string, List<ServiceNowStateSpan>> history, Team team)
        {
            return history.TryGetValue(entry.RecordId, out var spans)
                ? ServiceNowStateSpanMapper.ToTransitions(spans, team)
                : [];
        }

        private static WorkItem AsWorkItem(
            MappedRecord entry, Dictionary<string, List<ServiceNowStateSpan>> history, Team team)
        {
            var (startedDate, closedDate) = DatesFor(entry, history, team);

            return new WorkItem(entry.Item, team)
            {
                SyncedTransitions = MovesMadeBy(entry, history, team),
                StartedDate = startedDate,
                ClosedDate = closedDate,
            };
        }

        // ADR-118 decision 7 and ADR-117 decision 1 (amended 2026-07-31), through the same rules Jira
        // and Azure DevOps date work by (Bug #5621 F2). Where the record's state spans were measured
        // they decide; where none were, ADR-117's request-logged instant and `closed_at` stand,
        // inflated by queue time and still the only thing a read-only account can support.
        private static (DateTime? startedDate, DateTime? closedDate) DatesFor(
            MappedRecord entry, Dictionary<string, List<ServiceNowStateSpan>> history, Team team)
        {
            if (!history.TryGetValue(entry.RecordId, out var spans))
            {
                return (entry.Item.StartedDate, entry.Item.ClosedDate);
            }

            // Only work the team maps to Done carries a finish date at all, which MapRecord already
            // applied to the fallback and the spans do not know about.
            var closedDate = entry.Item.StateCategory == StateCategories.Done
                ? ServiceNowStateSpanMapper.WhenWorkFinished(spans, team)
                : null;

            var startedDate = ServiceNowStateSpanMapper.WhenWorkStarted(spans, team);
            var returnedToTheQueue = ServiceNowStateSpanMapper.WhenWorkWasQueued(spans, team);

            if (returnedToTheQueue.HasValue && startedDate.HasValue && returnedToTheQueue > startedDate)
            {
                startedDate = null;
            }

            // Finished without ever being observed in Doing -- a desk that resolves straight out of
            // the queue. The cycle is zero rather than absent, because a null start drops the item
            // out of Cycle Time altogether and work that demonstrably finished belongs in it.
            return (startedDate ?? closedDate, closedDate);
        }

        // DoD 5 forbids the silent no-op: a team quietly losing time-in-state reads as a team whose
        // work never moves, and nothing else would tell the administrator why.
        private void ReportHistoryUnavailable(Team team, ServiceNowHistoryAvailability availability)
        {
            logger.LogWarning(
                "ServiceNow supplied no transition history for team {TeamName} ({Reason}), so its state changes are derived from Lighthouse's own sync interval rather than from the instance. Validate the connection to see what to change.",
                team.Name, availability);
        }

        // Offset paging over disjoint pages, at ~600ms per call with no rate limiting (SPIKE Q7) —
        // so the cost is wall-clock and the read is batched. Where the instance sends paging links
        // they decide the next address and the last page; otherwise the offset advances by the rows
        // that actually came back, because a real instance caps its own pages.
        //
        // The two guards below exist because the failure they catch is silent. An instance that
        // ignores sysparm_offset answers every page with the same rows: with X-Total-Count present
        // that duplicates the team's work, and without it the loop never ends.
        private async Task<PagedRead> ReadEveryPage(
            WorkTrackingSystemConnection connection, string table, string query, WhenRefused whenRefused)
        {
            var instanceUrl = GetOptionValue(connection, ServiceNowWorkTrackingOptionNames.InstanceUrl);
            var records = new List<JsonElement>();
            var alreadyRead = new HashSet<string>(StringComparer.Ordinal);

            var pageUri = PageUriFor(instanceUrl, table, query, offset: 0);
            var pagesRead = 0;
            var pagesAllowed = PageCeiling;

            while (pageUri is not null)
            {
                var answer = await Read(pageUri, connection);

                // A history read carries the refusal home instead of throwing on it: a role revoked
                // after the connection validated must downgrade the sync, not fail it (ADR-118 D5).
                // The refusal is any answer that is not a readable record set, not merely a bad status
                // (Bug #5621) -- the sign-in page ADR-114 exists for arrives as a 200 and used to
                // reach RecordsFrom, which throws, taking the whole team's sync with it.
                if (whenRefused == WhenRefused.Downgrade && !CarriesAReadableRecordSet(answer))
                {
                    return new PagedRead(answer.StatusCode, records, CarriesRecords: false);
                }

                var page = RecordsFrom(answer, table);

                if (page.Count < 1)
                {
                    break;
                }

                GuardAgainstRepeatedRecords(page, alreadyRead, table);
                records.AddRange(page);
                pagesRead++;

                if (pagesRead == 1)
                {
                    pagesAllowed = PagesAllowed(answer.TotalCount, page.Count);
                }

                pageUri = FollowingPage(answer, instanceUrl, table, query, records.Count);

                if (pageUri is not null && pagesRead >= pagesAllowed)
                {
                    throw ServiceNowReadException.PagingDidNotTerminate(table, pagesRead, records.Count);
                }
            }

            return new PagedRead(HttpStatusCode.OK, records);
        }

        // One error policy for the whole read: anything other than a 200 carrying a record set
        // throws. Returning the pages that happened to succeed is indistinguishable from a team
        // whose query matched less work than before, and RefreshWorkItems deletes every stored item
        // the sync did not bring back — so a 403 halfway through would destroy the team's history
        // and restoring the credential would not bring it back.
        // The same two conditions RecordsFrom throws on, asked instead of thrown, so a read that
        // was told to downgrade can act on them.
        private static bool CarriesAReadableRecordSet(ServiceNowAnswer answer)
        {
            return answer.StatusCode == HttpStatusCode.OK && ParseRecords(answer.Body).CarriesRecords;
        }

        private static List<JsonElement> RecordsFrom(ServiceNowAnswer answer, string table)
        {
            var body = ParseRecords(answer.Body);

            if (answer.StatusCode != HttpStatusCode.OK)
            {
                throw new ServiceNowReadException(
                    ServiceNowValidationVerdict.FromResponse(answer.StatusCode, body.ResponseIsJson, body.Records.Count, table));
            }

            if (!body.CarriesRecords)
            {
                // A success carrying no record set at all — an SSO login page, an error envelope, a
                // gateway's own body. To the ladder that is the same answer as a body that was not
                // JSON: the instance said yes and returned no data.
                throw new ServiceNowReadException(
                    ServiceNowValidationVerdict.FromResponse(HttpStatusCode.OK, responseIsJson: false, rowCount: 0, table));
            }

            return body.Records;
        }

        // Identity is sys_id, never number. `number` is not unique on a real instance — measured on
        // the PDI, change_request held 118 rows with 113 distinct numbers — and tripping this guard
        // aborts the whole team's sync, so one collision anywhere would cost a customer every work
        // item on that team rather than the colliding pair.
        private static void GuardAgainstRepeatedRecords(List<JsonElement> page, HashSet<string> alreadyRead, string table)
        {
            foreach (var record in page)
            {
                var identity = ServiceNowWorkItemMapper.ReadRecordId(record);

                if (!alreadyRead.Add(string.IsNullOrWhiteSpace(identity) ? record.GetRawText() : identity))
                {
                    throw ServiceNowReadException.RepeatedAPage(table);
                }
            }
        }

        // Derived from what the instance itself said the result set holds, with two pages of slack
        // for a table that grew while it was being read.
        private static int PagesAllowed(int? totalCount, int rowsInTheFirstPage)
        {
            if (totalCount is null || rowsInTheFirstPage < 1)
            {
                return PageCeiling;
            }

            return Math.Min(PageCeiling, (totalCount.Value / rowsInTheFirstPage) + 2);
        }

        private static Uri? FollowingPage(
            ServiceNowAnswer answer, string instanceUrl, string table, string query, int recordsSoFar)
        {
            if (answer.Paging == PagingSignal.NextPage)
            {
                return answer.NextPage;
            }

            if (answer.Paging == PagingSignal.LastPage)
            {
                return null;
            }

            if (answer.TotalCount is not null && recordsSoFar >= answer.TotalCount)
            {
                return null;
            }

            return PageUriFor(instanceUrl, table, query, recordsSoFar);
        }

        private static Uri PageUriFor(string instanceUrl, string table, string query, int offset)
        {
            var parameters =
                $"{RecordPageParameters}&sysparm_offset={offset.ToString(CultureInfo.InvariantCulture)}&sysparm_query={Uri.EscapeDataString(InAStableOrder(query))}";

            if (!TryCreateTableUri(instanceUrl, table, parameters, out var pageUri))
            {
                throw ServiceNowReadException.InvalidInstanceAddress(instanceUrl);
            }

            return pageUri;
        }

        // Offset paging is only safe over a stable order, and an incident table on a live instance
        // is neither ordered nor still: a record created between two pages shifts the window and the
        // rows it pushed past the boundary are never read. RefreshWorkItems then deletes them and
        // re-creates them on a later sync without their history.
        //
        // The order has to be TOTAL, not merely stable. Measured on the PDI: 159 records over 98
        // distinct sys_created_on values, up to 10 sharing one second, and pages 1 and 2 overlapped
        // by one sys_id — one row unread, and the repeat of the other one trips the guard below and
        // fails the whole team's sync. With sys_id appended the two pages overlap by none.
        //
        // Appended unconditionally, including over a team's own ORDERBY (Bug #5621): encoded queries
        // chain ORDERBY terms, so the team's order stays primary and only gains a tie-breaker. Skipping
        // it there left the sort non-total for exactly the queries ServiceNow's own *Copy query* hands
        // a coach, since a list's current sort travels with the query it copies.
        private static string InAStableOrder(string query)
        {
            return $"{query}{OrderByClause}{StableOrderField}{OrderByClause}{TieBreakerField}";
        }

        public Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            throw new NotSupportedException(WorkItemReadingUnavailableMessage);
        }

        public Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            throw new NotSupportedException(WorkItemReadingUnavailableMessage);
        }

        // Two pre-flight rules the instance is never asked about, then everything that needs it.
        public async Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            var teamsOwnQuery = team.DataRetrievalValue;

            if (string.IsNullOrWhiteSpace(teamsOwnQuery))
            {
                return ServiceNowTeamQueryVerdict.FromMissingQuery();
            }

            var connection = team.WorkTrackingSystemConnection;
            var instanceUrl = GetOptionValue(connection, ServiceNowWorkTrackingOptionNames.InstanceUrl);
            var scope = ServiceNowReadScope.For(team.WorkItemTypes);

            if (scope.NamesNoKindsOfWork)
            {
                return ServiceNowTeamQueryVerdict.FromMissingWorkItemTypes();
            }

            try
            {
                return await AskTheInstanceAboutTheTeam(connection, instanceUrl, scope, teamsOwnQuery);
            }
            catch (HttpRequestException exception)
            {
                return UnreachableInstance(exception, instanceUrl);
            }
            catch (TaskCanceledException exception)
            {
                return UnreachableInstance(exception, instanceUrl);
            }
        }

        // AC6. Two probes, because one cannot tell a silently-widened query from a correct one —
        // both answer 200 with rows. The comparison IS the detection.
        private async Task<ConnectionValidationResult> AskTheInstanceAboutTheTeam(
            WorkTrackingSystemConnection connection, string instanceUrl, ServiceNowReadScope scope, string teamsOwnQuery)
        {
            var unreadable = await FirstUnreadableKindOfWork(connection, instanceUrl, scope);

            if (unreadable is not null)
            {
                return unreadable;
            }

            // ADR-124 decision 3: both counts are scoped to the kinds of work the team named, so
            // the ratio keeps meaning "how much of your work did this query select" rather than
            // "how much of the instance".
            var matched = await CountRows(
                connection, instanceUrl, ServiceNowReadScope.RootTable, scope.ScopedQuery(teamsOwnQuery));

            if (matched.Problem is not null)
            {
                return matched.Problem;
            }

            var everything = await CountRows(
                connection, instanceUrl, ServiceNowReadScope.RootTable, scope.BaselineQuery());

            if (everything.Problem is not null)
            {
                return everything.Problem;
            }

            return ServiceNowTeamQueryVerdict.FromTeamProbe(
                ServiceNowReadScope.RootTable, matched.Count, everything.Count);
        }

        // ADR-124 decision 1. One cheap probe per named kind of work, at the one moment a human is
        // already waiting on a Save, and never on a refresh. Serial and uncapped (OQ-5): a fan-out
        // here would be the only concurrent call path in this adapter, against an instance whose
        // rate-limiting behaviour is measured at exactly one request rate.
        private async Task<ConnectionValidationResult?> FirstUnreadableKindOfWork(
            WorkTrackingSystemConnection connection, string instanceUrl, ServiceNowReadScope scope)
        {
            foreach (var recordClass in scope.KindsOfWork)
            {
                var unreadable = await WhyThisKindOfWorkCannotBeRead(connection, instanceUrl, recordClass);

                if (unreadable is not null)
                {
                    return unreadable;
                }
            }

            return null;
        }

        // ADR-124 decision 2, re-ordered 2026-07-31. The probe that runs first is the one that asks
        // about the read Lighthouse actually performs — does this class contribute rows under `task`
        // — rather than the proxy question "is this name a readable table somewhere". A right
        // configuration therefore costs ONE request per kind of work, and only a class the hierarchy
        // holds none of pays for a second to explain why: misspelt, not work at all, or genuinely
        // empty everywhere, which is accepted (OQ-8).
        private async Task<ConnectionValidationResult?> WhyThisKindOfWorkCannotBeRead(
            WorkTrackingSystemConnection connection, string instanceUrl, string recordClass)
        {
            if (!TryCreateClassInTheHierarchyProbeUri(instanceUrl, recordClass, out var inTheHierarchyUri)
                || !TryCreateProbeUri(instanceUrl, recordClass, out var ownTableUri))
            {
                return ServiceNowValidationVerdict.FromInvalidInstanceAddress(instanceUrl);
            }

            var inTheHierarchy = await Probe(connection, inTheHierarchyUri);

            var contributes = ServiceNowTeamQueryVerdict.FromWorkHierarchyProbe(
                recordClass,
                ServiceNowReadScope.RootTable,
                inTheHierarchy.StatusCode,
                inTheHierarchy.CarriesRecords,
                inTheHierarchy.RecordsTheInstanceHolds,
                inTheHierarchy.VisibleRowCount);

            if (!contributes.IsValid)
            {
                return contributes;
            }

            if (inTheHierarchy.RecordsTheInstanceHolds is not 0)
            {
                return null;
            }

            var onItsOwnTable = await Probe(connection, ownTableUri);

            var kindOfWork = ServiceNowTeamQueryVerdict.FromClassTableProbe(
                recordClass,
                ServiceNowReadScope.RootTable,
                onItsOwnTable.StatusCode,
                onItsOwnTable.CarriesRecords,
                onItsOwnTable.RecordsTheInstanceHolds,
                onItsOwnTable.VisibleRowCount);

            return kindOfWork.IsValid ? null : kindOfWork;
        }

        private async Task<ProbedTable> Probe(WorkTrackingSystemConnection connection, Uri probeUri)
        {
            var answer = await Read(probeUri, connection);
            var body = ParseRecords(answer.Body);

            return new ProbedTable(answer.StatusCode, body.CarriesRecords, answer.TotalCount, body.Records.Count);
        }

        // One row is asked for and the size of the whole result set is read from the header, so a
        // comparison costs two rows rather than two table scans. Anything other than a readable 200
        // is not a query problem at all, and routes through slice 01's ladder — a rights failure
        // keeps its own name instead of being reported as a badly written query.
        private async Task<(ConnectionValidationResult? Problem, int Count)> CountRows(
            WorkTrackingSystemConnection connection, string instanceUrl, string table, string query)
        {
            var parameters = $"{SingleRowParameter}&sysparm_query={Uri.EscapeDataString(query)}";

            if (!TryCreateTableUri(instanceUrl, table, parameters, out var countUri))
            {
                return (ServiceNowValidationVerdict.FromInvalidInstanceAddress(instanceUrl), 0);
            }

            var answer = await Read(countUri, connection);
            var body = ParseRecords(answer.Body);

            if (answer.StatusCode != HttpStatusCode.OK || !body.ResponseIsJson)
            {
                return (ServiceNowValidationVerdict.FromResponse(
                    answer.StatusCode, body.ResponseIsJson, body.Records.Count, table), 0);
            }

            // The probe asks for one row, so the body can only ever say 0 or 1 and the header is the
            // only place the size of the result set comes from. Substituting the row count when the
            // header is missing makes every team look like matched == total == 1, which reads as a
            // query that selects the whole table and refuses every team on the instance.
            if (answer.TotalCount is null)
            {
                return (ServiceNowTeamQueryVerdict.FromUncountableResultSet(table), 0);
            }

            return (null, answer.TotalCount.Value);
        }

        // SPIKE Q5: no rollup exists to forecast over, so this refusal is settled rather than pending.
        public Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio)
        {
            return Task.FromResult(ConnectionValidationResult.Failure(
                "portfolio_not_supported",
                // Stryker disable String: the refusal code above is what callers branch on and what
                // PointingAPortfolioAtServiceNow_IsRefusedWithAnActionableReason asserts. The five lines
                // below are one concatenated explanation, so blanking any single fragment still leaves a
                // non-empty message; catching it would mean pinning the copy word for word, which turns
                // every wording change into a test change while protecting no behaviour.
                "Portfolios are not supported for ServiceNow, and are not planned. ServiceNow's ITSM tables carry " +
                "no parent record that Lighthouse could forecast a portfolio over: parent references sit empty in " +
                "practice, and the project, portfolio and demand tables are not exposed to reporting credentials. " +
                "Rather than show you a portfolio forecast it cannot compute, Lighthouse declines. Point this " +
                "portfolio at another work tracking system."));
            // Stryker restore String
        }

        public Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            throw new NotSupportedException(WriteBackUnsupportedMessage);
        }

        private async Task<ServiceNowAnswer> Read(Uri uri, WorkTrackingSystemConnection connection)
        {
            var authStrategy = authStrategyFactory.Resolve(ResolveAuthenticationMethodKey(connection));

            using var client = CreateHttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            await authStrategy.ApplyAsync(request, connection, CancellationToken.None);

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var (paging, nextPage) = ReadPagingLinks(response, uri);

            return new ServiceNowAnswer(response.StatusCode, body, ReadTotalCount(response), paging, nextPage);
        }

        // AC7. The instance caps its own page regardless of the requested sysparm_limit and reports
        // the true size of the result set here. A pager that trusts its own limit stops early, and
        // the team's Throughput then reads low with nothing anywhere reporting a failure.
        private static int? ReadTotalCount(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(TotalCountHeader, out var values))
            {
                return null;
            }

            // A negative count is not a count. Treating it as one would make the team-settings
            // comparison compare against nonsense instead of refusing to compare at all.
            if (!int.TryParse(values.FirstOrDefault(), CultureInfo.InvariantCulture, out var totalCount) || totalCount < 0)
            {
                return null;
            }

            return totalCount;
        }

        // The SPIKE measured a Link header carrying rel="first"/"next"/"last". Where the instance
        // sends it, it is the only paging signal that stays right when the instance caps its own
        // page or ignores the offset that was asked for — so it decides both the next address and
        // when there is no next address.
        private static (PagingSignal Signal, Uri? NextPage) ReadPagingLinks(HttpResponseMessage response, Uri requestedUri)
        {
            if (!response.Headers.TryGetValues(PagingLinkHeader, out var values))
            {
                return (PagingSignal.NoLinks, null);
            }

            var carriesANextRelation = false;

            foreach (var header in values)
            {
                var (mentionsNext, nextPage) = NextLinkIn(header, requestedUri);
                carriesANextRelation |= mentionsNext;

                if (nextPage is not null)
                {
                    return (PagingSignal.NextPage, nextPage);
                }
            }

            // A Link header naming a next page Lighthouse will not follow is not evidence that this
            // was the last page, so paging falls back to the count rather than stopping short.
            return (carriesANextRelation ? PagingSignal.NoLinks : PagingSignal.LastPage, null);
        }

        private static (bool MentionsNext, Uri? NextPage) NextLinkIn(string header, Uri requestedUri)
        {
            var mentionsNext = false;
            var position = 0;

            while (position < header.Length)
            {
                var open = header.IndexOf('<', position);
                var close = open < 0 ? -1 : header.IndexOf('>', open);

                if (close < 0)
                {
                    break;
                }

                var following = header.IndexOf('<', close);
                var relation = following < 0 ? header[close..] : header[close..following];

                if (DescribesTheNextPage(relation))
                {
                    mentionsNext = true;

                    if (IsOnTheSameInstance(header[(open + 1)..close], requestedUri, out var nextPage))
                    {
                        return (true, nextPage);
                    }
                }

                position = close + 1;
            }

            return (mentionsNext, null);
        }

        private static bool DescribesTheNextPage(string relation)
        {
            return relation
                .Replace("\"", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Contains("rel=next", StringComparison.OrdinalIgnoreCase);
        }

        // The next page is followed blind, so it may only ever point back at the instance that was
        // asked. Anywhere else is a redirect Lighthouse has no reason to send a credential to.
        private static bool IsOnTheSameInstance(string target, Uri requestedUri, [NotNullWhen(true)] out Uri? nextPage)
        {
            nextPage = null;

            if (!Uri.TryCreate(target, UriKind.Absolute, out var candidate)
                || candidate.Scheme != requestedUri.Scheme
                || !candidate.Authority.Equals(requestedUri.Authority, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            nextPage = candidate;

            return true;
        }

        private enum PagingSignal
        {
            /// <summary>The instance sent no usable Link header, so paging falls back to the count.</summary>
            NoLinks,

            /// <summary>The instance sent paging links and none of them names a next page.</summary>
            LastPage,

            /// <summary>The instance named the next page itself.</summary>
            NextPage,
        }

        private sealed record ServiceNowAnswer(
            HttpStatusCode StatusCode, string Body, int? TotalCount, PagingSignal Paging, Uri? NextPage);

        /// <summary>What a read does with an answer that is not a readable 200.</summary>
        private enum WhenRefused
        {
            /// <summary>Throw. A partial team sync deletes the work the failed pages would have carried.</summary>
            Fail,

            /// <summary>Carry the status back, so the caller downgrades instead of failing (ADR-118 D5).</summary>
            Downgrade,
        }

        /// <summary>Every page of a read, and the status the read ended on.</summary>
        // CarriesRecords is false only where a downgrading read gave up on the answer: it is the
        // difference between "the instance holds no rows" and "the instance did not answer with rows".
        private sealed record PagedRead(
            HttpStatusCode StatusCode, List<JsonElement> Records, bool CarriesRecords = true);

        /// <summary>
        /// One <c>sysparm_limit=1</c> probe, reduced to the four scalars a verdict is read from. The
        /// gap between what the instance says it holds and what came back is the whole mechanism
        /// (ADR-124).
        /// </summary>
        private sealed record ProbedTable(
            HttpStatusCode StatusCode, bool CarriesRecords, int? RecordsTheInstanceHolds, int VisibleRowCount);

        /// <summary>One record as mapped, kept alongside the handle its history is fetched by.</summary>
        private sealed record MappedRecord(string Label, string RecordId, WorkItemBase Item);

        /// <summary>The definitions measuring state on a table, and what their read says the instance can supply.</summary>
        private sealed record StateSpanDefinitions(ServiceNowHistoryAvailability Availability, List<string> Ids);

        /// <summary>
        /// What a body turned out to be. The two booleans are not the same question and every caller
        /// has to pick: <c>ResponseIsJson</c> is "it parsed", <c>CarriesRecords</c> is "it parsed and
        /// held a record set". A class probe that settles for the first passes an error envelope
        /// wearing a 200 as a readable class.
        /// </summary>
        private sealed record ServiceNowBody(bool ResponseIsJson, bool CarriesRecords, List<JsonElement> Records);

        // ADR-114: whether the body is JSON is decided by parsing it, never by Content-Type —
        // ServiceNow's gateway owns that header, and the body is parsed anyway to count rows.
        // The elements outlive the document they were parsed from, so each one is cloned.
        private static ServiceNowBody ParseRecords(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty(ResultProperty, out var rows)
                    || rows.ValueKind != JsonValueKind.Array)
                {
                    return new ServiceNowBody(ResponseIsJson: true, CarriesRecords: false, []);
                }

                return new ServiceNowBody(
                    ResponseIsJson: true,
                    CarriesRecords: true,
                    rows.EnumerateArray().Select(row => row.Clone()).ToList());
            }
            catch (JsonException)
            {
                return new ServiceNowBody(ResponseIsJson: false, CarriesRecords: false, []);
            }
        }

        private static bool TryCreateProbeUri(string instanceUrl, string table, [NotNullWhen(true)] out Uri? probeUri)
        {
            // No sysparm_fields: field projection was never measured against ACL row filtering
            // (SPIKE Q8), and this probe exists to distrust exactly that substrate.
            return TryCreateTableUri(instanceUrl, table, SingleRowParameter, out probeUri);
        }

        // The clause comes from the same pure function the read emits, so the probe cannot drift
        // into a form the sync never asks in — and one class always means the measured `=` shape.
        private static bool TryCreateClassInTheHierarchyProbeUri(
            string instanceUrl, string recordClass, [NotNullWhen(true)] out Uri? probeUri)
        {
            var classClause = ServiceNowReadScope.Matching(ServiceNowWorkItemMapper.RecordClassField, [recordClass]);

            return TryCreateTableUri(
                instanceUrl,
                ServiceNowReadScope.RootTable,
                $"{SingleRowParameter}&sysparm_query={Uri.EscapeDataString(classClause)}",
                out probeUri);
        }

        private static bool TryCreateTableUri(string instanceUrl, string table, string parameters, [NotNullWhen(true)] out Uri? tableUri)
        {
            tableUri = null;

            if (!Uri.TryCreate(instanceUrl, UriKind.Absolute, out var instanceUri))
            {
                return false;
            }

            if (instanceUri.Scheme != Uri.UriSchemeHttp && instanceUri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            return Uri.TryCreate(
                $"{instanceUrl.TrimEnd('/')}/api/now/table/{Uri.EscapeDataString(table)}?{parameters}",
                UriKind.Absolute,
                out tableUri);
        }

        private static string ResolveAuthenticationMethodKey(WorkTrackingSystemConnection connection)
        {
            return string.IsNullOrWhiteSpace(connection.AuthenticationMethodKey)
                ? AuthenticationMethodKeys.ServiceNowBasic
                : connection.AuthenticationMethodKey;
        }

        private static string GetOptionValue(WorkTrackingSystemConnection connection, string key)
        {
            return connection.Options.Find(option => option.Key == key)?.Value ?? string.Empty;
        }

        private HttpClient CreateHttpClient()
        {
            // Stryker disable Boolean: disposeHandler is test-seam plumbing, not behaviour. It only
            // matters on the branch a test reaches, where it lets one shared stub handler outlive the
            // client that borrowed it; in production the field is null and that branch never runs. The
            // scope is Boolean on purpose — which branch is taken is real behaviour and stays mutated.
            return httpMessageHandlerForTesting is null
                ? new HttpClient()
                : new HttpClient(httpMessageHandlerForTesting, disposeHandler: false);
            // Stryker restore Boolean
        }

        private ConnectionValidationResult UnreachableInstance(Exception exception, string instanceUrl)
        {
            // Stryker disable once all: diagnostic log text is not behaviour, and neither is the warning
            // being emitted — what the administrator sees is the verdict returned below.
            logger.LogWarning(exception, "Could not reach ServiceNow instance {InstanceUrl}", instanceUrl);

            return ServiceNowValidationVerdict.FromUnreachableInstance(exception.Message);
        }
    }
}
