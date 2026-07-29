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
    // Slice 01 implements ValidateConnection only; the remaining members are still scaffolds.
    public class ServiceNowWorkTrackingConnector(
        ILogger<ServiceNowWorkTrackingConnector> logger,
        IWorkTrackingAuthStrategyFactory authStrategyFactory,
        HttpMessageHandler? httpMessageHandlerForTesting = null)
        : IServiceNowWorkTrackingConnector
    {
        private readonly ILogger<ServiceNowWorkTrackingConnector> logger = logger;

        private readonly IWorkTrackingAuthStrategyFactory authStrategyFactory = authStrategyFactory;

        private readonly HttpMessageHandler? httpMessageHandlerForTesting = httpMessageHandlerForTesting;

        public bool SupportsTransitionHistory(WorkTrackingSystemConnection connection)
        {
            // Scaffold returns the opposite of the specified false (D6) so the test is RED.
            LogScaffoldUse(nameof(SupportsTransitionHistory));
            return true;
        }

        public IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection)
        {
            // Scaffold returns a placeholder where the specification says empty, so the test is RED.
            LogScaffoldUse(nameof(GetPredefinedAdditionalFields));
            return [new AdditionalFieldDefinition { DisplayName = "__scaffold__" }];
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
            LogScaffoldUse(nameof(GetWorkItemsForTeam));
            return Task.FromResult(Enumerable.Empty<WorkItem>());
        }

        public Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            LogScaffoldUse(nameof(GetFeaturesForProject));
            return Task.FromResult(new List<Feature>());
        }

        public Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            LogScaffoldUse(nameof(GetParentFeaturesDetails));
            return Task.FromResult(new List<Feature>());
        }

        public Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            LogScaffoldUse(nameof(ValidateTeamSettings));
            return Task.FromResult(ConnectionValidationResult.Success());
        }

        public Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio)
        {
            LogScaffoldUse(nameof(ValidatePortfolioSettings));
            return Task.FromResult(ConnectionValidationResult.Success());
        }

        public Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            LogScaffoldUse(nameof(WriteFieldsToWorkItems));
            return Task.FromResult(new WriteBackResult());
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
            return httpMessageHandlerForTesting is null
                ? new HttpClient()
                : new HttpClient(httpMessageHandlerForTesting, disposeHandler: false);
        }

        private ConnectionValidationResult UnreachableInstance(Exception exception, string instanceUrl)
        {
            logger.LogWarning(exception, "Could not reach ServiceNow instance {InstanceUrl}", instanceUrl);

            return ServiceNowValidationVerdict.FromUnreachableInstance(exception.Message);
        }

        private void LogScaffoldUse(string member)
        {
            logger.LogDebug(
                "ServiceNow connector scaffold hit for {Member} (auth strategies: {HasFactory}, test transport: {HasHandler})",
                member,
                authStrategyFactory is not null,
                httpMessageHandlerForTesting is not null);
        }
    }
}
