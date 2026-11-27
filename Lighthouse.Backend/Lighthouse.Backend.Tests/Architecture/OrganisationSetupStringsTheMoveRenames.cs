namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A string in a test file that names a moving Organisation set-up type, file or namespace by its old Services path, and the
    /// text it reads after the move. MayLeave marks a string the move later takes away altogether: a list entry that leaves its
    /// list once the edge it allows becomes legal, a row of a list of files that stay in Services, or the expected offender of a
    /// made-up type whose rule stops reading the moved type as forbidden, so the made-up type is pointed at another one.
    /// </summary>
    internal sealed record OrganisationSetupRename(string File, string OldText, string NewText, bool MayLeave = false)
    {
        public RenamedString AsRenamedString() => new(File, OldText, NewText);
    }

    /// <summary>
    /// Every string in the test project that names a moving Organisation set-up type by its old Services path, taken from a
    /// search of the test project before the move, as old and new text. A string is accepted in either form, so the list
    /// holds before, during and after the move and is never edited by a later step.
    /// </summary>
    internal static class OrganisationSetupStringsTheMoveRenames
    {
        private const string Architecture = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/";

        private const string NewApplicationFolder = "Modules/OrganisationSetup/Application/";

        public static IReadOnlyList<OrganisationSetupRename> Entries { get; } =
        [
            new(Architecture + "RecurringBlackoutEventsSeamArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.IBlackoutPeriodService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IBlackoutPeriodService"),
            new(Architecture + "WorkItemsMembershipArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Architecture + "TrackerIntegrationUnitTestPlacementTest.cs",
                "Services/Implementation/Connections/ConnectionSaveService.cs",
                NewApplicationFolder + "Connections/ConnectionSaveService.cs"),

            // Edges into set-up listed by other contexts; the move renames their target.
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Platform.Host.SystemInfoService -> Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Platform.Host.SystemInfoService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Platform.Seeding.InstallTimestampSeeder -> Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Platform.Seeding.InstallTimestampSeeder -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Platform.UsageData.UsageDataConsentService -> Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Platform.UsageData.UsageDataConsentService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService", MayLeave: true),
            new(Architecture + "WorkItemsModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Implementation.Connections.ConnectionSaveService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService"),

            // Listed edges that leave their list once set-up's Application and Domain become legal targets by namespace.
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Modules.Promises.Application.DeliveryMetricValuesProjector -> Lighthouse.Backend.Services.Interfaces.IBlackoutPeriodService",
                "Lighthouse.Backend.Modules.Promises.Application.DeliveryMetricValuesProjector -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IBlackoutPeriodService", MayLeave: true),
            new(Architecture + "RefinementModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Modules.Refinement.Application.RefinementCadenceCalendar -> Lighthouse.Backend.Services.Implementation.WeeklyRecurrence",
                "Lighthouse.Backend.Modules.Refinement.Application.RefinementCadenceCalendar -> Lighthouse.Backend.Modules.OrganisationSetup.Domain.WeeklyRecurrence", MayLeave: true),
            new(Architecture + "RefinementModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Modules.Refinement.Application.RefinementCalendar -> Lighthouse.Backend.Services.Interfaces.IBlackoutPeriodService",
                "Lighthouse.Backend.Modules.Refinement.Application.RefinementCalendar -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IBlackoutPeriodService", MayLeave: true),
            new(Architecture + "WorkItemsModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Modules.WorkItems.Application.RefreshLogService -> Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Modules.WorkItems.Application.RefreshLogService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService", MayLeave: true),
            new(Architecture + "WorkItemsModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService -> Lighthouse.Backend.Services.Interfaces.IFeatureOrdering",
                "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IFeatureOrdering", MayLeave: true),

            new(Architecture + "RefinementModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Modules.Refinement.Application.RefinementNeedCalculator -> Lighthouse.Backend.Services.Interfaces.IBlackoutPeriodService",
                "Lighthouse.Backend.Modules.Refinement.Application.RefinementNeedCalculator -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IBlackoutPeriodService", MayLeave: true),
            new(Architecture + "RefinementModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.IBlackoutPeriodService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IBlackoutPeriodService", MayLeave: true),
            new(Architecture + "RepositoryLeakRatchetArchUnitTest.cs",
                "Services/Implementation/Connections/ConnectionSaveService.cs",
                NewApplicationFolder + "Connections/ConnectionSaveService.cs"),
            new(Architecture + "RepositoryLeakRatchetArchUnitTest.cs",
                "Services/Implementation/WorkTrackingSystemConnectionUsage.cs",
                NewApplicationFolder + "Connections/WorkTrackingSystemConnectionUsage.cs"),

            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService", MayLeave: true),
            new(Architecture + "ReportsModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.IAppSettingService",
                "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService", MayLeave: true),

            // The platform lane's list of files that stay in Services names each moving set-up file; the row goes when its file moves.
            .. OrganisationSetupMembership.MovingTypes.Select(type => new OrganisationSetupRename(
                Architecture + "PlatformLaneUnitTestPlacementTest.cs",
                PathOf(type.OldNamespace, type.Name),
                PathOf(type.NewNamespace, type.Name),
                MayLeave: true)),

            // The Promises placement list names the ordering files as set-up's; the rows leave the list when their files move.
            .. RowsThatLeaveThePromisesPlacementList(
                ("Services/Implementation/FeatureOrdering.cs", "FeatureOrdering.cs"),
                ("Services/Implementation/FeatureOrderingPolicyProvider.cs", "FeatureOrderingPolicyProvider.cs"),
                ("Services/Implementation/FeatureRankingService.cs", "FeatureRankingService.cs"),
                ("Services/Implementation/FeatureRankSeeder.cs", "FeatureRankSeeder.cs"),
                ("Services/Implementation/OptionalFeatures/FeatureOrderingApplier.cs", "OptionalFeatures/FeatureOrderingApplier.cs"),
                ("Services/Interfaces/IFeatureOrdering.cs", "IFeatureOrdering.cs"),
                ("Services/Interfaces/IFeatureOrderingPolicyProvider.cs", "IFeatureOrderingPolicyProvider.cs"),
                ("Services/Interfaces/IFeatureRankingService.cs", "IFeatureRankingService.cs"),
                ("Services/Interfaces/IFeatureRankSeeder.cs", "IFeatureRankSeeder.cs")),
        ];

        private static string PathOf(string namespaceName, string typeName)
            => $"{namespaceName["Lighthouse.Backend.".Length..].Replace('.', '/')}/{typeName}.cs";

        private static IEnumerable<OrganisationSetupRename> RowsThatLeaveThePromisesPlacementList(params (string OldPath, string NewFile)[] rows)
            => rows.Select(row => new OrganisationSetupRename(Architecture + "PromisesUnitTestPlacementTest.cs", row.OldPath, NewApplicationFolder + row.NewFile, MayLeave: true));
    }
}
