namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every string in the test project that names a published type by its old full name or its old file path, taken from a
    /// search of the test project before the move, as old and new text. Each file holds exactly one of the two forms, so the
    /// list holds before, during and after the moves into Contracts and is never edited by a later step.
    /// </summary>
    internal static class ContextContractsStringsTheMoveRenames
    {
        private const string Tests = "Lighthouse.Backend/Lighthouse.Backend.Tests/";

        public static IReadOnlyList<RenamedString> Entries { get; } =
        [
            new(Tests + "Architecture/AccessAndLicensingMembershipArchUnitTest.cs", "Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService", "Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService"),
            new(Tests + "Architecture/ConnectorUnsupportedMembers.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory"),
            new(Tests + "Architecture/ForecastingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService", "Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService"),
            new(Tests + "Architecture/LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService", "Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService"),
            new(Tests + "Architecture/LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService", "Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService"),
            new(Tests + "Architecture/OrganisationSetupIsAnOpenHostArchUnitTest.cs", "Lighthouse.Backend.Models.Events.TeamDeleted", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.TeamDeleted"),
            new(Tests + "Architecture/OrganisationSetupIsAnOpenHostArchUnitTest.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Tests + "Architecture/OrganisationSetupIsAnOpenHostArchUnitTest.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IFeatureOrdering", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IFeatureOrdering"),
            new(Tests + "Architecture/OrganisationSetupMembership.cs", "Models/Events/BlackoutConfigurationChanged", "Modules/OrganisationSetup/Contracts/BlackoutConfigurationChanged"),
            new(Tests + "Architecture/OrganisationSetupMembership.cs", "Models/Events/FeatureOrderingPolicyChanged", "Modules/OrganisationSetup/Contracts/FeatureOrderingPolicyChanged"),
            new(Tests + "Architecture/OrganisationSetupMembership.cs", "Models/Events/FeatureRankChanged", "Modules/OrganisationSetup/Contracts/FeatureRankChanged"),
            new(Tests + "Architecture/OrganisationSetupMembership.cs", "Models/Events/TeamDeleted", "Modules/OrganisationSetup/Contracts/TeamDeleted"),
            new(Tests + "Architecture/OrganisationSetupMembershipArchUnitTest.cs", @"^Lighthouse\.Backend\.(Services\.Interfaces|Modules\.OrganisationSetup\.Application)\.IFeatureOrdering$", @"^Lighthouse\.Backend\.(Services\.Interfaces|Modules\.OrganisationSetup\.Contracts)\.IFeatureOrdering$"),
            new(Tests + "Architecture/OrganisationSetupModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Tests + "Architecture/OrganisationSetupStringsTheMoveRenames.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Tests + "Architecture/OrganisationSetupStringsTheMoveRenames.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IBlackoutPeriodService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IBlackoutPeriodService"),
            new(Tests + "Architecture/OrganisationSetupStringsTheMoveRenames.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IFeatureOrdering", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IFeatureOrdering"),
            new(Tests + "Architecture/PlatformLaneBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Tests + "Architecture/PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService", "Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService"),
            new(Tests + "Architecture/RecurringBlackoutEventsSeamArchUnitTest.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IBlackoutPeriodService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IBlackoutPeriodService"),
            new(Tests + "Architecture/RefinementModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Access.Application.IAuthModeResolver", "Lighthouse.Backend.Modules.Access.Contracts.IAuthModeResolver"),
            new(Tests + "Architecture/RefinementModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory"),
            new(Tests + "Architecture/ReportsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService", "Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService"),
            new(Tests + "Architecture/ReportsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService", "Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService"),
            new(Tests + "Architecture/ReportsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory"),
            new(Tests + "Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService", "Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService"),
            new(Tests + "Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.FlowMetrics.Application.IPortfolioMetricsService", "Lighthouse.Backend.Modules.FlowMetrics.Contracts.IPortfolioMetricsService"),
            new(Tests + "Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Forecasting.Application.IForecastService", "Lighthouse.Backend.Modules.Forecasting.Contracts.IForecastService"),
            new(Tests + "Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService", "Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService"),
            new(Tests + "Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Tests + "Architecture/TrackerIntegrationReachesNoPromisesArchUnitTest.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IDescriptionBlockMerger", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IDescriptionBlockMerger"),
            new(Tests + "Architecture/WorkItemsDoesNotCommandFlowMetricsArchUnitTest.cs", "Lighthouse.Backend.Modules.FlowMetrics.Application.ITeamMetricsService", "Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService"),
            new(Tests + "Architecture/WorkItemsMembershipArchUnitTest.cs", "Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService", "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Tests + "Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService", "Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService"),
            new(Tests + "Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.FlowMetrics.Application.IPortfolioMetricsService", "Lighthouse.Backend.Modules.FlowMetrics.Contracts.IPortfolioMetricsService"),
            new(Tests + "Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.FlowMetrics.Application.ITeamMetricsService", "Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService"),
            new(Tests + "Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService", "Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService"),
            new(Tests + "Architecture/WriteBackMembership.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWriteBackTarget", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWriteBackTarget"),
            new(Tests + "Architecture/WriteBackRoundBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnector", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnector"),
            new(Tests + "Architecture/WriteBackRoundBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory"),
            new(Tests + "Architecture/WriteBackRoundBoundariesArchUnitTest.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWriteBackTarget", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWriteBackTarget"),
            new(Tests + "Architecture/WriteBackRulesNameOnlyForecastingArchUnitTest.cs", "Lighthouse.Backend.Modules.Licensing.Application.ILicenseService", "Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService"),
            new(Tests + "Modules/TrackerIntegration/Application/IncrementalSourceCapabilityTest.cs", "Lighthouse.Backend.Modules.TrackerIntegration.Application.IIncrementalSource", "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IIncrementalSource"),
        ];
    }
}
