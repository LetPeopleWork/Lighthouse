namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every rule pattern of the test assembly that sees a moving write-back round type, under its old or its new name, with the
    /// subject its rule matches it against and the role it plays. A pattern is keyed by its class (nested classes joined with
    /// dots) and field name.
    /// </summary>
    internal static class WriteBackPatternRoles
    {
        private const PatternSubject Namespace = PatternSubject.Namespace;

        private const PatternSubject FullName = PatternSubject.FullName;

        private const string WholeOrCore = "takes in the round wherever it lives";

        private const string SeesForecasting = "a part of the code that takes in Forecasting, which the round joins";

        private const string LeftBehind = "an old location of the round, which it leaves";

        public static IReadOnlyDictionary<string, PatternDeclaration> PatternRoles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            // Patterns that take in the whole backend, the application core or every service, and see a round type in either place.
            ["AccessAndLicensingUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["AccessHttpAdaptersLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["AdditionalFieldInUseRulePurityArchUnitTest.AnythingOfLighthouseOutsideModels"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["ApplicationCoreRules.ApplicationCore"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["CalendarDayAnchorSeamArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["CategoryListOwnershipGuardTest.ProductionProject"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["ConnectorsDeclareNoUnsupportedMembersArchUnitTest.AllOfTheBackend"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["DependencySingleDecisionArchUnitTest.BackendProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["FastWithManyFeaturesArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["JiraRouteLiteralsTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["OverTimeReconstructionSeamArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["ProductionSource.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["PromisesLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["PromisesModuleBoundariesArchUnitTest.OutsideTrackerIntegration"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["PromisesUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["RealityCheckReadOnlyArchUnitTest.ImplementationPattern"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["SecretReadPathSourceStructureTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["SecretRotationWriteSurfaceTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["StalenessPolicyPurityArchUnitTest.ServicesPattern"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["TrackerIntegrationUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["UpdateCancellationContextWriterArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            ["ZonedTimestampParsingArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, WholeOrCore),
            // Forecasting and the patterns that take in every module or name Forecasting: they see the round once it moves into Forecasting.
            ["AccessModuleBoundariesArchUnitTest.TheOtherContexts"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["AccessModuleBoundariesArchUnitTest.TheOtherModules"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["FlowMetricsInboundApplicationArchUnitTest.TheLegalCallerNamespaces"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["ForecastingApplicationLayoutArchUnitTest.ForecastingApplicationNamespace"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["ForecastingModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingApplication"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingModule"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["LicensingModuleBoundariesArchUnitTest.AccessAndTheOtherContexts"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["LicensingModuleBoundariesArchUnitTest.TheOtherModules"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["ModuleBoundariesArchUnitTest.ForecastingPattern"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["ModuleRules.ForecastingModule"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["OrganisationSetupMembershipArchUnitTest.TheParentNamespace"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["PlatformLaneMembershipArchUnitTest.ModuleNamespaces"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["SimulatedRunIsolationArchUnitTest.TheForecastComponents"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherContexts"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherModules"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["WorkItemsModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["WorkItemsModuleBoundariesArchUnitTest.WorkItemsLegalCallers"] = PatternDeclaration.Context(Namespace, SeesForecasting),
            ["ModuleRules.TheWriteBackRound"] = PatternDeclaration.Context(FullName, "the write-back round itself, matched against the full name"),
            // Old locations: the loose Services namespaces and the platform, which the round leaves; they must not see it in Forecasting.
            ["KernelRules.TheServices"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["AccessModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["LicensingModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["PromisesModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["PlatformLaneMembershipArchUnitTest.PlatformLaneNamespaces"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["PromisesLayoutArchUnitTest.WhereTheServicesLivedBefore"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["PromisesMembershipArchUnitTest.NamespacesOfTheMembersFoundByName"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["PromisesPoliciesLayoutArchUnitTest.WhereTheHandlersLivedBefore"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["RefinementModuleArchUnitTest.ServicesImplementationPattern"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["TrackerIntegrationConnectorsLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheValueTypesLivedBefore"] = PatternDeclaration.Place(Namespace, LeftBehind),
            ["WorkItemsApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(FullName, "types still in the loose Services namespaces, named by full name; the round leaves them"),
            ["WorkItemsModuleBoundariesArchUnitTest.StrayLooseImplementation"] = PatternDeclaration.Place(FullName, "types still in the loose Services namespaces, named by full name; the round leaves them"),
            ["WorkItemsModuleBoundariesArchUnitTest.StrayLooseInterfaces"] = PatternDeclaration.Place(FullName, "types still in the loose Services namespaces, named by full name; the round leaves them"),
            ["AccessModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("AccessModuleBoundariesArchUnitTest.ThePlatform", "built from that key; the other parts see no round type"),
            ["LicensingModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("LicensingModuleBoundariesArchUnitTest.ThePlatform", "built from that key; the other parts see no round type"),
            ["PromisesModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("PromisesModuleBoundariesArchUnitTest.ThePlatform", "built from that key; the other parts see no round type"),
            ["RefinementModuleBoundariesArchUnitTest.AccessSetUpPlatformAndRepositories"] = PatternDeclaration.Composite("KernelRules.TheServices", "built from that key; the other parts see no round type"),
            ["WorkItemsModuleBoundariesArchUnitTest.StrayLooseServices"] = PatternDeclaration.Composite("WorkItemsModuleBoundariesArchUnitTest.StrayLooseImplementation", "built from that key; the other parts see no round type"),
        };

        /// <summary>
        /// A part of the code that must see the round in Forecasting: the round pattern itself and the Forecasting patterns. Each
        /// must match the new name of every moving type at every step; its old name is left free, so the same check holds while
        /// the round pattern sees both places and after it is narrowed to the new one. Keyed like <see cref="PatternRoles"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, string> SeeTheRoundInItsNewPlace { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessModuleBoundariesArchUnitTest.TheOtherContexts"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["AccessModuleBoundariesArchUnitTest.TheOtherModules"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["FlowMetricsInboundApplicationArchUnitTest.TheLegalCallerNamespaces"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["ForecastingApplicationLayoutArchUnitTest.ForecastingApplicationNamespace"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["ForecastingModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingApplication"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingModule"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["LicensingModuleBoundariesArchUnitTest.AccessAndTheOtherContexts"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["LicensingModuleBoundariesArchUnitTest.TheOtherModules"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["ModuleBoundariesArchUnitTest.ForecastingPattern"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["ModuleRules.ForecastingModule"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["OrganisationSetupMembershipArchUnitTest.TheParentNamespace"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["PlatformLaneMembershipArchUnitTest.ModuleNamespaces"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["SimulatedRunIsolationArchUnitTest.TheForecastComponents"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherContexts"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherModules"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["WorkItemsModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["WorkItemsModuleBoundariesArchUnitTest.WorkItemsLegalCallers"] = "takes in Forecasting, so it sees the round only once it moves there",
            ["ModuleRules.TheWriteBackRound"] = "the one owner of which types are the write-back round, matched against the full name",
        };

        /// <summary>
        /// A part of the code that saw a moving round type only through the Services folder the type lived in. For that pattern the
        /// type is read as an old location: the pattern must not follow it into Forecasting. Keyed by the pattern and the type's name.
        /// </summary>
        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } = new Dictionary<(string Field, string Type), string>();
    }
}
