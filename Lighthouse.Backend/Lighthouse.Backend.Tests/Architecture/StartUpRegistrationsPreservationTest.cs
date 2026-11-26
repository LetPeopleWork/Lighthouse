using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The start-up registrations move out of Program into one start-up method per module, a few lines at a time.
    /// A moved line that is dropped, doubled, put in another order beside a type registered more than once, or given
    /// another lifetime changes what the application starts with, and most of those changes break no other test.
    /// This test reads what the start-up code registers into a service collection, without starting a host, and
    /// compares it with the registrations written out below, captured before the first line moved. The list is
    /// written out by hand on purpose: a list computed from the registrations would follow any change and could
    /// never fail. It is temporary and goes once every module starts itself up.
    /// </summary>
    [TestFixture]
    public partial class StartUpRegistrationsPreservationTest
    {
        private const string Separator = " :: ";

        private static readonly string[] RegistrationsBeforeTheModulesTookThemOver =
        [
            "Lighthouse.Backend.API.FeatureListRows :: Scoped Lighthouse.Backend.API.FeatureListRows",
            "Lighthouse.Backend.API.Helpers.OwnerSettingRefusals :: Scoped Lighthouse.Backend.API.Helpers.OwnerSettingRefusals",
            "Lighthouse.Backend.Cache.Cache<System.String, System.Object> :: Singleton Lighthouse.Backend.Cache.Cache<System.String, System.Object>",
            "Lighthouse.Backend.Factories.IDemoDataFactory :: Scoped Lighthouse.Backend.Factories.DemoDataFactory",
            "Lighthouse.Backend.Models.Forecast.ForecastSimulationLimits :: Singleton instance of Lighthouse.Backend.Models.Forecast.ForecastSimulationLimits",
            "Lighthouse.Backend.Modules.Access.Application.IApiKeyService :: Scoped Lighthouse.Backend.Modules.Access.Application.ApiKeyService",
            "Lighthouse.Backend.Modules.Access.Application.IAuthConfigurationValidator :: Singleton Lighthouse.Backend.Modules.Access.Application.AuthConfigurationValidator",
            "Lighthouse.Backend.Modules.Access.Application.IAuthModeResolver :: Scoped Lighthouse.Backend.Modules.Access.Application.AuthModeResolver",
            "Lighthouse.Backend.Modules.Access.Application.ICurrentUserProfileService :: Scoped Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService",
            "Lighthouse.Backend.Modules.Access.Application.IEmbedSessionTokenService :: Scoped Lighthouse.Backend.Modules.Access.Application.EmbedSessionTokenService",
            "Lighthouse.Backend.Modules.Access.Application.IFeatureMoveAuthorization :: Scoped Lighthouse.Backend.Modules.Access.Application.FeatureMoveAuthorization",
            "Lighthouse.Backend.Modules.Access.Application.IOidcGroupSnapshotWriter :: Scoped Lighthouse.Backend.Modules.Access.Application.OidcGroupSnapshotWriter",
            "Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService :: Scoped Lighthouse.Backend.Modules.Access.Application.RbacAdministrationService",
            "Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller :: Singleton Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IBlockedCountSnapshotRepository :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence.BlockedCountSnapshotRepository",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.ICycleTimeOutlierDefinition :: Singleton instance of Lighthouse.Backend.Modules.FlowMetrics.Application.UpperNaturalProcessLimitOutlierDefinition",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IForecastDataSufficiency :: Scoped factory returning Lighthouse.Backend.Modules.FlowMetrics.Application.IForecastDataSufficiency asking [Lighthouse.Backend.Modules.Forecasting.Application.ForecastingAnswersToFlowMetrics]",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IHowManyForecaster :: Scoped factory returning Lighthouse.Backend.Modules.FlowMetrics.Application.IHowManyForecaster asking [Lighthouse.Backend.Modules.Forecasting.Application.ForecastingAnswersToFlowMetrics]",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IOverTimeGapReconciler :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.OverTimeGapReconciler",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IOverTimeHistoryFillActivity :: Singleton factory returning Lighthouse.Backend.Modules.FlowMetrics.Application.IOverTimeHistoryFillActivity asking [Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller]",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IOverTimeHistoryFiller :: Singleton factory returning Lighthouse.Backend.Modules.FlowMetrics.Application.IOverTimeHistoryFiller asking [Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller]",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IPercentileSnapshotWriter :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.PercentileSnapshotWriter",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IPercentilesOverTimeSeriesQuery :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.GapAskingPercentilesOverTimeSeriesQuery",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IPercentilesOverTimeSnapshotRepository :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence.PercentilesOverTimeSnapshotRepository",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IPortfolioMetricsService :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.PortfolioMetricsService",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IProcessBehaviorSeriesQuery :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.GapAskingProcessBehaviorSeriesQuery",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IProcessBehaviorSnapshotRepository :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence.ProcessBehaviorSnapshotRepository",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IProcessBehaviorSnapshotWriter :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.ProcessBehaviorSnapshotWriter",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.ITeamMetricsService :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.TeamMetricsService",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.IThroughputFilter :: Scoped factory returning Lighthouse.Backend.Modules.FlowMetrics.Application.IThroughputFilter asking [Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService]",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.PercentilesOverTimeSeriesQuery :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.PercentilesOverTimeSeriesQuery",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.ProcessBehaviorSeriesQuery :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.ProcessBehaviorSeriesQuery",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.ReconstructionMemo :: Singleton Lighthouse.Backend.Modules.FlowMetrics.Application.ReconstructionMemo",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastLane :: Singleton factory returning Lighthouse.Backend.Modules.Forecasting.Application.ForecastLane asking [Lighthouse.Backend.Models.Forecast.ForecastSimulationLimits]",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastingAnswersToFlowMetrics :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.ForecastingAnswersToFlowMetrics",
            "Lighthouse.Backend.Modules.Forecasting.Application.IForecastFilterRuleService :: Scoped factory returning Lighthouse.Backend.Modules.Forecasting.Application.IForecastFilterRuleService asking [Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService]",
            "Lighthouse.Backend.Modules.Forecasting.Application.IForecastRealityCheckService :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.ForecastRealityCheckService",
            "Lighthouse.Backend.Modules.Forecasting.Application.IForecastService :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.ForecastService",
            "Lighthouse.Backend.Modules.Forecasting.Application.IForecastUpdater :: Singleton Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater",
            "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackCollector :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackCollector",
            "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackService :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackService",
            "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackTriggerService :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackTriggerService",
            "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRoundContext :: Singleton Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRoundContext",
            "Lighthouse.Backend.Modules.Forecasting.Domain.ForecastSimulation :: Singleton Lighthouse.Backend.Modules.Forecasting.Domain.ForecastSimulation",
            "Lighthouse.Backend.Modules.Forecasting.Domain.IDrawStreamFactory :: Singleton Lighthouse.Backend.Modules.Forecasting.Domain.DrawStreamFactory",
            "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService :: Scoped Lighthouse.Backend.Modules.Licensing.Application.LicenseService",
            "Lighthouse.Backend.Modules.Licensing.Application.ILicenseVerifier :: Scoped Lighthouse.Backend.Modules.Licensing.Application.LicenseVerifier",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.IConnectionSaveService :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.IWorkTrackingSystemConnectionUsage :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.WorkTrackingSystemConnectionUsage",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.AppSettingService",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.IBlackoutPeriodService :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.BlackoutPeriodService",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.IFeatureOrdering :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureOrdering",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.IFeatureOrderingPolicyProvider :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureOrderingPolicyProvider",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.IFeatureRankSeeder :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankSeeder",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.IFeatureRankingService :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.IRecurringBlackoutRuleService :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.RecurringBlackoutRuleService",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.ITerminologyService :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.TerminologyService",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.DefaultOptionalFeatureApplier :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.DefaultOptionalFeatureApplier",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.IOptionalFeatureApplier :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.FeatureOrderingApplier",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.OptionalFeatureApplierRegistry :: Scoped Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.OptionalFeatureApplierRegistry",
            "Lighthouse.Backend.Modules.Promises.Application.DeliveryMetricValuesProjector :: Scoped Lighthouse.Backend.Modules.Promises.Application.DeliveryMetricValuesProjector",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer :: Scoped Lighthouse.Backend.Modules.Promises.Application.DeliveryForecastBlockRenderer",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastPublishingService :: Scoped Lighthouse.Backend.Modules.Promises.Application.DeliveryForecastPublishingService",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliveryRuleService :: Scoped Lighthouse.Backend.Modules.Promises.Application.DeliveryRuleService",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliverySourceResolver :: Scoped Lighthouse.Backend.Modules.Promises.Application.DeliverySourceResolver",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliverySourceSyncService :: Scoped Lighthouse.Backend.Modules.Promises.Application.DeliverySourceSyncService",
            "Lighthouse.Backend.Modules.Refinement.Application.IRefinementCalendar :: Scoped Lighthouse.Backend.Modules.Refinement.Application.RefinementCalendar",
            "Lighthouse.Backend.Modules.Refinement.Application.IRefinementViewQuery :: Scoped Lighthouse.Backend.Modules.Refinement.Application.RefinementViewQuery",
            "Lighthouse.Backend.Modules.Refinement.Application.ISizingLogCommands :: Scoped Lighthouse.Backend.Modules.Refinement.Application.SizingLogCommands",
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementList :: Scoped Lighthouse.Backend.Modules.Refinement.Application.RefinementList",
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementNeedCalculator :: Scoped Lighthouse.Backend.Modules.Refinement.Application.RefinementNeedCalculator",
            "Lighthouse.Backend.Modules.Refinement.Application.SleYardstickResolver :: Scoped Lighthouse.Backend.Modules.Refinement.Application.SleYardstickResolver",
            "Lighthouse.Backend.Modules.Refinement.Application.StageRuleMatcher :: Scoped Lighthouse.Backend.Modules.Refinement.Application.StageRuleMatcher",
            "Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver :: Scoped Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver",
            "Lighthouse.Backend.Modules.Reports.Application.IReportBulkCommands :: Scoped Lighthouse.Backend.Modules.Reports.Application.ReportBulkCommands",
            "Lighthouse.Backend.Modules.Reports.Application.IReportBulkRemover :: Scoped Lighthouse.Backend.Modules.Reports.Application.ReportBulkRemover",
            "Lighthouse.Backend.Modules.Reports.Application.IReportCommands :: Scoped Lighthouse.Backend.Modules.Reports.Application.ReportCommands",
            "Lighthouse.Backend.Modules.Reports.Application.IReportMetric :: Scoped Lighthouse.Backend.Modules.Reports.Application.Metrics.CycleTimeReportMetric",
            "Lighthouse.Backend.Modules.Reports.Application.IReportMetric :: Scoped Lighthouse.Backend.Modules.Reports.Application.Metrics.ThroughputReportMetric",
            "Lighthouse.Backend.Modules.Reports.Application.IReportMetric :: Scoped Lighthouse.Backend.Modules.Reports.Application.Metrics.WipReportMetric",
            "Lighthouse.Backend.Modules.Reports.Application.IReportMetric :: Scoped Lighthouse.Backend.Modules.Reports.Application.Metrics.WorkItemAgeReportMetric",
            "Lighthouse.Backend.Modules.Reports.Application.IReportMetric :: Scoped Lighthouse.Backend.Modules.Reports.Application.Metrics.ArrivalsReportMetric",
            "Lighthouse.Backend.Modules.Reports.Application.IReportMetricSeries :: Scoped Lighthouse.Backend.Modules.Reports.Application.ReportMetricSeriesByOwner",
            "Lighthouse.Backend.Modules.Reports.Application.IReportOwnerLookup :: Scoped Lighthouse.Backend.Modules.Reports.Application.ReportOwnerLookup",
            "Lighthouse.Backend.Modules.Reports.Application.IReportQueries :: Scoped Lighthouse.Backend.Modules.Reports.Application.ReportQueries",
            "Lighthouse.Backend.Modules.Reports.Application.IReportReader :: Scoped factory returning Lighthouse.Backend.Modules.Reports.Application.IReportReader asking [Lighthouse.Backend.Services.Implementation.Repositories.ReportRepository]",
            "Lighthouse.Backend.Modules.Reports.Application.IReportTemplate :: Scoped Lighthouse.Backend.Modules.Reports.Application.ThenAndNowTemplate",
            "Lighthouse.Backend.Modules.Reports.Application.IReportWriter :: Scoped factory returning Lighthouse.Backend.Modules.Reports.Application.IReportWriter asking [Lighthouse.Backend.Services.Implementation.Repositories.ReportRepository]",
            "Lighthouse.Backend.Modules.Reports.Application.Metrics.ReportMetricCatalog :: Scoped Lighthouse.Backend.Modules.Reports.Application.Metrics.ReportMetricCatalog",
            "Lighthouse.Backend.Modules.Reports.Application.PortfolioReportMetricSeries :: Scoped Lighthouse.Backend.Modules.Reports.Application.PortfolioReportMetricSeries",
            "Lighthouse.Backend.Modules.Reports.Application.TeamReportMetricSeries :: Scoped Lighthouse.Backend.Modules.Reports.Application.TeamReportMetricSeries",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Background.ConnectionHealthProber :: Singleton Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Background.ConnectionHealthProber",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.JiraCloudBasicAuthStrategy :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.JiraCloudBasicAuthStrategy",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.LinearApiKeyAuthStrategy :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.LinearApiKeyAuthStrategy",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.NoOpAuthStrategy :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.NoOpAuthStrategy",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.OAuthBearerAuthStrategy :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.OAuthBearerAuthStrategy",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.PatAuthStrategy :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.PatAuthStrategy",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.ServiceNowBasicAuthStrategy :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.ServiceNowBasicAuthStrategy",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv.CsvWorkTrackingConnector :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv.CsvWorkTrackingConnector",
            "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira.IIssueFactory :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira.IssueFactory",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.IConnectionHealthService :: Scoped factory returning Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.IConnectionHealthService asking [Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService]",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IAzureDevOpsWorkTrackingConnector :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.AzureDevOps.AzureDevOpsWorkTrackingConnector",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IDescriptionBlockMerger :: Scoped Lighthouse.Backend.Modules.Promises.Application.DeliveryForecastBlockRenderer",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IJiraWorkTrackingConnector :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira.JiraWorkTrackingConnector",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ILinearWorkTrackingConnector :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Linear.LinearWorkTrackingConnector",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IServiceNowWorkTrackingConnector :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.ServiceNow.ServiceNowWorkTrackingConnector",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingAuthStrategyFactory :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth.WorkTrackingAuthStrategyFactory",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Application.WorkTrackingConnectorFactory",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingSystemFactory :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Application.WorkTrackingSystemFactory",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.IOAuthProvider :: Singleton Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth.JiraOAuthProvider",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.IOAuthProvider :: Singleton Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth.AdoOAuthProvider",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.IOAuthProviderRegistry :: Singleton Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.OAuthProviderRegistry",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.IOAuthService :: Scoped Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.OAuthService",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.IOAuthStateTokenIssuer :: Singleton Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.OAuthStateTokenIssuer",
            "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering",
            "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.IDependencyDecision :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyDecision",
            "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.IDependencyReconciler :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyReconciler",
            "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.IDependencyRefreshReporter :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyRefreshReporter",
            "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.IWhatTheForecastWaitsFor :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.WhatTheForecastWaitsFor",
            "Lighthouse.Backend.Modules.WorkItems.Application.IBlockedItemService :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.BlockedItemService",
            "Lighthouse.Backend.Modules.WorkItems.Application.IRefreshLogService :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.RefreshLogService",
            "Lighthouse.Backend.Modules.WorkItems.Application.IRuleEvaluator<Lighthouse.Backend.Models.WorkItem> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.RuleEvaluator<Lighthouse.Backend.Models.WorkItem>",
            "Lighthouse.Backend.Modules.WorkItems.Application.IRuleFieldProvider<Lighthouse.Backend.Models.WorkItem> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.WorkItemFieldProvider",
            "Lighthouse.Backend.Modules.WorkItems.Application.ITeamDataService :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.TeamDataService",
            "Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService",
            "Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies.IDependencyHonourPolicy :: Scoped Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies.DependencyHonourPolicy",
            "Lighthouse.Backend.Platform.DatabaseManagement.DatabaseMaintenanceGate :: Singleton Lighthouse.Backend.Platform.DatabaseManagement.DatabaseMaintenanceGate",
            "Lighthouse.Backend.Platform.DatabaseManagement.DatabaseOperationTracker :: Singleton Lighthouse.Backend.Platform.DatabaseManagement.DatabaseOperationTracker",
            "Lighthouse.Backend.Platform.DatabaseManagement.ICommandRunner :: Singleton Lighthouse.Backend.Platform.DatabaseManagement.CommandRunner",
            "Lighthouse.Backend.Platform.DatabaseManagement.IDatabaseManagementProvider :: Singleton factory returning Lighthouse.Backend.Platform.DatabaseManagement.IDatabaseManagementProvider asking [Microsoft.Extensions.Options.IOptions<Lighthouse.Backend.Data.DatabaseConfiguration>]",
            "Lighthouse.Backend.Platform.DatabaseManagement.IDatabaseManagementService :: Singleton Lighthouse.Backend.Platform.DatabaseManagement.DatabaseManagementService",
            "Lighthouse.Backend.Platform.DemoData.IDemoDataService :: Scoped Lighthouse.Backend.Platform.DemoData.DemoDataService",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventDispatcher :: Singleton Lighthouse.Backend.Platform.DomainEvents.DomainEventDispatcher",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.BlackoutConfigurationChanged> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.BlackoutConfigurationChangedMetricsInvalidationHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.ConnectionFetchShapeChanged> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Policies.ConnectionFetchShapeChangedUpdateTriggerHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.FeatureBlocked> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Policies.FeatureBlockedTransitionCaptureHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.FeatureOrderingPolicyChanged> :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.Policies.FeatureOrderingPolicyChangedForecastTriggerHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.FeatureRankChanged> :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.Policies.FeatureRankChangedForecastTriggerHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.FeatureUnblocked> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Policies.FeatureBlockedTransitionCloseHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioDeleted> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Policies.PortfolioDeletedOrphanedFeatureCleanupHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> :: Scoped factory returning Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> asking [Lighthouse.Backend.Modules.FlowMetrics.Application.ReconstructionMemo]",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.PortfolioFeaturesRefreshedMetricsInvalidationHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.BlockedCountSnapshotRecordingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.PercentilesOverTimeRecordingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.ProcessBehaviorRecordingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.DemoBlockedHistoryBackfillHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioFeaturesRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.DemoPercentilesBackfillHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioForecastsUpdated> :: Scoped Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.PortfolioForecastsUpdated> :: Scoped Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryForecastPublishingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.TeamDataRefreshedTeamMetricsUpdateHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped factory returning Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> asking [Lighthouse.Backend.Modules.FlowMetrics.Application.ReconstructionMemo]",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.Policies.TeamDataRefreshedForecastTriggerHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.BlockedCountSnapshotRecordingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.PercentilesOverTimeRecordingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.ProcessBehaviorRecordingHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.DemoBlockedHistoryBackfillHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDataRefreshed> :: Scoped Lighthouse.Backend.Modules.FlowMetrics.Application.Policies.DemoPercentilesBackfillHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDeleted> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Policies.TeamDeletedRefreshLogCleanupHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.TeamDeleted> :: Scoped Lighthouse.Backend.Modules.Forecasting.Application.Policies.TeamDeletedForecastRetriggerHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.WorkItemBlocked> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Policies.WorkItemBlockedTransitionCaptureHandler",
            "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<Lighthouse.Backend.Models.Events.WorkItemUnblocked> :: Scoped Lighthouse.Backend.Modules.WorkItems.Application.Policies.WorkItemBlockedTransitionCloseHandler",
            "Lighthouse.Backend.Platform.Encryption.ICryptoService :: Singleton Lighthouse.Backend.Platform.Encryption.CryptoService",
            "Lighthouse.Backend.Platform.Host.IFileSystemService :: Scoped Lighthouse.Backend.Platform.Host.FileSystemService",
            "Lighthouse.Backend.Platform.Host.ILighthouseClock :: Singleton factory returning Lighthouse.Backend.Platform.Host.ILighthouseClock asking [System.TimeProvider]",
            "Lighthouse.Backend.Platform.Host.IPlatformService :: Singleton Lighthouse.Backend.Platform.Host.PlatformService",
            "Lighthouse.Backend.Platform.Host.IProcessService :: Singleton Lighthouse.Backend.Platform.Host.ProcessService",
            "Lighthouse.Backend.Platform.Host.IRandomNumberService :: Singleton Lighthouse.Backend.Platform.Host.RandomNumberService",
            "Lighthouse.Backend.Platform.Host.IServiceConfig :: Singleton instance of Lighthouse.Backend.Platform.Host.ServiceConfig",
            "Lighthouse.Backend.Platform.Host.ISystemInfoService :: Singleton Lighthouse.Backend.Platform.Host.SystemInfoService",
            "Lighthouse.Backend.Platform.Logging.IConfigFileUpdater :: Scoped Lighthouse.Backend.Platform.Logging.ConfigFileUpdater",
            "Lighthouse.Backend.Platform.Logging.IRecentProblemsReport :: Scoped Lighthouse.Backend.Platform.Logging.RecentProblemsReport",
            "Lighthouse.Backend.Platform.Releases.IAssemblyService :: Scoped Lighthouse.Backend.Platform.Releases.AssemblyService",
            "Lighthouse.Backend.Platform.Releases.IGitHubService :: Singleton Lighthouse.Backend.Platform.Releases.GitHubService",
            "Lighthouse.Backend.Platform.Releases.ILighthouseReleaseService :: Scoped Lighthouse.Backend.Platform.Releases.LighthouseReleaseService",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Services.Implementation.Seeding.AppSettingSeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Platform.Seeding.InstallTimestampSeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Services.Implementation.Seeding.OptionalFeatureSeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Services.Implementation.Seeding.TerminologySeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Services.Implementation.Seeding.RefreshLogSeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Services.Implementation.Seeding.ApiKeyOwnerReconciliationSeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastInputsForgottenAtStartSeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Services.Implementation.Seeding.CategoryListNormalisationSeeder",
            "Lighthouse.Backend.Platform.Seeding.ISeeder :: Scoped Lighthouse.Backend.Services.Implementation.Seeding.EveryoneSignedInSeeder",
            "Lighthouse.Backend.Platform.UpdateQueue.IUpdateCancellationNotifier :: Singleton Lighthouse.Backend.Platform.UpdateQueue.InProcessUpdateCancellationNotifier",
            "Lighthouse.Backend.Platform.UpdateQueue.IUpdateCompletionNotifier :: Singleton Lighthouse.Backend.Platform.UpdateQueue.InProcessUpdateCompletionNotifier",
            "Lighthouse.Backend.Platform.UpdateQueue.IUpdateExecutionLock :: Singleton Lighthouse.Backend.Platform.UpdateQueue.InProcessUpdateExecutionLock",
            "Lighthouse.Backend.Platform.UpdateQueue.IUpdateQueueService :: Singleton Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService",
            "Lighthouse.Backend.Platform.UpdateQueue.IUpdateStatusStore :: Singleton Lighthouse.Backend.Platform.UpdateQueue.InProcessUpdateStatusStore",
            "Lighthouse.Backend.Platform.UpdateQueue.IUpdateTaskNaming :: Scoped Lighthouse.Backend.Platform.UpdateQueue.UpdateTaskNaming",
            "Lighthouse.Backend.Platform.UpdateQueue.UpdateCancellationContext :: Singleton Lighthouse.Backend.Platform.UpdateQueue.UpdateCancellationContext",
            "Lighthouse.Backend.Platform.UpdateQueue.UpdateSubstrate :: Singleton Lighthouse.Backend.Platform.UpdateQueue.UpdateSubstrate",
            "Lighthouse.Backend.Platform.UsageData.IUsageDataConsentService :: Scoped Lighthouse.Backend.Platform.UsageData.UsageDataConsentService",
            "Lighthouse.Backend.Platform.UsageData.IUsageDataDeploymentModeResolver :: Singleton Lighthouse.Backend.Platform.UsageData.UsageDataDeploymentModeResolver",
            "Lighthouse.Backend.Platform.UsageData.IUsageDataEventQueue :: Singleton Lighthouse.Backend.Platform.UsageData.UsageDataEventQueue",
            "Lighthouse.Backend.Platform.UsageData.IUsageDataGate :: Singleton Lighthouse.Backend.Platform.UsageData.UsageDataGate",
            "Lighthouse.Backend.Platform.UsageData.IUsageDataInstanceProperties :: Singleton Lighthouse.Backend.Platform.UsageData.UsageDataInstanceProperties",
            "Lighthouse.Backend.Platform.UsageData.IUsageDataMasterSwitch :: Scoped Lighthouse.Backend.Platform.UsageData.UsageDataMasterSwitch",
            "Lighthouse.Backend.Platform.UsageData.IUsageDataPublisher :: Singleton Lighthouse.Backend.Platform.UsageData.PostHogUsageDataPublisher",
            "Lighthouse.Backend.Platform.UsageData.UsageDataConsentPruningService :: Singleton Lighthouse.Backend.Platform.UsageData.UsageDataConsentPruningService",
            "Lighthouse.Backend.Platform.UsageData.UsageDataForwardingService :: Singleton Lighthouse.Backend.Platform.UsageData.UsageDataForwardingService",
            "Lighthouse.Backend.Services.Implementation.FeatureListActiveWork :: Scoped Lighthouse.Backend.Services.Implementation.FeatureListActiveWork",
            "Lighthouse.Backend.Services.Implementation.FeatureListOwners :: Scoped Lighthouse.Backend.Services.Implementation.FeatureListOwners",
            "Lighthouse.Backend.Services.Implementation.Repositories.ConnectionHealthVerdictRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.ConnectionHealthVerdictRepository",
            "Lighthouse.Backend.Services.Implementation.Repositories.ReportRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.ReportRepository",
            "Lighthouse.Backend.Services.Interfaces.IFeatureListService :: Scoped Lighthouse.Backend.Services.Implementation.FeatureListService",
            "Lighthouse.Backend.Services.Interfaces.IFeaturePositionMap :: Scoped Lighthouse.Backend.Services.Implementation.FeaturePositionMap",
            "Lighthouse.Backend.Services.Interfaces.IOrphanedFeatureCleanupService :: Singleton Lighthouse.Backend.Services.Implementation.OrphanedFeatureCleanupService",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IApiKeyRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.ApiKeyRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IDeliveryMetricSnapshotRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.DeliveryMetricSnapshotRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IDeliveryRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.DeliveryRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IEmbedSessionTokenRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.EmbedSessionTokenRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureBlockedTransitionRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.FeatureBlockedTransitionRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureListReader :: Scoped factory returning Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureListReader asking [Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureRepository]",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.FeatureRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureStateTransitionRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.FeatureStateTransitionRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IPortfolioRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.PortfolioRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.AppSetting> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.AppSettingRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.Auth.UserProfile> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.UserProfileRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.Authorization.ApiKeyPermission> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.ApiKeyPermissionRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.BlackoutPeriod> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.BlackoutPeriodRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.ConnectionHealth.ConnectionHealthVerdict> :: Scoped factory returning Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.ConnectionHealth.ConnectionHealthVerdict> asking [Lighthouse.Backend.Services.Implementation.Repositories.ConnectionHealthVerdictRepository]",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.Feature> :: Scoped factory returning Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.Feature> asking [Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureRepository]",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.LicenseInformation> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.LicenseInformationRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.OAuth.OAuthCredential> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.OAuthCredentialRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.OptionalFeatures.OptionalFeature> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.OptionalFeatureRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.Portfolio> :: Scoped factory returning Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.Portfolio> asking [Lighthouse.Backend.Services.Interfaces.Repositories.IPortfolioRepository]",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.RecurringBlackoutRule> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.RecurringBlackoutRuleRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.RefreshLog> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.RefreshLogRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.Team> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.TeamRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.TerminologyEntry> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.TerminologyRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.WorkTrackingSystemConnection> :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.WorkTrackingSystemConnectionRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.ISizingLogRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.SizingLogRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IUsageDataConsentRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.UsageDataConsentRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IUserProfileLookup :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.UserProfileLookup",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IWorkItemBlockedTransitionRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.WorkItemBlockedTransitionRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IWorkItemRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.WorkItemRepository",
            "Lighthouse.Backend.Services.Interfaces.Repositories.IWorkItemStateTransitionRepository :: Scoped Lighthouse.Backend.Services.Implementation.Repositories.WorkItemStateTransitionRepository",
            "Lighthouse.Backend.Services.Interfaces.Update.IPortfolioUpdater :: Singleton Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater",
            "Lighthouse.Backend.Services.Interfaces.Update.ITeamUpdater :: Singleton Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.TeamUpdater",
            "Microsoft.AspNetCore.Http.IHttpContextAccessor :: Singleton Microsoft.AspNetCore.Http.HttpContextAccessor",
            "Microsoft.Extensions.DependencyInjection.DefaultHttpClientConfigurationTracker :: Singleton instance of Microsoft.Extensions.DependencyInjection.DefaultHttpClientConfigurationTracker",
            "Microsoft.Extensions.DependencyInjection.HttpClientMappingRegistry :: Singleton instance of Microsoft.Extensions.DependencyInjection.HttpClientMappingRegistry",
            "Microsoft.Extensions.Hosting.IHostedService :: Singleton factory returning Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller asking [Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller]",
            "Microsoft.Extensions.Hosting.IHostedService :: Singleton Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.TeamUpdater",
            "Microsoft.Extensions.Hosting.IHostedService :: Singleton Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater",
            "Microsoft.Extensions.Hosting.IHostedService :: Singleton factory returning Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Background.ConnectionHealthProber asking [Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Background.ConnectionHealthProber]",
            "Microsoft.Extensions.Hosting.IHostedService :: Singleton factory returning Lighthouse.Backend.Platform.UsageData.UsageDataForwardingService asking [Lighthouse.Backend.Platform.UsageData.UsageDataForwardingService]",
            "Microsoft.Extensions.Hosting.IHostedService :: Singleton factory returning Lighthouse.Backend.Platform.UsageData.UsageDataConsentPruningService asking [Lighthouse.Backend.Platform.UsageData.UsageDataConsentPruningService]",
            "Microsoft.Extensions.Http.DefaultHttpClientFactory :: Singleton Microsoft.Extensions.Http.DefaultHttpClientFactory",
            "Microsoft.Extensions.Http.DefaultTypedHttpClientFactory+Cache<TClient> :: Singleton Microsoft.Extensions.Http.DefaultTypedHttpClientFactory+Cache<TClient>",
            "Microsoft.Extensions.Http.HttpMessageHandlerBuilder :: Transient Microsoft.Extensions.Http.DefaultHttpMessageHandlerBuilder",
            "Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter :: Singleton Microsoft.Extensions.Http.LoggingHttpMessageHandlerBuilderFilter",
            "Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter :: Singleton Microsoft.Extensions.Http.MetricsFactoryHttpMessageHandlerFilter",
            "Microsoft.Extensions.Http.ITypedHttpClientFactory<TClient> :: Transient Microsoft.Extensions.Http.DefaultTypedHttpClientFactory<TClient>",
            "Microsoft.Extensions.Options.IConfigureOptions<Lighthouse.Backend.Configuration.UsageDataConfiguration> :: Singleton instance of Microsoft.Extensions.Options.NamedConfigureFromConfigurationOptions<Lighthouse.Backend.Configuration.UsageDataConfiguration>",
            "Microsoft.Extensions.Options.IConfigureOptions<Lighthouse.Backend.Models.Auth.AuthenticationConfiguration> :: Singleton instance of Microsoft.Extensions.Options.NamedConfigureFromConfigurationOptions<Lighthouse.Backend.Models.Auth.AuthenticationConfiguration>",
            "Microsoft.Extensions.Options.IConfigureOptions<Lighthouse.Backend.Models.Auth.AuthorizationConfiguration> :: Singleton instance of Microsoft.Extensions.Options.NamedConfigureFromConfigurationOptions<Lighthouse.Backend.Models.Auth.AuthorizationConfiguration>",
            "Microsoft.Extensions.Options.IConfigureOptions<Microsoft.Extensions.Http.HttpClientFactoryOptions> :: Singleton instance of Microsoft.Extensions.Options.ConfigureNamedOptions<Microsoft.Extensions.Http.HttpClientFactoryOptions>",
            "Microsoft.Extensions.Options.IConfigureOptions<Microsoft.Extensions.Options.StartupValidatorOptions> :: Transient factory returning Microsoft.Extensions.Options.IConfigureOptions<Microsoft.Extensions.Options.StartupValidatorOptions> asking [Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.DependencyInjection.MetricsServiceExtensions+NoOpOptions>]",
            "Microsoft.Extensions.Options.IOptionsChangeTokenSource<Lighthouse.Backend.Configuration.UsageDataConfiguration> :: Singleton instance of Microsoft.Extensions.Options.ConfigurationChangeTokenSource<Lighthouse.Backend.Configuration.UsageDataConfiguration>",
            "Microsoft.Extensions.Options.IOptionsChangeTokenSource<Lighthouse.Backend.Models.Auth.AuthenticationConfiguration> :: Singleton instance of Microsoft.Extensions.Options.ConfigurationChangeTokenSource<Lighthouse.Backend.Models.Auth.AuthenticationConfiguration>",
            "Microsoft.Extensions.Options.IOptionsChangeTokenSource<Lighthouse.Backend.Models.Auth.AuthorizationConfiguration> :: Singleton instance of Microsoft.Extensions.Options.ConfigurationChangeTokenSource<Lighthouse.Backend.Models.Auth.AuthorizationConfiguration>",
            "Microsoft.Extensions.Options.IValidateOptions<Lighthouse.Backend.Configuration.UsageDataConfiguration> :: Singleton Lighthouse.Backend.Configuration.UsageDataConfigurationValidator",
            "System.Collections.Concurrent.ConcurrentDictionary<Lighthouse.Backend.Platform.UpdateQueue.UpdateKey, Lighthouse.Backend.Platform.UpdateQueue.UpdateStatus> :: Singleton instance of System.Collections.Concurrent.ConcurrentDictionary<Lighthouse.Backend.Platform.UpdateQueue.UpdateKey, Lighthouse.Backend.Platform.UpdateQueue.UpdateStatus>",
            "System.Net.Http.HttpClient :: Transient factory returning System.Net.Http.HttpClient asking [System.Net.Http.IHttpClientFactory]",
            "System.Net.Http.IHttpClientFactory :: Singleton factory returning System.Net.Http.IHttpClientFactory asking [Microsoft.Extensions.Http.DefaultHttpClientFactory]",
            "System.Net.Http.IHttpMessageHandlerFactory :: Singleton factory returning System.Net.Http.IHttpMessageHandlerFactory asking [Microsoft.Extensions.Http.DefaultHttpClientFactory]",
            "System.TimeProvider :: Singleton instance of System.TimeProvider+SystemTimeProvider",
        ];

        [Test]
        public void TheStartUpCode_RegistersEveryServiceWithTheLifetimeKeyAndImplementationsInTheOrderItDidBeforeTheMove()
        {
            var expected = ByServiceType(RegistrationsBeforeTheModulesTookThemOver);
            var actual = ByServiceType(TheStartUpRegistrations());

            var differences = expected.Keys.Union(actual.Keys)
                .Order(StringComparer.Ordinal)
                .Select(serviceType => DifferenceOf(serviceType, expected, actual))
                .OfType<string>()
                .ToList();

            Assert.That(differences, Is.Empty,
                "The start-up code no longer registers the same services as it did before the registrations moved into the modules. " +
                "Every service type below is registered with other lifetimes, keys or implementations, or in another order, than before the move. " +
                "The container hands out the last registration of a type and runs every registration of a type in their order, so such a change changes what the application starts with. " +
                "Move the lines unchanged and call each module's start-up method where its lines stood; if the change is intended, change the list in this test in the same commit and say why." +
                Environment.NewLine + string.Join(Environment.NewLine, differences));
        }

        private static string? DifferenceOf(
            string serviceType,
            Dictionary<string, List<string>> expected,
            Dictionary<string, List<string>> actual)
        {
            var before = expected.GetValueOrDefault(serviceType, []);
            var now = actual.GetValueOrDefault(serviceType, []);

            return before.SequenceEqual(now, StringComparer.Ordinal)
                ? null
                : $"{serviceType}: before the move [{string.Join("; ", before)}], now [{string.Join("; ", now)}]";
        }

        private static Dictionary<string, List<string>> ByServiceType(IEnumerable<string> registrations)
        {
            var byServiceType = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var registration in registrations)
            {
                var at = registration.IndexOf(Separator, StringComparison.Ordinal);
                var serviceType = registration[..at];
                if (!byServiceType.TryGetValue(serviceType, out var ofThatType))
                {
                    ofThatType = [];
                    byServiceType[serviceType] = ofThatType;
                }

                ofThatType.Add(registration[(at + Separator.Length)..]);
            }

            return byServiceType;
        }

        private static List<string> TheStartUpRegistrations()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            var registeredByTheBuilder = builder.Services.Count;

            StartUpMethod("RegisterServices", typeof(WebApplicationBuilder)).Invoke(null, [builder]);

            return [.. builder.Services
                .Skip(registeredByTheBuilder)
                .Select(Described)
                .GroupBy(registration => registration[..registration.IndexOf(Separator, StringComparison.Ordinal)], StringComparer.Ordinal)
                .OrderBy(registrationsOfAType => registrationsOfAType.Key, StringComparer.Ordinal)
                .SelectMany(registrationsOfAType => registrationsOfAType)];
        }

        private static MethodInfo StartUpMethod(string name, Type parameter)
            => typeof(Program).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, [parameter])
               ?? throw new InvalidOperationException(
                   $"The start-up code has no {name}({parameter.Name}) any more, so this test cannot read the start-up registrations. " +
                   "Point it at the methods Main now calls to register the services, in the order Main calls them.");

        private static string Described(ServiceDescriptor registration)
        {
            var key = registration.IsKeyedService ? $" key {KeyOf(registration.ServiceKey)}" : string.Empty;
            return $"{NameOf(registration.ServiceType)}{Separator}{registration.Lifetime}{key} {ImplementationOf(registration)}";
        }

        private static string ImplementationOf(ServiceDescriptor registration)
        {
            if (registration.IsKeyedService)
            {
                return registration.KeyedImplementationType is { } keyedType ? NameOf(keyedType)
                    : registration.KeyedImplementationInstance is { } keyedInstance ? $"instance of {NameOf(keyedInstance.GetType())}"
                    : FactoryDescribed(registration.KeyedImplementationFactory!, provider => registration.KeyedImplementationFactory!(provider, registration.ServiceKey));
            }

            return registration.ImplementationType is { } type ? NameOf(type)
                : registration.ImplementationInstance is { } instance ? $"instance of {NameOf(instance.GetType())}"
                : FactoryDescribed(registration.ImplementationFactory!, provider => registration.ImplementationFactory!(provider));
        }

        private static string FactoryDescribed(Delegate factory, Func<IServiceProvider, object> run)
        {
            var provider = new ProviderThatAnswersNothing();
            provider.Run(run);
            return $"factory returning {NameOf(factory.Method.ReturnType)} asking [{string.Join(", ", provider.Requests)}]";
        }

        private static string KeyOf(object? key)
            => key switch
            {
                null => "null",
                string text => $"\"{text}\"",
                _ => $"{NameOf(key.GetType())}:{key}",
            };

        private static string NameOf(Type type)
        {
            var name = GenericArity().Replace(type.FullName ?? type.Name, string.Empty);
            if (!type.IsGenericType)
            {
                return name;
            }

            var definition = GenericArity().Replace(type.GetGenericTypeDefinition().FullName ?? type.Name, string.Empty);
            return $"{definition}<{string.Join(", ", type.GetGenericArguments().Select(NameOf))}>";
        }

        [GeneratedRegex(@"`\d+")]
        private static partial Regex GenericArity();

        /// <summary>
        /// Runs a factory to learn which services it asks for, without building any of them: the real ones reach the
        /// database and the key ring, which only a started host sets up. It answers every request with nothing, so most
        /// factories stop at their first request; the services asked up to there tell two factories apart, while the
        /// name of the compiler-made method behind a lambda would change whenever the line moves.
        /// </summary>
        private sealed class ProviderThatAnswersNothing : IKeyedServiceProvider
        {
            private readonly List<string> requests = [];

            public IReadOnlyList<string> Requests => requests;

            public void Run(Func<IServiceProvider, object> factory)
            {
                try
                {
                    factory(this);
                }
                catch (InvalidOperationException)
                {
                    // A required service it was refused: what it asked for until then has been recorded.
                }
            }

            public object? GetService(Type serviceType)
            {
                requests.Add(NameOf(serviceType));
                return null;
            }

            public object? GetKeyedService(Type serviceType, object? serviceKey)
            {
                requests.Add($"{NameOf(serviceType)} key {KeyOf(serviceKey)}");
                return null;
            }

            public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
            {
                GetKeyedService(serviceType, serviceKey);
                throw new InvalidOperationException($"No service is registered for {NameOf(serviceType)}.");
            }
        }
    }
}
