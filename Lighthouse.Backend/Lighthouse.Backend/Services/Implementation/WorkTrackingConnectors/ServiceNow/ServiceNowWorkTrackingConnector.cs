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

        // D6: transition history lives behind an itil-grade role, so v1 says no rather than guessing.
        public bool SupportsTransitionHistory(WorkTrackingSystemConnection connection)
        {
            return false;
        }

        public IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection)
        {
            return [];
        }

        public async Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            var instanceUrl = GetOptionValue(connection, ServiceNowWorkTrackingOptionNames.InstanceUrl);
            var table = ResolveWorkItemTable(connection);

            if (!TryCreateProbeUri(instanceUrl, table, out var probeUri))
            {
                return ServiceNowValidationVerdict.FromInvalidInstanceAddress(instanceUrl);
            }

            try
            {
                var answer = await Read(probeUri, connection);
                var (responseIsJson, rowCount) = ReadRows(answer.Body);

                return ServiceNowValidationVerdict.FromResponse(answer.StatusCode, responseIsJson, rowCount, table);
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
            var table = ResolveWorkItemTable(connection);
            var records = await ReadEveryPage(connection, table, teamsOwnQuery);

            // Linear's precedent: a team only sees work in the states it has mapped. An unmapped
            // label is work the flow coach never told Lighthouse how to interpret.
            return records
                .Select(record => ServiceNowWorkItemMapper.MapRecord(record, team, table))
                .Where(workItem => workItem.StateCategory != StateCategories.Unknown)
                .Select(workItem => new WorkItem(workItem, team))
                .ToList();
        }

        // Offset paging over disjoint pages, at ~600ms per call with no rate limiting (SPIKE Q7) —
        // so the cost is wall-clock and the read is batched. The offset advances by the rows the
        // instance actually returned rather than by the page size that was asked for, because a real
        // instance caps its own pages.
        private async Task<List<JsonElement>> ReadEveryPage(WorkTrackingSystemConnection connection, string table, string query)
        {
            var instanceUrl = GetOptionValue(connection, ServiceNowWorkTrackingOptionNames.InstanceUrl);
            var records = new List<JsonElement>();
            var moreToRead = true;

            while (moreToRead)
            {
                var offset = records.Count.ToString(CultureInfo.InvariantCulture);
                var parameters = $"{RecordPageParameters}&sysparm_offset={offset}&sysparm_query={Uri.EscapeDataString(query)}";

                if (!TryCreateTableUri(instanceUrl, table, parameters, out var pageUri))
                {
                    logger.LogError(
                        "'{InstanceUrl}' is not a valid ServiceNow instance address, so no work could be read from table {Table}.",
                        instanceUrl, table);

                    return records;
                }

                var answer = await Read(pageUri, connection);
                var page = ReadRecords(answer.Body);

                records.AddRange(page);
                moreToRead = page.Count > 0 && (answer.TotalCount is null || records.Count < answer.TotalCount);
            }

            return records;
        }

        public Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            throw new NotSupportedException(WorkItemReadingUnavailableMessage);
        }

        public Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            throw new NotSupportedException(WorkItemReadingUnavailableMessage);
        }

        // AC6. Two probes, because one cannot tell a silently-widened query from a correct one —
        // both answer 200 with rows. The comparison IS the detection.
        public async Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            var teamsOwnQuery = team.DataRetrievalValue;

            if (string.IsNullOrWhiteSpace(teamsOwnQuery))
            {
                return ServiceNowTeamQueryVerdict.FromMissingQuery();
            }

            var connection = team.WorkTrackingSystemConnection;
            var instanceUrl = GetOptionValue(connection, ServiceNowWorkTrackingOptionNames.InstanceUrl);
            var table = ResolveWorkItemTable(connection);

            try
            {
                var matched = await CountRows(connection, instanceUrl, table, teamsOwnQuery);

                if (matched.Problem is not null)
                {
                    return matched.Problem;
                }

                var everything = await CountRows(connection, instanceUrl, table, query: null);

                if (everything.Problem is not null)
                {
                    return everything.Problem;
                }

                return ServiceNowTeamQueryVerdict.FromTeamProbe(table, matched.Count, everything.Count);
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

        // One row is asked for and the size of the whole result set is read from the header, so a
        // comparison costs two rows rather than two table scans. Anything other than a readable 200
        // is not a query problem at all, and routes through slice 01's ladder — a rights failure
        // keeps its own name instead of being reported as a badly written query.
        private async Task<(ConnectionValidationResult? Problem, int Count)> CountRows(
            WorkTrackingSystemConnection connection, string instanceUrl, string table, string? query)
        {
            var parameters = query is null
                ? SingleRowParameter
                : $"{SingleRowParameter}&sysparm_query={Uri.EscapeDataString(query)}";

            if (!TryCreateTableUri(instanceUrl, table, parameters, out var countUri))
            {
                return (ServiceNowValidationVerdict.FromInvalidInstanceAddress(instanceUrl), 0);
            }

            var answer = await Read(countUri, connection);
            var (responseIsJson, rowCount) = ReadRows(answer.Body);

            if (answer.StatusCode != HttpStatusCode.OK || !responseIsJson)
            {
                return (ServiceNowValidationVerdict.FromResponse(answer.StatusCode, responseIsJson, rowCount, table), 0);
            }

            return (null, answer.TotalCount ?? rowCount);
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

            return new ServiceNowAnswer(response.StatusCode, body, ReadTotalCount(response));
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

            return int.TryParse(values.FirstOrDefault(), CultureInfo.InvariantCulture, out var totalCount)
                ? totalCount
                : null;
        }

        // The elements outlive the document they were parsed from, so each one is cloned.
        private static List<JsonElement> ReadRecords(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty(ResultProperty, out var rows)
                    || rows.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                return rows.EnumerateArray().Select(row => row.Clone()).ToList();
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private sealed record ServiceNowAnswer(HttpStatusCode StatusCode, string Body, int? TotalCount);

        // ADR-114: whether the body is JSON is decided by parsing it, never by Content-Type —
        // ServiceNow's gateway owns that header, and the body is parsed anyway to count rows.
        private static (bool ResponseIsJson, int RowCount) ReadRows(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("result", out var rows)
                    && rows.ValueKind == JsonValueKind.Array)
                {
                    return (true, rows.GetArrayLength());
                }

                return (true, 0);
            }
            catch (JsonException)
            {
                return (false, 0);
            }
        }

        private static bool TryCreateProbeUri(string instanceUrl, string table, [NotNullWhen(true)] out Uri? probeUri)
        {
            // No sysparm_fields: field projection was never measured against ACL row filtering
            // (SPIKE Q8), and this probe exists to distrust exactly that substrate.
            return TryCreateTableUri(instanceUrl, table, SingleRowParameter, out probeUri);
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

        private static string ResolveWorkItemTable(WorkTrackingSystemConnection connection)
        {
            var table = GetOptionValue(connection, ServiceNowWorkTrackingOptionNames.WorkItemTable);

            return string.IsNullOrWhiteSpace(table) ? ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable : table;
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
