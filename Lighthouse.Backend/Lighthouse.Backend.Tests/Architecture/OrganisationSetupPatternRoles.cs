namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every rule pattern of the test assembly that sees a moving Organisation set-up type, under its old or its new name, with
    /// the subject its rule matches it against and the role it plays. A pattern is keyed by its class (nested classes joined
    /// with dots) and field name.
    /// </summary>
    internal static class OrganisationSetupPatternRoles
    {
        private const PatternSubject Namespace = PatternSubject.Namespace;

        private const PatternSubject FullName = PatternSubject.FullName;

        private const string TheWholeBackend = "names the whole backend, so it sees every type wherever it lives";

        private const string AProjectFolder = "the name of the backend project folder, read as a path; it names the whole backend under either name";

        private const string BuiltFromThePlatformKey = "built from the platform key, which saw set-up only by its folder; the other parts see no moving type";

        public static IReadOnlyDictionary<string, PatternDeclaration> PatternRoles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            // Organisation set-up, as the other contexts' rules protect or forbid it: it must keep seeing a set-up type after it moves.
            ["WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp"] = PatternDeclaration.Context(Namespace, "the organisation set-up services Work Items reaches only through listed edges"),
            ["WorkItemsModuleBoundariesArchUnitTest.AccessLicensingOrganisationSetUpAndTheUpdateQueue"] = PatternDeclaration.Composite("WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp", "built from the organisation set-up key; the other parts see no moving type"),
            ["RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace"] = PatternDeclaration.Context(Namespace, "the service implementations the recurring-rule entity must not depend on, among them the blackout period service"),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace"] = PatternDeclaration.Context(Namespace, "the implementation code the board port must not name, set-up's services among it"),
            ["RefinementModuleArchUnitTest.WeeklyRecurrenceAndItsNestedTypesPattern"] = PatternDeclaration.Context(FullName, "the weekly recurrence Refinement's cadence calendar may use, wherever it lives"),

            // Parts of the code that see a set-up type only because it lives in the loose Services namespaces; they are
            // declared folder-only below and must not follow the type into Modules.OrganisationSetup.
            ["KernelRules.TheServices"] = PatternDeclaration.Context(Namespace, "the services the kernel rules keep the kernel away from, and the former home of the types a layout rule looks for"),
            ["AccessModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Access may sit on"),
            ["AccessModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("AccessModuleBoundariesArchUnitTest.ThePlatform", BuiltFromThePlatformKey),
            ["LicensingModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Licensing may sit on"),
            ["LicensingModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("LicensingModuleBoundariesArchUnitTest.ThePlatform", BuiltFromThePlatformKey),
            ["PromisesModuleBoundariesArchUnitTest.ThePlatform"] = PatternDeclaration.Context(Namespace, "the platform Promises may sit on"),
            ["PromisesModuleBoundariesArchUnitTest.TheLayersBelowTheApplicationAndThePlatform"] = PatternDeclaration.Composite("PromisesModuleBoundariesArchUnitTest.ThePlatform", BuiltFromThePlatformKey),
            ["RefinementModuleArchUnitTest.ServicesImplementationPattern"] = PatternDeclaration.Context(Namespace, "the service implementations a Refinement rule or validator must stay out of"),
            ["RefinementModuleBoundariesArchUnitTest.AccessSetUpPlatformAndRepositories"] = PatternDeclaration.Composite("KernelRules.TheServices", "built from the kernel's services key, among the Access, platform and repository parts"),
            ["PlatformLaneMembershipArchUnitTest.PlatformLaneNamespaces"] = PatternDeclaration.Context(Namespace, "the namespaces the platform lane is measured in, old and new"),

            // Old locations: a rule asks whether a type is still left there, so they must not see the new place.
            ["PromisesLayoutArchUnitTest.WhereTheServicesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Promises services lived before they moved into their module"),
            ["PromisesPoliciesLayoutArchUnitTest.WhereTheHandlersLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Promises handlers lived before they moved into their module"),
            ["PromisesMembershipArchUnitTest.NamespacesOfTheMembersFoundByName"] = PatternDeclaration.Place(Namespace, "the loose Services namespaces where Promises members are found by name"),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the Tracker integration application types lived before they moved into their module"),
            ["TrackerIntegrationConnectorsLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the connectors lived before they moved into their module"),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the OAuth and health types lived before they moved into their module"),
            ["TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.WhereTheValueTypesLivedBefore"] = PatternDeclaration.Place(Namespace, "where the OAuth and health value types lived before they moved into their module"),
            ["WorkItemsApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore"] = PatternDeclaration.Place(FullName, "the old Services prefix of a Work Items type named in source text, read on full names"),

            // Patterns that take in the whole backend, the application core or every module, and see a set-up type in either place.
            ["ApplicationCoreRules.ApplicationCore"] = PatternDeclaration.Context(Namespace, "the application core the core rules hold to account, which takes in Services and every module"),
            ["RealityCheckReadOnlyArchUnitTest.ImplementationPattern"] = PatternDeclaration.Context(Namespace, "the services and modules the read-only check examines"),
            ["StalenessPolicyPurityArchUnitTest.ServicesPattern"] = PatternDeclaration.Context(Namespace, "the services and modules the staleness policy must stay free of"),
            ["PlatformLaneMembershipArchUnitTest.ModuleNamespaces"] = PatternDeclaration.Context(Namespace, "every module, where a domain-event handler may live; set-up becomes one by the move"),
            ["AccessHttpAdaptersLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["PromisesLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["TrackerIntegrationApplicationLayoutArchUnitTest.TheWholeAssembly"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["ConnectorsDeclareNoUnsupportedMembersArchUnitTest.AllOfTheBackend"] = PatternDeclaration.Context(Namespace, TheWholeBackend),
            ["AdditionalFieldInUseRulePurityArchUnitTest.AnythingOfLighthouseOutsideModels"] = PatternDeclaration.Context(Namespace, "everything of the backend outside Models, which takes in set-up in either place"),
            ["PromisesModuleBoundariesArchUnitTest.OutsideTrackerIntegration"] = PatternDeclaration.Context(Namespace, "everything outside Tracker integration, which takes in set-up in either place"),
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
        /// A part of the code that takes in a whole parent namespace the set-up types move into, so it sees them for the first
        /// time after the move, and rightly. The check admits that new sight only while the pattern matches the parent
        /// namespace itself and the new namespace of every moving type. Keyed like <see cref="PatternRoles"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, string> WholeParentPatterns { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PlatformLaneMembershipArchUnitTest.ModuleNamespaces"] = "every module, where a domain-event handler may live; set-up becomes one by the move",
        };

        /// <summary>
        /// A part of the code that saw a moving set-up type only through the Services folder the type lived in, not because the
        /// type belongs to that part of the code. For that pattern, and every pattern built from it, the type is read as an old
        /// location: the pattern must not follow it into Modules.OrganisationSetup. Keyed by the pattern and the type's name.
        /// </summary>
        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } =
            new (string Field, string Reason)[]
            {
                ("KernelRules.TheServices", "set-up becomes a module of its own, not one of the services the kernel rules or a layout rule's former home name; Modules is in the application core already"),
                ("AccessModuleBoundariesArchUnitTest.ThePlatform", "set-up is a context, not part of the platform Access may sit on"),
                ("LicensingModuleBoundariesArchUnitTest.ThePlatform", "set-up is a context, not part of the platform Licensing may sit on"),
                ("PromisesModuleBoundariesArchUnitTest.ThePlatform", "set-up is a context, not part of the platform Promises may sit on"),
                ("RefinementModuleArchUnitTest.ServicesImplementationPattern", "set-up is a context of its own, not one of the loose service implementations a Refinement rule or validator must stay out of"),
                ("PlatformLaneMembershipArchUnitTest.PlatformLaneNamespaces", "set-up types sit in the loose namespaces the platform lane is measured in, but are not platform"),
            }
            .SelectMany(folderOnly => OrganisationSetupMembership.MovingTypes.Select(type => (Key: (folderOnly.Field, type.Name), folderOnly.Reason)))
            .ToDictionary(match => match.Key, match => match.Reason);
    }
}
