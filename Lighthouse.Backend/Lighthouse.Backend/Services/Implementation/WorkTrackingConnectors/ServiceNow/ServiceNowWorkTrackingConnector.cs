using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // SCAFFOLD (DISTILL slice 01, Story #5574) — signatures only. Every member deliberately
    // returns the OPPOSITE of the specified behaviour so ServiceNowWorkTrackingConnectorTest
    // fails at its assertions (MISSING_FUNCTIONALITY) rather than passing by accident.
    // DELIVER replaces the bodies.
    //
    // The imperative shell around ServiceNowValidationVerdict: one Table API probe
    // (GET /api/now/table/{table}?sysparm_limit=1), hand (status, contentIsJson, rowCount) to the
    // pure verdict, return what it says. Slice 01 implements ValidateConnection only; the other
    // seven members report an explicit unsupported state — never a silent no-op (DoD 5 / KPI 3).
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

        public Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            // Must NOT be Success(): the happy-path test asserts IsValid plus Code "valid", which is
            // exactly what Success() returns, so the scaffold would pass it vacuously — the same
            // denial-in-a-success-costume shape this slice exists to prevent.
            LogScaffoldUse(nameof(ValidateConnection));
            return Task.FromResult(ConnectionValidationResult.Failure("__scaffold__", "__scaffold__"));
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
