namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every rule pattern of the test assembly that sees a type a context publishes, under its old or its Contracts name, with
    /// the subject its rule matches it against and the role it plays: a front door that must see the type before and after it
    /// moves into its context's Contracts, an old location that must not see it there, or a part of the code that takes in
    /// both. A pattern is keyed by its class (nested classes joined with dots) and field name.
    /// </summary>
    internal static class ContextContractsPatternRoles
    {
        private const PatternSubject Namespace = PatternSubject.Namespace;

        private const string FrontDoorOfItsContext = "the layer other contexts reach this context through; its Contracts joins it";

        private const string WholeContextOrMore = "takes in the whole context the type belongs to, several contexts or the whole backend";

        private const string ALayerItLeaves = "a layer the published type leaves for its context's Contracts";

        private const string ACallingLayer = "the layer of another context that calls this one; a published type there calls nothing";

        private const string TheKernelTheEventsLeave = "the shared kernel, which the events leave for the Contracts of the context that publishes them";

        public static IReadOnlyDictionary<string, PatternDeclaration> PatternRoles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            // Front doors: the layer other code reaches a context through. Contracts joins it, so each must see a published type in both places.
            ["AccessModuleBoundariesArchUnitTest.AccessApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["ConnectorsDeclareNoUnsupportedMembersArchUnitTest.ConnectorPort"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["FlowMetricsInboundApplicationArchUnitTest.FlowMetricsApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["ForecastingModuleBoundariesArchUnitTest.FlowMetricsApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["LicensingModuleBoundariesArchUnitTest.LicensingApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.TrackerIntegrationApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["WorkItemsModuleBoundariesArchUnitTest.FlowMetricsApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            ["WorkItemsModuleBoundariesArchUnitTest.WorkItemsApplication"] = PatternDeclaration.FrontDoor(Namespace, FrontDoorOfItsContext),
            // Parts of the code that take in a whole context, several contexts or the whole backend: they see a published type in either place.
            ["AdditionalFieldInUseRulePurityArchUnitTest.AnythingOfLighthouseOutsideModels"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessAndLicensingMembershipArchUnitTest.AccessNamespaces"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessAndLicensingMembershipArchUnitTest.LicensingNamespaces"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessAndLicensingUnitTestPlacementTest.AModuleMapThatNoLongerSeesTheOldPlace.TheAccessModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessAndLicensingUnitTestPlacementTest.AModuleMapThatNoLongerSeesTheOldPlace.TheLicensingModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessAndLicensingUnitTestPlacementTest.AModuleMapThatStillSeesTheOldPlace.TheAccessModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessAndLicensingUnitTestPlacementTest.AModuleMapThatStillSeesTheOldPlace.TheOldAndTheNewPlace"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessAndLicensingUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessHttpAdaptersLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessModuleBoundariesArchUnitTest.TheOtherContexts"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["AccessModuleBoundariesArchUnitTest.TheOtherModules"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ApplicationCoreRules.ApplicationCore"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["CalendarDayAnchorSeamArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["CategoryListOwnershipGuardTest.ProductionProject"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ConnectorsDeclareNoUnsupportedMembersArchUnitTest.AllOfTheBackend"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["DependencySingleDecisionArchUnitTest.BackendProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["DependencySingleDecisionArchUnitTest.TheConnectors"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["DependencySingleDecisionArchUnitTest.TheNamespaceThisEpicOwns"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["FastWithManyFeaturesArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["FlowMetricsInboundApplicationArchUnitTest.TheLegalCallerNamespaces"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["FlowMetricsInboundDomainAndAdaptersArchUnitTest.BelowTheFlowMetricsStartUp"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ForecastingModuleBoundariesArchUnitTest.BelowTheForecastingStartUp"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ForecastingModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ForecastingModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndTheLicence"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ForecastingModuleBoundariesArchUnitTest.TheLayersBelowTheMadeUpApplicationAndTheLicence"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ForecastingModuleBoundariesArchUnitTest.TheLicence"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["JiraRouteLiteralsTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["LicensingModuleBoundariesArchUnitTest.AccessAndLicensing"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["LicensingModuleBoundariesArchUnitTest.AccessAndTheOtherContexts"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["LicensingModuleBoundariesArchUnitTest.TheOtherModules"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ModuleBoundariesArchUnitTest.ForecastingPattern"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ModuleBoundariesArchUnitTest.TrackerIntegrationPattern"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ModuleBoundariesArchUnitTest.WorkItemsSyncPattern"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ModuleRules.AccessModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ModuleRules.FlowMetricsModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ModuleRules.ForecastingModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ModuleRules.LicensingModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["OrganisationSetupMembershipArchUnitTest.TheModuleNamespace"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["OrganisationSetupMembershipArchUnitTest.TheParentNamespace"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["OrganisationSetupType.NewRootNamespace"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["OverTimeReconstructionSeamArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["PlatformLaneMembershipArchUnitTest.ModuleNamespaces"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ProductionSource.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["PromisesLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["PromisesModuleBoundariesArchUnitTest.AccessAndLicensing"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["PromisesModuleBoundariesArchUnitTest.OutsideTrackerIntegration"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["PromisesModuleBoundariesArchUnitTest.TheNamespacedUpstreamCode"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["PromisesUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["RealityCheckReadOnlyArchUnitTest.ImplementationPattern"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["RefinementModuleBoundariesArchUnitTest.AccessSetUpPlatformAndRepositories"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["RefinementModuleBoundariesArchUnitTest.TheForbiddenCode"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["RefinementModuleBoundariesArchUnitTest.TrackerIntegration"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ReportsModuleBoundariesArchUnitTest.TrackerIntegration"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["SecretReadPathSourceStructureTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["SecretRotationWriteSurfaceTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["SimulatedRunIsolationArchUnitTest.TheForecastComponents"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["StalenessPolicyPurityArchUnitTest.ServicesPattern"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationMembershipArchUnitTest.TrackerIntegrationNamespaces"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherContexts"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherModules"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.TrackerIntegrationLegalCallers"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.TrackerIntegrationModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.WorkItemsModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.ModuleNamespace"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["TrackerIntegrationUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["UpdateCancellationContextWriterArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["WorkItemsDoesNotCommandFlowMetricsArchUnitTest.WorkItemsModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["WorkItemsMembershipArchUnitTest.WorkItemsNamespaces"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["WorkItemsModuleBoundariesArchUnitTest.AccessAndLicensing"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["WorkItemsModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["WorkItemsModuleBoundariesArchUnitTest.WorkItemsLegalCallers"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["WorkItemsModuleBoundariesArchUnitTest.WorkItemsModule"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            ["ZonedTimestampParsingArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeContextOrMore),
            // Layer locations a published type leaves for its context's Contracts: they must not see it there.
            ["AccessApplicationLayoutArchUnitTest.ApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["AccessModuleBoundariesArchUnitTest.AccessApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["FlowMetricsApplicationOutboundArchUnitTest.FlowMetricsApplication"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["FlowMetricsDoesNotReachForecastingArchUnitTest.FlowMetricsApplication"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["FlowMetricsModuleLayoutArchUnitTest.ModuleApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["ForecastingApplicationLayoutArchUnitTest.ForecastingApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["ForecastingDomainLayoutArchUnitTest.ForecastingDomain"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["ForecastingDomainLayoutArchUnitTest.ForecastingDomainNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingDomain"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["LicensingLayoutArchUnitTest.ApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["LicensingModuleBoundariesArchUnitTest.LicensingApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["ModuleRules.FlowMetricsDomain"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["ModuleRules.FlowMetricsDomainNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.ApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.WhereTheBoardPortLives"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["TrackerIntegrationDomainLayoutArchUnitTest.ApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["WorkItemsApplicationLayoutArchUnitTest.ApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["WorkItemsApplicationLayoutArchUnitTest.DependenciesApplicationNamespace"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            ["WorkItemsApplicationLayoutArchUnitTest.WorkItemsApplication"] = PatternDeclaration.Place(Namespace, ALayerItLeaves),
            // A calling layer of another context: a published type there is no caller.
            ["LicensingModuleBoundariesArchUnitTest.AccessApplication"] = PatternDeclaration.Place(Namespace, ACallingLayer),
            // The shared kernel, which the events leave for the Contracts of the context that publishes them.
            ["AccessAndLicensingMembershipArchUnitTest.KernelNamespaces"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["AccessAndLicensingMembershipArchUnitTest.KernelOrBaseLibrary"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["KernelRules.TheModels"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["PlatformLaneMembershipArchUnitTest.KernelNamespaces"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["PromisesMembershipArchUnitTest.KernelNamespaces"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["RefinementMembershipArchUnitTest.KernelOrBaseLibrary"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["TrackerIntegrationMembershipArchUnitTest.KernelNamespaces"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.TheModels"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheValueTypesLivedBefore"] = PatternDeclaration.Place(Namespace, TheKernelTheEventsLeave),
        };

        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } = new Dictionary<(string Field, string Type), string>();
    }
}
