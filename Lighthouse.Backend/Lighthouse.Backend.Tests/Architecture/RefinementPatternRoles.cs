namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every rule pattern of the test assembly that sees a moving Refinement type, under its old or its new name, with the
    /// subject its rule matches it against and the role it plays. A pattern is keyed by its class (nested classes joined
    /// with dots) and field name.
    /// </summary>
    internal static class RefinementPatternRoles
    {
        private const PatternSubject Namespace = PatternSubject.Namespace;

        private const PatternSubject FullName = PatternSubject.FullName;

        private const string TheWholeBackend = "names the whole backend, so it sees every type wherever it lives";

        private const string AProjectFolder = "the name of the backend project folder, read as a path; it names the whole backend under either name";

        private const string BuiltFromTheRefinementPattern = "built from the module map's Refinement pattern; the other parts see no moving type";

        private const string BuiltFromThePlatformKey = "built from the platform key, which saw Refinement only by its folder; the other parts see no moving type";

        private const string BuiltFromTheRefinementAndReportsKey = "built from the Refinement and Reports key; the other parts see no moving type";

        public static IReadOnlyDictionary<string, PatternDeclaration> PatternRoles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            // The Refinement context, as the other contexts' rules protect or forbid it: it must keep seeing a Refinement type after it moves.
            ["ModuleBoundariesArchUnitTest.RefinementPattern"] = PatternDeclaration.Context(Namespace, "the Refinement module of the module map, which Flow metrics, Forecasting and Work Items allow as a caller and the Refinement guards hold to account"),
            ["FlowMetricsInboundApplicationArchUnitTest.TheLegalCallerNamespaces"] = PatternDeclaration.Composite("ModuleBoundariesArchUnitTest.RefinementPattern", BuiltFromTheRefinementPattern),
            ["ForecastingModuleBoundariesArchUnitTest.ForecastingsLegalCallers"] = PatternDeclaration.Composite("ModuleBoundariesArchUnitTest.RefinementPattern", BuiltFromTheRefinementPattern),
            ["ForecastingModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = PatternDeclaration.Composite("ModuleBoundariesArchUnitTest.RefinementPattern", BuiltFromTheRefinementPattern),
            ["WorkItemsModuleBoundariesArchUnitTest.WorkItemsLegalCallers"] = PatternDeclaration.Composite("ModuleBoundariesArchUnitTest.RefinementPattern", BuiltFromTheRefinementPattern),
            ["WorkItemsModuleBoundariesArchUnitTest.FlowMetricsLegalCallers"] = PatternDeclaration.Composite("ModuleBoundariesArchUnitTest.RefinementPattern", BuiltFromTheRefinementPattern),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing"] = PatternDeclaration.Context(Namespace, "the Refinement context among the contexts Tracker integration must not reach, wherever it lives"),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherContexts"] = PatternDeclaration.Composite("TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing", "built from the Refinement, Reports, Access and Licensing key; the other parts see no moving type"),
            ["AccessModuleBoundariesArchUnitTest.RefinementAndReports"] = PatternDeclaration.Context(Namespace, "the Refinement context Access must not reach, wherever it lives"),
            ["AccessModuleBoundariesArchUnitTest.TheOtherContexts"] = PatternDeclaration.Composite("AccessModuleBoundariesArchUnitTest.RefinementAndReports", BuiltFromTheRefinementAndReportsKey),
            ["LicensingModuleBoundariesArchUnitTest.RefinementAndReports"] = PatternDeclaration.Context(Namespace, "the Refinement context Licensing must not reach, wherever it lives"),
            ["LicensingModuleBoundariesArchUnitTest.AccessAndTheOtherContexts"] = PatternDeclaration.Composite("LicensingModuleBoundariesArchUnitTest.RefinementAndReports", BuiltFromTheRefinementAndReportsKey),

            // The Refinement module's own guards: each names one Refinement type with its nested types, or the services it must stay free of.
            ["RefinementModuleArchUnitTest.ServicesImplementationPattern"] = PatternDeclaration.Context(Namespace, "the services a Refinement rule or validator must not reach into; a Refinement service stays a service after the move"),
            ["RefinementModuleArchUnitTest.RefinementResolutionAndItsNestedTypesPattern"] = PatternDeclaration.Context(FullName, "the refinement resolution and the lambdas compiled beside it, wherever it lives"),
            ["RefinementModuleArchUnitTest.RefinementCadenceCalendarAndItsNestedTypesPattern"] = PatternDeclaration.Context(FullName, "the cadence calendar and the lambdas compiled beside it, wherever it lives"),
            ["RefinementModuleArchUnitTest.NeedBandAndItsNestedTypesPattern"] = PatternDeclaration.Context(FullName, "the need band and the lambdas compiled beside it, wherever it lives"),
            ["RefinementMembershipArchUnitTest.RefinementNamespaces"] = PatternDeclaration.Context(Namespace, "the namespaces the Refinement module is measured in, old and new"),

            // Parts of the code that saw a Refinement type only because it lives in the Services folder; they are declared
            // folder-only below and must not follow the type into Modules.Refinement.
            ["KernelRules.TheServices"] = PatternDeclaration.Context(Namespace, "the services the kernel rules keep the kernel away from, and the former home of the types a layout rule looks for"),
            ["AccessModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Access may sit on"),
            ["AccessModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("AccessModuleBoundariesArchUnitTest.ThePlatform", BuiltFromThePlatformKey),
            ["LicensingModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Licensing may sit on"),
            ["LicensingModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("LicensingModuleBoundariesArchUnitTest.ThePlatform", BuiltFromThePlatformKey),
            ["PromisesModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Promises may sit on"),
            ["PromisesModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("PromisesModuleBoundariesArchUnitTest.ThePlatform", BuiltFromThePlatformKey),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace"] = PatternDeclaration.Context(Namespace, "the implementation code the board port must not name"),

            // Old locations: a rule asks whether a type is still left there, so they must not see the new place.
            ["PromisesLayoutArchUnitTest.WhereTheServicesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Promises services lived before they moved into their module"),
            ["PromisesPoliciesLayoutArchUnitTest.WhereTheHandlersLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Promises handlers lived before they moved into their module"),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Tracker integration application types lived before they moved into their module"),
            ["TrackerIntegrationConnectorsLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the connectors lived before they moved into their module"),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the OAuth and health types lived before they moved into their module"),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheValueTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the OAuth and health value types lived before they moved into their module"),
            ["WorkItemsApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(FullName, "the old Services prefix of a Work Items type named in source text, read on full names"),

            // Patterns that take in the whole backend, the whole application core, or every module, and see a Refinement type in either place.
            ["ApplicationCoreRules.ApplicationCore"] = PatternDeclaration.Context(Namespace, "the application core the core rules hold to account, which takes in Services and every module"),
            ["RealityCheckReadOnlyArchUnitTest.ImplementationPattern"] = PatternDeclaration.Context(Namespace, "the services and modules the read-only check examines"),
            ["StalenessPolicyPurityArchUnitTest.ServicesPattern"] = PatternDeclaration.Context(Namespace, "the services and modules the staleness policy must stay free of"),
            ["PlatformLaneMembershipArchUnitTest.ModuleNamespaces"] = PatternDeclaration.Context(Namespace, "every module, where a domain-event handler may live; Refinement becomes one by the move"),
            ["AccessHttpAdaptersLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["PromisesLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["ConnectorsDeclareNoUnsupportedMembersArchUnitTest.AllOfTheBackend"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["AdditionalFieldInUseRulePurityArchUnitTest.AnythingOfLighthouseOutsideModels"] = PatternDeclaration.Context(Namespace, "everything of the backend outside Models, which takes in Refinement in either place"),
            ["PromisesModuleBoundariesArchUnitTest.OutsideTrackerIntegration"] = PatternDeclaration.Context(Namespace, "everything outside Tracker integration, which takes in Refinement in either place"),
            ["AccessAndLicensingUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["CalendarDayAnchorSeamArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["CategoryListOwnershipGuardTest.ProductionProject"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["DependencySingleDecisionArchUnitTest.BackendProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["FastWithManyFeaturesArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["JiraRouteLiteralsTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["OverTimeReconstructionSeamArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["ProductionSource.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["PromisesUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["SecretReadPathSourceStructureTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["SecretRotationWriteSurfaceTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["TrackerIntegrationUnitTestPlacementTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["UpdateCancellationContextWriterArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
            ["ZonedTimestampParsingArchUnitTest.ProductionProjectDirectory"] = PatternDeclaration.Context(Namespace, AProjectFolder),
        };

        /// <summary>
        /// A part of the code that takes in a whole parent namespace the Refinement types move into, so it sees them for the
        /// first time after the move, and rightly. The check admits that new sight only while the pattern matches the parent
        /// namespace itself and the new namespace of every moving type. Keyed like <see cref="PatternRoles"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, string> WholeParentPatterns { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PlatformLaneMembershipArchUnitTest.ModuleNamespaces"] = "every module, where a domain-event handler may live; Refinement becomes one by the move",
        };

        /// <summary>
        /// A part of the code that saw a moving Refinement type only through the Services folder the type lived in, not
        /// because the type belongs to that part of the code. For that pattern, and every pattern built from it, the type
        /// is read as an old location: the pattern must not follow it into Modules.Refinement. Keyed by the pattern (class
        /// and field, as in <see cref="PatternRoles"/>) and the name of the moving type.
        /// </summary>
        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } =
            new (string Field, string Reason)[]
            {
                ("KernelRules.TheServices", "Refinement is a context of its own, not one of the services the kernel rules or a layout rule's former home name; Modules is in the application core already"),
                ("AccessModuleBoundariesArchUnitTest.ThePlatform", "Refinement is a context Access must not reach, not part of the platform it may sit on"),
                ("LicensingModuleBoundariesArchUnitTest.ThePlatform", "Refinement is a context Licensing must not reach, not part of the platform it may sit on"),
                ("PromisesModuleBoundariesArchUnitTest.ThePlatform", "Refinement is a context of its own, not part of the platform Promises may sit on"),
                ("TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace", "a Refinement service is another context's application code, which Tracker integration's boundary rules keep it away from already"),
            }
            .SelectMany(folderOnly => RefinementMembership.MovingTypes.Select(type => (Key: (folderOnly.Field, type.Name), folderOnly.Reason)))
            .ToDictionary(match => match.Key, match => match.Reason);
    }
}
