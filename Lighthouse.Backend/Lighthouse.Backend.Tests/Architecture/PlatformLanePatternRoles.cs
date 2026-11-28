namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every rule pattern of the test assembly that sees a moving platform type, under its old or its new name, with the
    /// subject its rule matches it against and the role it plays. A pattern is keyed by its class (nested classes joined
    /// with dots) and field name.
    /// </summary>
    internal static class PlatformLanePatternRoles
    {
        private const PatternSubject Namespace = PatternSubject.Namespace;

        private const PatternSubject FullName = PatternSubject.FullName;

        private const string TheWholeBackend = "names the whole backend, so it sees every type wherever it lives";

        private const string AProjectFolder = "the name of the backend project folder, read as a path; it names the whole backend under either name";

        public static IReadOnlyDictionary<string, PatternDeclaration> PatternRoles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            // Parts of the code a rule protects or forbids: they must keep seeing a platform type after it moves.
            ["KernelRules.TheServices"] = PatternDeclaration.Context(Namespace, "the application services the kernel rules keep the kernel away from; the platform services stay among them after the move"),
            ["KernelRules.TheUpdateQueue"] = PatternDeclaration.Context(Namespace, "the update queue, forbidden to the kernel and shared as the update code by Forecasting and Work Items, wherever it lives"),
            ["ApplicationCoreRules.ApplicationCore"] = PatternDeclaration.Context(Namespace, "the application core the core rules hold to account, which takes in the platform"),
            ["AccessModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Access may sit on, wherever its types live"),
            ["AccessModuleBoundariesArchUnitTest.TheUpdateCode"] = PatternDeclaration.Context(Namespace, "the update code Access must not reach, wherever it lives"),
            ["AccessModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("AccessModuleBoundariesArchUnitTest.ThePlatform", "built from the platform key; the other parts see no moving type"),
            ["AccessModuleBoundariesArchUnitTest.TheOtherContexts"] = PatternDeclaration.Composite("AccessModuleBoundariesArchUnitTest.TheUpdateCode", "built from the update-code key; the other parts see no moving type"),
            ["LicensingModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Licensing may sit on, wherever its types live"),
            ["LicensingModuleBoundariesArchUnitTest.TheUpdateCode"] = PatternDeclaration.Context(Namespace, "the update code Licensing must not reach, wherever it lives"),
            ["LicensingModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("LicensingModuleBoundariesArchUnitTest.ThePlatform", "built from the platform key; the other parts see no moving type"),
            ["LicensingModuleBoundariesArchUnitTest.AccessAndTheOtherContexts"] = PatternDeclaration.Composite("LicensingModuleBoundariesArchUnitTest.TheUpdateCode", "built from the update-code key; the other parts see no moving type"),
            ["PromisesModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Promises may sit on, wherever its types live"),
            ["PromisesModuleBoundariesArchUnitTest.TheUpdateCode"] = PatternDeclaration.Context(Namespace, "the update code Promises reaches only through listed edges, wherever it lives"),
            ["PromisesModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("PromisesModuleBoundariesArchUnitTest.ThePlatform", "built from the platform key; the other parts see no moving type"),
            ["PromisesModuleBoundariesArchUnitTest.TheNamespacedUpstreamCode"] = PatternDeclaration.Composite("PromisesModuleBoundariesArchUnitTest.TheUpdateCode", "built from the update-code key; the other parts see no moving type"),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.TheKeyRing"] = PatternDeclaration.Context(Namespace, "the key ring Tracker integration may sit on, wherever it lives"),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.PromisesAndTheUpdateCode"] = PatternDeclaration.Context(Namespace, "Promises and the update code, which Tracker integration must not reach, wherever they live"),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("TrackerIntegrationModuleBoundariesArchUnitTest.TheKeyRing", "built from the key-ring key; the stored-records and demo-data parts are read the same way"),
            ["TrackerIntegrationModuleBoundariesArchUnitTest.OtherContexts"] = PatternDeclaration.Composite("TrackerIntegrationModuleBoundariesArchUnitTest.PromisesAndTheUpdateCode", "built from the Promises and update-code key; the other parts see no moving type"),
            ["WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp"] = PatternDeclaration.Context(Namespace, "the organisation set-up services Work Items reaches only through listed edges; the platform types among them stay reached after the move"),
            ["WorkItemsModuleBoundariesArchUnitTest.AccessLicensingOrganisationSetUpAndTheUpdateQueue"] = PatternDeclaration.Composite("WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp", "built from the organisation set-up key and the kernel's update-queue key, both parts of the code"),
            ["RefinementModuleBoundariesArchUnitTest.TheUpdateCode"] = PatternDeclaration.Context(Namespace, "the update code Refinement's Application must not reach, wherever it lives"),
            ["RefinementModuleBoundariesArchUnitTest.TheForbiddenCode"] = PatternDeclaration.Composite("RefinementModuleBoundariesArchUnitTest.TheUpdateCode", "built from the update-code key, among the EF Core, data-layer, API, module-adapter and Tracker integration parts, none of which sees a moving type"),
            ["RefinementModuleBoundariesArchUnitTest.AccessSetUpPlatformAndRepositories"] = PatternDeclaration.Composite("KernelRules.TheServices", "built from the kernel's services key, among the Access module and the former Access home parts, none of which sees a moving type; Refinement reaches all of them only through listed edges"),
            ["DependencyAwareForecastSeamArchUnitTest.TheUpdateComponents"] = PatternDeclaration.Context(Namespace, "the update components the forecast seam must not reach, wherever they live"),
            ["ModuleBoundariesArchUnitTest.PortfolioDeliveryPattern"] = PatternDeclaration.Context(Namespace, "the portfolio and delivery update layer of the module map, wherever its types live"),
            ["RefinementModuleArchUnitTest.ServicesImplementationPattern"] = PatternDeclaration.Context(Namespace, "the service implementations Refinement must not depend on; a platform implementation stays one after the move"),
            ["RefinementModuleArchUnitTest.BackgroundUpdatesPattern"] = PatternDeclaration.Context(Namespace, "the background update code Refinement must not depend on, wherever it lives"),
            ["RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace"] = PatternDeclaration.Context(Namespace, "the flat service implementations the blackout seam must not depend on; the platform implementations among them stay forbidden after the move"),
            ["RealityCheckReadOnlyArchUnitTest.ImplementationPattern"] = PatternDeclaration.Context(Namespace, "the implementations the read-only check examines, which take in the platform"),
            ["StalenessPolicyPurityArchUnitTest.ServicesPattern"] = PatternDeclaration.Context(Namespace, "the services the staleness policy must stay free of, which take in the platform"),
            ["RepositoryRecognition.PlatformPersistencePattern"] = PatternDeclaration.Context(Namespace, "the persistence code a repository is recognised by, wherever the database and seeding types live"),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace"] = PatternDeclaration.Context(Namespace, "the implementation code the board port must not name; a platform implementation stays one after the move"),
            ["PlatformOwnedModelsTypesLiveInPlatformArchUnitTest.TheLanesUsageDataNamespace"] = PatternDeclaration.Place(Namespace, "the platform lane's usage-data namespace, where the usage-data types that left the Models are looked up"),
            ["PlatformLaneMembershipArchUnitTest.PlatformLaneNamespaces"] = PatternDeclaration.Context(Namespace, "the namespaces the platform lane is measured in, old and new"),

            // Old locations: a rule asks whether a type is still left there, so they must not see the new place.
            ["PromisesLayoutArchUnitTest.WhereTheServicesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Promises services lived before they moved into their module; a Promises type is never looked for in the platform"),
            ["PromisesPoliciesLayoutArchUnitTest.WhereTheHandlersLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Promises handlers lived before they moved into their module"),
            ["PromisesMembershipArchUnitTest.NamespacesOfTheMembersFoundByName"] = PatternDeclaration.Place(Namespace, "the flat Services namespaces where Promises members are found by name; no Promises member moves into the platform"),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Tracker integration application types lived before they moved into their module"),
            ["TrackerIntegrationConnectorsLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the connectors lived before they moved into their module"),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the OAuth and health types lived before they moved into their module"),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheValueTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the OAuth and health value types lived before they moved into their module"),
            ["WorkItemsApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(FullName, "the old Services prefix of a Work Items type named in source text, read on full names"),
            ["WorkItemsPoliciesLayoutArchUnitTest.WhereTheHandlersLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Work Items handlers lived before they moved into their module"),
            ["WorkItemsPoliciesLayoutArchUnitTest.AnOldNamespacePrefix"] = PatternDeclaration.Place(FullName, "the old namespace prefix of a Work Items handler named in source text, read on full names"),

            // Patterns that take in the whole backend, or everything outside one module, and see every type in either place.
            ["AccessHttpAdaptersLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["PromisesLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["ConnectorsDeclareNoUnsupportedMembersArchUnitTest.AllOfTheBackend"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["AdditionalFieldInUseRulePurityArchUnitTest.AnythingOfLighthouseOutsideModels"] = PatternDeclaration.Context(Namespace, "everything of the backend outside Models, which takes in the platform in either place"),
            ["PromisesModuleBoundariesArchUnitTest.OutsideTrackerIntegration"] = PatternDeclaration.Context(Namespace, "everything outside Tracker integration, which takes in the platform in either place"),
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
        /// A part of the code that saw a platform area through the loose Services.Implementation or Services.Interfaces
        /// namespace and no longer reads those namespaces, because Organisation set-up, the last part of the code it read
        /// there, now lives in its own module. Every platform type that lived in the loose namespaces has left them, so the
        /// part of the code no longer sees that area in its old place, which the role check would read as an area it never
        /// saw; the check admits that for these patterns only, while no platform type is left in the loose namespaces.
        /// Keyed like <see cref="PatternRoles"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, string> NoLongerReadTheLooseServicesNamespaces { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp"] = "names set-up by its module and the platform areas by their new place; the loose services Work Items still reaches are named by full name",
            ["RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace"] = "names set-up by its module and the platform implementations by their new place; the loose implementations are named by full name",
            ["TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace"] = "names set-up by its module and the platform by its new place; the loose implementations are named by full name",
        };

        /// <summary>
        /// A part of the code that saw one moving type only through the folder the type lived in, not because the type
        /// belongs to that part of the code. For that pattern, and every pattern built from it, the type is read as an
        /// old location: the pattern must not follow the type into its new place, and the type's area is not widened.
        /// Keyed by the pattern (class and field, as in <see cref="PatternRoles"/>) and the name of the moving type.
        /// </summary>
        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } = new Dictionary<(string Field, string Type), string>
        {
            [("TrackerIntegrationModuleBoundariesArchUnitTest.PromisesAndTheUpdateCode", "KeyRingFileWatcher")] = "the key-ring watcher lived in the BackgroundServices folder the update code also lives in; it is neither Promises nor update code, so Tracker integration may still sit on the key ring after the move",
        };
    }
}
