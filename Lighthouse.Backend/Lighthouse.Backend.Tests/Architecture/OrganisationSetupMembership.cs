namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>The layer of the Organisation set-up module a moving type goes to.</summary>
    internal enum OrganisationSetupLayer
    {
        Domain,
        Application,
        Contracts,
        ApplicationConnections,
        ApplicationOptionalFeatures,
    }

    /// <summary>
    /// A type of the Organisation set-up module: found today in its old namespace, after the move in
    /// Lighthouse.Backend.Modules.OrganisationSetup.Domain, .Application, .Contracts, .Application.Connections or .Application.OptionalFeatures.
    /// </summary>
    internal sealed record OrganisationSetupType(string Name, OrganisationSetupLayer Layer, string OldNamespace)
    {
        public const string NewRootNamespace = "Lighthouse.Backend.Modules.OrganisationSetup";

        public string NewNamespace => Layer switch
        {
            OrganisationSetupLayer.Domain => $"{NewRootNamespace}.Domain",
            OrganisationSetupLayer.Application => $"{NewRootNamespace}.Application",
            OrganisationSetupLayer.Contracts => $"{NewRootNamespace}.Contracts",
            OrganisationSetupLayer.ApplicationConnections => $"{NewRootNamespace}.Application.Connections",
            OrganisationSetupLayer.ApplicationOptionalFeatures => $"{NewRootNamespace}.Application.OptionalFeatures",
            _ => throw new ArgumentOutOfRangeException(nameof(Layer), Layer, "An Organisation set-up type goes to the Domain, the Application or one of its two groupings."),
        };

        public string OldFullName => $"{OldNamespace}.{Name}";

        public string NewFullName => $"{NewNamespace}.{Name}";
    }

    /// <summary>An Organisation set-up file that stays where it is, with the part of the code that owns it and why it stays.</summary>
    internal sealed record OrganisationSetupFileThatStays(string File, string Owner, string Reason);

    /// <summary>
    /// A reference from one type to a moving type that the compiler resolves today only because the referring type sits in
    /// the moving type's namespace or below it, and that the move breaks: after the move the referring type is neither in
    /// the moving type's new namespace nor below it, so it needs a using line. Origin is the referring type's short name when
    /// it moves too, its full name when it stays; Target is the moving type's short name.
    /// </summary>
    internal sealed record OrganisationSetupImplicitEdge(string Origin, string Target);

    /// <summary>
    /// The Organisation set-up module, measured from the compiled code: every type that moves into
    /// Lighthouse.Backend.Modules.OrganisationSetup with its layer and old namespace, the set-up files that stay with their owner
    /// and reason, and the references the move takes away from namespace scope. A type is listed with its old namespace and
    /// found in that namespace or in its new one, so the map holds before, during and after the move.
    /// </summary>
    internal static class OrganisationSetupMembership
    {
        // Properties, not constants: another move's preservation check reads every string constant of the test project
        // as a rule pattern, and the loose namespaces are not a rule.
        private static string Implementation => "Lighthouse.Backend.Services.Implementation";

        private static string Interfaces => "Lighthouse.Backend.Services.Interfaces";

        public static IReadOnlyList<OrganisationSetupType> MovingTypes { get; } =
        [
            // The calendar: blackout periods and recurring blackout rules.
            new("WeeklyRecurrence", OrganisationSetupLayer.Domain, Implementation),
            new("RecurringBlackoutRuleExtensions", OrganisationSetupLayer.Domain, Implementation),
            new("BlackoutPeriodService", OrganisationSetupLayer.Application, Implementation),
            new("RecurringBlackoutRuleService", OrganisationSetupLayer.Application, Implementation),
            new("IBlackoutPeriodService", OrganisationSetupLayer.Contracts, Interfaces),
            new("IRecurringBlackoutRuleService", OrganisationSetupLayer.Application, Interfaces),

            // Feature ordering and ranking.
            new("FeatureOrdering", OrganisationSetupLayer.Application, Implementation),
            new("FeatureOrderingPolicyProvider", OrganisationSetupLayer.Application, Implementation),
            new("FeatureRankingService", OrganisationSetupLayer.Application, Implementation),
            new("FeatureRankSeeder", OrganisationSetupLayer.Application, Implementation),
            new("IFeatureOrdering", OrganisationSetupLayer.Contracts, Interfaces),
            new("IFeatureOrderingPolicyProvider", OrganisationSetupLayer.Application, Interfaces),
            new("IFeatureRankingService", OrganisationSetupLayer.Application, Interfaces),
            new("IFeatureRankSeeder", OrganisationSetupLayer.Application, Interfaces),

            // The optional-features mechanism, which moves with ordering.
            new("FeatureOrderingApplier", OrganisationSetupLayer.ApplicationOptionalFeatures, $"{Implementation}.OptionalFeatures"),
            new("DefaultOptionalFeatureApplier", OrganisationSetupLayer.ApplicationOptionalFeatures, $"{Implementation}.OptionalFeatures"),
            new("OptionalFeatureApplierRegistry", OrganisationSetupLayer.ApplicationOptionalFeatures, $"{Implementation}.OptionalFeatures"),
            new("IOptionalFeatureApplier", OrganisationSetupLayer.ApplicationOptionalFeatures, $"{Interfaces}.OptionalFeatures"),

            // Settings and terminology.
            new("AppSettingService", OrganisationSetupLayer.Application, Implementation),
            new("TerminologyService", OrganisationSetupLayer.Application, Implementation),
            new("IAppSettingService", OrganisationSetupLayer.Contracts, Interfaces),
            new("ITerminologyService", OrganisationSetupLayer.Application, Interfaces),

            // Connection defaults and the connection usage.
            new("ConnectionSaveService", OrganisationSetupLayer.ApplicationConnections, $"{Implementation}.Connections"),
            new("ChangedConnectionDefault", OrganisationSetupLayer.ApplicationConnections, $"{Implementation}.Connections"),
            new("WorkTrackingSystemConnectionUsage", OrganisationSetupLayer.ApplicationConnections, Implementation),
            new("IConnectionSaveService", OrganisationSetupLayer.ApplicationConnections, Interfaces),
            new("IWorkTrackingSystemConnectionUsage", OrganisationSetupLayer.ApplicationConnections, Interfaces),
        ];

        public static IReadOnlyList<OrganisationSetupFileThatStays> FilesThatStay { get; } =
        [
            .. Staying(
                "Organisation set-up",
                "a context seeder; module start-up decides where seeders live, and the seeders keep their registration order until then",
                "Services/Implementation/Seeding/AppSettingSeeder.cs",
                "Services/Implementation/Seeding/TerminologySeeder.cs",
                "Services/Implementation/Seeding/OptionalFeatureSeeder.cs",
                "Services/Implementation/Seeding/CategoryListNormalisationSeeder.cs"),
            .. Staying(
                "the feature list",
                "numbers the Features table by the set-up ordering, so it stays with the feature-list read side that uses it",
                "Services/Implementation/FeaturePositionMap.cs",
                "Services/Interfaces/IFeaturePositionMap.cs"),
            .. Staying(
                "Organisation set-up",
                "a repository of settings, terminology or blackouts; repositories move when the persistence adapters are reshaped",
                "Services/Implementation/Repositories/AppSettingRepository.cs",
                "Services/Implementation/Repositories/TerminologyRepository.cs",
                "Services/Implementation/Repositories/BlackoutPeriodRepository.cs",
                "Services/Implementation/Repositories/RecurringBlackoutRuleRepository.cs"),
            .. Staying(
                "Teams",
                "the Team repository moves when the Team persistence is reshaped",
                "Services/Implementation/Repositories/TeamRepository.cs"),
            .. Staying(
                "Portfolios",
                "the Portfolio repository and its port move when the Portfolio persistence is reshaped",
                "Services/Implementation/Repositories/PortfolioRepository.cs",
                "Services/Interfaces/Repositories/IPortfolioRepository.cs"),
            .. Staying(
                "connections",
                "the connection repository moves when the connection persistence is reshaped",
                "Services/Implementation/Repositories/WorkTrackingSystemConnectionRepository.cs"),
            .. Staying(
                "the kernel",
                "an entity, a type an entity reaches, a type on the wire or an event other contexts handle; the kernel keeps it",
                "Models/AppSetting.cs",
                "Models/TerminologyEntry.cs",
                "Models/BlackoutPeriod.cs",
                "Models/RecurringBlackoutRule.cs",
                "Models/SettingSource.cs",
                "Models/EffectiveSetting.cs",
                "Models/IncomingSetting.cs",
                "Models/WorkTrackingSystemOptionsOwner.cs",
                "Models/BlackoutDaysExtensions.cs",
                "Models/FeatureOrderingPolicy.cs",
                "Modules/OrganisationSetup/Domain/ManualRankComparer.cs",
                "Modules/OrganisationSetup/Contracts/TeamDeleted.cs",
                "Modules/OrganisationSetup/Contracts/BlackoutConfigurationChanged.cs",
                "Modules/OrganisationSetup/Contracts/FeatureOrderingPolicyChanged.cs",
                "Modules/OrganisationSetup/Contracts/FeatureRankChanged.cs"),
            .. Staying(
                "the API",
                "a driving adapter of set-up; controllers, their helpers and their DTOs stay in the API",
                "API/TeamController.cs",
                "API/PortfolioController.cs",
                "API/AppSettingsController.cs",
                "API/TerminologyController.cs",
                "API/BlackoutPeriodsController.cs",
                "API/RecurringBlackoutRulesController.cs",
                "API/OptionalFeaturesController.cs",
                "API/WorkTrackingSystemConnectionController.cs",
                "API/WorkTrackingSystemConnectionsController.cs",
                "API/Helpers/OwnerSettingRefusals.cs",
                "API/Helpers/WorkTrackingSystemOptionsOwnerExtensions.cs"),
            .. Staying(
                "the update pipelines",
                "the Team and Portfolio update pipelines move when the structure of units is reshaped",
                "Services/Implementation/BackgroundServices/Update/TeamUpdater.cs",
                "Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs"),
            .. Staying(
                "the write-back round",
                "the write-back round lives in Forecasting and is not part of Organisation set-up",
                "Modules/Forecasting/Application/WriteBack/WriteBackRound.cs",
                "Modules/Forecasting/Application/WriteBack/WriteBackRoundContext.cs",
                "Modules/Forecasting/Application/WriteBack/WriteBackCollector.cs"),
        ];

        public static IReadOnlyList<string> DomainCandidates { get; } = ["WeeklyRecurrence", "RecurringBlackoutRuleExtensions"];

        public static IReadOnlyList<OrganisationSetupImplicitEdge> ImplicitEdges { get; } =
        [
            // The blackout service calls the recurring-rule extension methods, which it reaches only by sharing their
            // namespace today; once the extensions sit in the Domain and the service in the Application it needs a using line.
            new("BlackoutPeriodService", "RecurringBlackoutRuleExtensions"),
        ];

        private static IEnumerable<OrganisationSetupFileThatStays> Staying(string owner, string reason, params string[] files)
            => files.Select(file => new OrganisationSetupFileThatStays(file, owner, reason));
    }
}
