using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // The imperative shell around ServiceNowValidationVerdict (ADR-114): one Table API probe,
    // hand (status, responseIsJson, rowCount) to the pure verdict, return what it says unchanged.
    // Slice 01 delivers ValidateConnection; every other capability refuses out loud (DoD 5).
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
            "Reading work items from ServiceNow is not available yet. This release validates a ServiceNow " +
            "connection so you know the instance and credential work; fetching work from it follows in a " +
            "later release.";

        private const string WriteBackUnsupportedMessage =
            "Lighthouse does not write back to ServiceNow, and will not. Reading work data is the only " +
            "supported direction for this connection.";

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
                var (statusCode, body) = await Probe(probeUri, connection);
                var (responseIsJson, rowCount) = ReadRows(body);

                return ServiceNowValidationVerdict.FromResponse(statusCode, responseIsJson, rowCount, table);
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

        public Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            // SCAFFOLD (DISTILL slice 02, Story #5575). One deliberately wrong item rather than a
            // throw or an empty list: it is the record the team never mapped, and it carries a
            // fabricated transition. Every slice-02 assertion — the counts, the state filter, and
            // the "no invented history" rule of AC5 — therefore fails at its own assertion site
            // instead of passing vacuously against a connector that returns nothing.
            var scaffoldItem = new WorkItemBase
            {
                ReferenceId = "INC0000005",
                Name = "__scaffold__",
                Type = "__scaffold__",
                State = "__scaffold__",
                Order = "__scaffold__",
                SyncedTransitions = [new WorkItemStateTransition { FromState = "__scaffold__", ToState = "__scaffold__", TransitionedAt = DateTime.UnixEpoch }],
            };

            return Task.FromResult<IEnumerable<WorkItem>>([new WorkItem(scaffoldItem, team)]);
        }

        public Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            throw new NotSupportedException(WorkItemReadingUnavailableMessage);
        }

        public Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            throw new NotSupportedException(WorkItemReadingUnavailableMessage);
        }

        public Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            // SCAFFOLD (DISTILL slice 02, Story #5575). Slice 02 supersedes slice 01's blanket
            // refusal: a team can now say which ServiceNow work is theirs, so the answer becomes a
            // verdict about their query rather than a "not built yet". The scaffold routes through
            // the (also scaffolded) verdict, so every rung fails at its assertion site.
            _ = team;
            return Task.FromResult(ServiceNowTeamQueryVerdict.FromMissingQuery());
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

        private async Task<(HttpStatusCode StatusCode, string Body)> Probe(Uri probeUri, WorkTrackingSystemConnection connection)
        {
            var authStrategy = authStrategyFactory.Resolve(ResolveAuthenticationMethodKey(connection));

            using var client = CreateHttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, probeUri);
            await authStrategy.ApplyAsync(request, connection, CancellationToken.None);

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            return (response.StatusCode, body);
        }

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
            probeUri = null;

            if (!Uri.TryCreate(instanceUrl, UriKind.Absolute, out var instanceUri))
            {
                return false;
            }

            if (instanceUri.Scheme != Uri.UriSchemeHttp && instanceUri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            // No sysparm_fields: field projection was never measured against ACL row filtering
            // (SPIKE Q8), and this probe exists to distrust exactly that substrate.
            return Uri.TryCreate(
                $"{instanceUrl.TrimEnd('/')}/api/now/table/{Uri.EscapeDataString(table)}?sysparm_limit=1",
                UriKind.Absolute,
                out probeUri);
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
