namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>The move that takes a platform area out of Services and into Lighthouse.Backend.Platform.</summary>
    internal enum PlatformMove
    {
        KeyRing,
        DatabaseAndSeeding,
        UsageDataLoggingAndReleases,
        DomainEventsAndHost,
        UpdateQueue,
    }

    /// <summary>A type of the platform lane: found today in its old namespace, after the move in Lighthouse.Backend.Platform.Area.</summary>
    internal sealed record PlatformLaneType(string Name, string Area, string OldNamespace)
    {
        public string NewNamespace => $"Lighthouse.Backend.Platform.{Area}";

        public string OldFullName => $"{OldNamespace}.{Name}";

        public string NewFullName => $"{NewNamespace}.{Name}";
    }

    /// <summary>A type that sits beside the platform types today and stays where it is, with the part of the code that owns it.</summary>
    internal sealed record TypeThatStays(string Name, string Namespace, string Owner)
    {
        public string FullName => $"{Namespace}.{Name}";
    }

    /// <summary>A whole namespace next to the platform lane that stays where it is, with its owner.</summary>
    internal sealed record NamespaceThatStays(string Namespace, string Owner);

    /// <summary>
    /// A suppression or analyser-config target that names a moving type. Target is written from the type's name on, without
    /// its namespace, so the entry holds before and after the move; the move rewrites the namespace in the same commit.
    /// </summary>
    internal sealed record SuppressionTargetToRewrite(string File, string Target, PlatformMove RewrittenBy);

    /// <summary>A string in a test or script that names a moving type or its file, and the text it reads after the move.</summary>
    internal sealed record StringTheMoveRenames(string File, string OldText, string NewText, PlatformMove RenamedBy);

    /// <summary>
    /// The platform lane, measured from the compiled code: every type that moves into Lighthouse.Backend.Platform with its
    /// area and old namespace, the types beside them that stay with their owner, the shared Models types weighed for the
    /// lane, the suppression targets and the strings the move must rewrite. A type is listed with its old namespace and found
    /// in that namespace or in its new one, so the map holds before, during and after the move.
    /// </summary>
    internal static class PlatformLaneMembership
    {
        private const string TheSuppressionsFile = "Lighthouse.Backend/Lighthouse.Backend/GlobalSuppressions.cs";

        public static IReadOnlyDictionary<string, PlatformMove> Areas { get; } = new Dictionary<string, PlatformMove>(StringComparer.Ordinal)
        {
            ["Encryption"] = PlatformMove.KeyRing,
            ["DatabaseManagement"] = PlatformMove.DatabaseAndSeeding,
            ["DemoData"] = PlatformMove.DatabaseAndSeeding,
            ["Seeding"] = PlatformMove.DatabaseAndSeeding,
            ["Persistence"] = PlatformMove.DatabaseAndSeeding,
            ["UsageData"] = PlatformMove.UsageDataLoggingAndReleases,
            ["Logging"] = PlatformMove.UsageDataLoggingAndReleases,
            ["Releases"] = PlatformMove.UsageDataLoggingAndReleases,
            ["DomainEvents"] = PlatformMove.DomainEventsAndHost,
            ["Host"] = PlatformMove.DomainEventsAndHost,
            ["UpdateQueue"] = PlatformMove.UpdateQueue,
        };

        public static IReadOnlyList<PlatformLaneType> MovingTypes { get; } =
        [
            .. In("Encryption", "Lighthouse.Backend.Services.Implementation.Encryption",
                "AKeyOnlyItsOwnerCanReplace",
                "ConfiguredKeyRingSource",
                "ConnectionSecrets",
                "DatabaseSecretPresenceProbe",
                "DatabaseSecretReadabilityProbe",
                "EncryptionKeyRingBootstrapper",
                "EncryptionKeyRingHolder",
                "GeneratedKeyRingMinter",
                "GeneratedKeyRingStore",
                "IKeyStoreFileSystem",
                "IStoredSecretPresenceProbe",
                "IStoredSecretReadabilityProbe",
                "KeyRingSerializer",
                "KeyStoreCase",
                "KeyStoreFile",
                "KeyStoreLocation",
                "KeyStoreMigration",
                "KeyStoreMigrationOutcome",
                "KeyStoreResolver",
                "KeySupply",
                "KeyThatReadsNothing",
                "LegacyDefaultEncryptionKey",
                "MintingNotPermittedException",
                "MountedFileKeyRingSource",
                "NoDurableKeyStore",
                "OneSecretPassAtATime",
                "PhysicalKeyStoreFileSystem",
                "PublishedKeyAsTheActiveKey",
                "PublishedKeySecretCount",
                "ReadableSecretsNotOnTheActiveKey",
                "ReferencedKeyIds",
                "SecretCustodyService",
                "SecretEnvelope",
                "SecretStateClassifier",
                "StoredSecret",
                "StoredSecretFinding",
                "StoredSecretLocation",
                "StoredSecretLocations",
                "StoredSecretPresence",
                "StoredSecretReadability",
                "StoredSecretSummaryReader",
                "SuppliedKeyRing",
                "UnreadableSecretException",
                "UnreadableStoredSecrets",
                "WhereTheKeyCameFrom"),
            .. In("Encryption", "Lighthouse.Backend.Services.Interfaces.Encryption",
                "IEncryptionKeyRingHolder",
                "IKeyRingMinter",
                "IPublishedKeySecretCount",
                "IReadableSecretsNotOnTheActiveKey",
                "IReferencedKeyIds",
                "ISecretCustodyReader",
                "ISecretCustodyService",
                "IStoredSecretSummary",
                "KeyCustodyDescription",
                "StoredSecretSummary"),
            .. In("Encryption", "Lighthouse.Backend.Services.Implementation",
                "CryptoService"),
            .. In("Encryption", "Lighthouse.Backend.Services.Interfaces",
                "ICryptoService"),
            .. In("Encryption", "Lighthouse.Backend.Services.Implementation.BackgroundServices",
                "KeyRingFileWatcher"),
            .. In("DatabaseManagement", "Lighthouse.Backend.Services.Implementation.DatabaseManagement",
                "CommandRunner",
                "DatabaseMaintenanceGate",
                "DatabaseManagementService",
                "DatabaseOperationTracker",
                "GateAcquisitionResult",
                "PostgresDatabaseManagementProvider",
                "SqliteDatabaseManagementProvider"),
            .. In("DatabaseManagement", "Lighthouse.Backend.Services.Interfaces.DatabaseManagement",
                "CommandResult",
                "DatabaseCapabilityStatus",
                "DatabaseOperationFailure",
                "DatabaseOperationState",
                "DatabaseOperationStatus",
                "DatabaseOperationType",
                "ICommandRunner",
                "IDatabaseManagementProvider",
                "IDatabaseManagementService",
                "IStagedBackup",
                "UnreadableCredentials"),
            .. In("DemoData", "Lighthouse.Backend.Services.Implementation",
                "DemoDataService"),
            .. In("DemoData", "Lighthouse.Backend.Services.Interfaces",
                "IDemoDataService"),
            .. In("Seeding", "Lighthouse.Backend.Services.Implementation.Seeding",
                "InstallTimestampSeeder"),
            .. In("Seeding", "Lighthouse.Backend.Services.Interfaces.Seeding",
                "ISeeder"),
            .. In("Persistence", "Lighthouse.Backend.Services.Implementation",
                "LostRaceTolerantSave"),
            .. In("UsageData", "Lighthouse.Backend.Services.Implementation.UsageData",
                "PostHogUsageDataPublisher",
                "UsageDataConsentService",
                "UsageDataConsentToken",
                "UsageDataDeploymentModeResolver",
                "UsageDataEventQueue",
                "UsageDataGate",
                "UsageDataInstanceProperties",
                "UsageDataMasterSwitch",
                "UsageDataPublishing"),
            .. In("UsageData", "Lighthouse.Backend.Services.Interfaces.UsageData",
                "AcceptedUsageDataBatch",
                "IUsageDataConsentService",
                "IUsageDataDeploymentModeResolver",
                "IUsageDataEventQueue",
                "IUsageDataGate",
                "IUsageDataInstanceProperties",
                "IUsageDataMasterSwitch",
                "IUsageDataPublisher",
                "UsageDataDeploymentMode",
                "UsageDataInstanceFacts"),
            .. In("UsageData", "Lighthouse.Backend.Services.Implementation.BackgroundServices",
                "UsageDataConsentPruningService",
                "UsageDataForwardingService"),
            .. In("Logging", "Lighthouse.Backend.Services.Implementation.Logging",
                "RecentProblemsReport",
                "RecentProblemsSink"),
            .. In("Logging", "Lighthouse.Backend.Services.Implementation",
                "ConfigFileUpdater",
                "SerilogLogConfiguration"),
            .. In("Logging", "Lighthouse.Backend.Services.Interfaces",
                "IConfigFileUpdater",
                "ILogConfiguration",
                "IRecentProblems",
                "IRecentProblemsReport"),
            .. In("Releases", "Lighthouse.Backend.Services.Implementation",
                "AssemblyService",
                "GitHubService",
                "LighthouseReleaseService"),
            .. In("Releases", "Lighthouse.Backend.Services.Interfaces",
                "IAssemblyService",
                "IGitHubService",
                "ILighthouseReleaseService"),
            .. In("DomainEvents", "Lighthouse.Backend.Services.Implementation.DomainEvents",
                "DomainEventDispatcher"),
            .. In("DomainEvents", "Lighthouse.Backend.Services.Interfaces.DomainEvents",
                "IDomainEventDispatcher",
                "IDomainEventHandler`1"),
            .. In("Host", "Lighthouse.Backend.Services.Implementation",
                "FileSystemService",
                "LighthouseClock",
                "PlatformExtensions",
                "PlatformService",
                "ProcessService",
                "RandomNumberService",
                "ServiceConfig",
                "SystemInfoService"),
            .. In("Host", "Lighthouse.Backend.Services.Interfaces",
                "IFileSystemService",
                "ILighthouseClock",
                "IPlatformService",
                "IProcessService",
                "IRandomNumberService",
                "IServiceConfig",
                "ISystemInfoService",
                "SupportedPlatform"),
            .. In("UpdateQueue", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update",
                "AdmittedCancellations",
                "AdmittedWorkOrdering",
                "InProcessUpdateCancellationNotifier",
                "InProcessUpdateCompletionNotifier",
                "InProcessUpdateExecutionLock",
                "InProcessUpdateStatusStore",
                "PostgresUpdateExecutionLock",
                "RedisUpdateCancellationNotifier",
                "RedisUpdateCompletionNotifier",
                "RedisUpdateStatusStore",
                "UpdateCancellationContext",
                "UpdateCancellationPolicy",
                "UpdateEntityKind",
                "UpdateEntityKinds",
                "UpdateKey",
                "UpdateMoments",
                "UpdateNotificationHub",
                "UpdateProgress",
                "UpdateQueueService",
                "UpdateStatus",
                "UpdateSubstrate",
                "UpdateTaskNaming"),
            .. In("UpdateQueue", "Lighthouse.Backend.Services.Interfaces.Update",
                "IUpdateCancellationNotifier",
                "IUpdateCompletionNotifier",
                "IUpdateExecutionLock",
                "IUpdateQueueService",
                "IUpdateStatusStore",
                "IUpdateTaskNaming"),
            .. In("UpdateQueue", "Lighthouse.Backend.Services.Implementation",
                "RefreshRoundSummary",
                "RefreshRoundSummaryLine"),
        ];

        public static IReadOnlyList<TypeThatStays> TypesThatStay { get; } =
        [
            .. Stays("Usage data", "Lighthouse.Backend.Platform.UsageData",
                "UsageDataEmitPermit",
                "UsageDataEventReported",
                "UsageDataEventShapes",
                "UsageDataSuppressionReason"),
            .. Stays("Organisation set-up", "Lighthouse.Backend.Services.Implementation",
                "FeaturePositionMap"),
            .. Stays("Organisation set-up", "Lighthouse.Backend.Services.Interfaces",
                "IFeaturePositionMap"),
            .. Stays("Organisation set-up", "Lighthouse.Backend.Services.Implementation.Seeding",
                "AppSettingSeeder",
                "CategoryListNormalisationSeeder",
                "OptionalFeatureSeeder",
                "TerminologySeeder"),
            .. Stays("Access", "Lighthouse.Backend.Services.Implementation.Seeding",
                "ApiKeyOwnerReconciliationSeeder",
                "EveryoneSignedInSeeder"),
            .. Stays("Work Items", "Lighthouse.Backend.Services.Implementation.Seeding",
                "RefreshLogSeeder"),
            .. Stays("Work Items", "Lighthouse.Backend.Services.Implementation",
                "EstimateNormalizationBatchResult",
                "EstimateNormalizationResult",
                "EstimateNormalizer",
                "FeatureListActiveWork",
                "FeatureListOwners",
                "FeatureListService",
                "InProgressFeatureResolution",
                "OrphanedFeatureCleanupService"),
            .. Stays("Work Items", "Lighthouse.Backend.Services.Interfaces",
                "FeatureListRead",
                "IFeatureListService",
                "IOrphanedFeatureCleanupService",
                "TeamAtWork"),
            .. Stays("The update pipelines", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update",
                "PortfolioUpdater",
                "TeamUpdater",
                "UpdateServiceBase`1"),
            .. Stays("The update pipelines", "Lighthouse.Backend.Services.Interfaces.Update",
                "IPortfolioUpdater",
                "ITeamUpdater",
                "IUpdateService"),
            .. Stays("The web host's pipeline", "Lighthouse.Backend.Platform.Host",
                "BreachSafeResponseCompressionProvider",
                "NeverCompressResponseAttribute"),
        ];

        public static IReadOnlyList<NamespaceThatStays> NamespacesThatStay { get; } =
        [
            new("Lighthouse.Backend.Data", "The database context, which the migrations name by this namespace"),
            new("Lighthouse.Backend.Services.Implementation.Repositories", "Each context's persistence adapters"),
            new("Lighthouse.Backend.Services.Interfaces.Repositories", "Each context's persistence ports, IRepository included"),
        ];

        public static IReadOnlyList<string> ModelsCandidates { get; } =
        [
            "Lighthouse.Backend.Models.Encryption.ConnectionSecretSummary",
            "Lighthouse.Backend.Models.Encryption.EncryptionKey",
            "Lighthouse.Backend.Models.Encryption.EncryptionKeyRing",
            "Lighthouse.Backend.Models.Encryption.KeyCustody",
            "Lighthouse.Backend.Models.Encryption.SecretMoveOutcome",
            "Lighthouse.Backend.Models.Encryption.SecretReadResult",
            "Lighthouse.Backend.Models.Encryption.SecretReadabilityReport",
            "Lighthouse.Backend.Models.Encryption.SecretState",
            "Lighthouse.Backend.Models.Encryption.StoredSecretRecord",
            "Lighthouse.Backend.Models.Logging.RecentProblem",
            "Lighthouse.Backend.Models.Logging.RefreshSubject",
        ];

        public static IReadOnlyList<SuppressionTargetToRewrite> SuppressionTargets { get; } =
        [
            new(TheSuppressionsFile, "RandomNumberService.GetRandomNumber(System.Int32)~System.Int32", PlatformMove.DomainEventsAndHost),
            new(TheSuppressionsFile, "PostgresDatabaseManagementProvider", PlatformMove.DatabaseAndSeeding),
            new(TheSuppressionsFile, "LighthouseReleaseService", PlatformMove.UsageDataLoggingAndReleases),
        ];

        public static IReadOnlyList<StringTheMoveRenames> StringsTheMoveRenames { get; } =
        [
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.UsageData.UsageDataInstanceProperties", "Lighthouse.Backend.Platform.UsageData.UsageDataInstanceProperties", PlatformMove.UsageDataLoggingAndReleases),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IPlatformService", "Lighthouse.Backend.Platform.Host.IPlatformService", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.ILighthouseClock", "Lighthouse.Backend.Platform.Host.ILighthouseClock", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey", "Lighthouse.Backend.Platform.UpdateQueue.UpdateKey", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/CalendarDayAnchorSeamArchUnitTest.cs", "Services/Implementation/LighthouseClock.cs", "Platform/Host/LighthouseClock.cs", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/FlowMetricsInboundApplicationArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.DatabaseManagement.DatabaseMaintenanceGate", "Lighthouse.Backend.Platform.DatabaseManagement.DatabaseMaintenanceGate", PlatformMove.DatabaseAndSeeding),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/ForecastingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateCancellationContext", "Lighthouse.Backend.Platform.UpdateQueue.UpdateCancellationContext", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/ForecastingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey", "Lighthouse.Backend.Platform.UpdateQueue.UpdateKey", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.UsageDataConsentPruningService", "Lighthouse.Backend.Platform.UsageData.UsageDataConsentPruningService", PlatformMove.UsageDataLoggingAndReleases),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.UsageData.UsageDataConsentService", "Lighthouse.Backend.Platform.UsageData.UsageDataConsentService", PlatformMove.UsageDataLoggingAndReleases),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.UsageData.UsageDataInstanceProperties", "Lighthouse.Backend.Platform.UsageData.UsageDataInstanceProperties", PlatformMove.UsageDataLoggingAndReleases),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.ILighthouseClock", "Lighthouse.Backend.Platform.Host.ILighthouseClock", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.ILighthouseClock", "Lighthouse.Backend.Platform.Host.ILighthouseClock", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.DomainEvents.IDomainEventHandler`1", "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler`1", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey", "Lighthouse.Backend.Platform.UpdateQueue.UpdateKey", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/SecretCustodySeamArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.CryptoService", "Lighthouse.Backend.Platform.Encryption.CryptoService", PlatformMove.KeyRing),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/SecretCustodySeamArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.Encryption.UnreadableSecretException", "Lighthouse.Backend.Platform.Encryption.UnreadableSecretException", PlatformMove.KeyRing),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/SecretCustodySeamArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.Encryption.", "Lighthouse.Backend.Platform.Encryption.", PlatformMove.KeyRing),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/SecretReadPathSourceStructureTest.cs", "Services/Implementation/Encryption/", "Platform/Encryption/", PlatformMove.KeyRing),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/SecretReadPathSourceStructureTest.cs", "Services/Implementation/DatabaseManagement/DatabaseManagementService.cs", "Platform/DatabaseManagement/DatabaseManagementService.cs", PlatformMove.DatabaseAndSeeding),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/SecretRotationWriteSurfaceTest.cs", "Services/Implementation/Encryption/SecretCustodyService.cs", "Platform/Encryption/SecretCustodyService.cs", PlatformMove.KeyRing),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs", "Lighthouse.Backend.Tests.Services.Implementation.LighthouseClockTest", "Lighthouse.Backend.Tests.Platform.Host.LighthouseClockTest", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.HostUsersStillToMove.cs", "Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.DomainEventDispatcherGoldTest", "Lighthouse.Backend.Tests.Platform.DomainEvents.DomainEventDispatcherGoldTest", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.HostUsersStillToMove.cs", "Lighthouse.Backend.Tests.Services.Implementation.LighthouseClockTest", "Lighthouse.Backend.Tests.Platform.Host.LighthouseClockTest", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.Encryption.ConnectionSecrets", "Lighthouse.Backend.Platform.Encryption.ConnectionSecrets", PlatformMove.KeyRing),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.Encryption.UnreadableSecretException", "Lighthouse.Backend.Platform.Encryption.UnreadableSecretException", PlatformMove.KeyRing),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey", "Lighthouse.Backend.Platform.UpdateQueue.UpdateKey", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/UpdateCancellationContextWriterArchUnitTest.cs", "Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs", "Platform/UpdateQueue/UpdateQueueService.cs", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/UpdateCancellationContextWriterArchUnitTest.cs", "Services/Implementation/BackgroundServices/Update/UpdateCancellationContext.cs", "Platform/UpdateQueue/UpdateCancellationContext.cs", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.DomainEvents.IDomainEventHandler`1", "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler`1", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateNotificationHub", "Lighthouse.Backend.Platform.UpdateQueue.UpdateNotificationHub", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateCancellationContext", "Lighthouse.Backend.Platform.UpdateQueue.UpdateCancellationContext", PlatformMove.UpdateQueue),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WorkItemsModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.ILighthouseClock", "Lighthouse.Backend.Platform.Host.ILighthouseClock", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WorkItemsPoliciesLayoutArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.DomainEvents.IDomainEventHandler`1", "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler`1", PlatformMove.DomainEventsAndHost),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Logging/RecentProblemsSinkTest.cs", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateQueueService", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService", PlatformMove.UpdateQueue),
            new("Scripts/test-timings/test-time-budget.json", "Lighthouse.Backend.Tests.Services.Implementation.Encryption.SecretCustodyServiceTests", "Lighthouse.Backend.Tests.Platform.Encryption.SecretCustodyServiceTests", PlatformMove.KeyRing),
            new("Scripts/test-timings/tests/test_check_budget_time_budgets.py", "Services.Implementation.Encryption.SecretCustodyServiceTests", "Platform.Encryption.SecretCustodyServiceTests", PlatformMove.KeyRing),
            new("Scripts/test-timings/test-time-budget.json", "Lighthouse.Backend.Tests.Services.Implementation.Encryption.PublishedKeySecretCountTests", "Lighthouse.Backend.Tests.Platform.Encryption.PublishedKeySecretCountTests", PlatformMove.KeyRing),
            new("Scripts/test-timings/tests/test_check_budget_time_budgets.py", "Services.Implementation.Encryption.PublishedKeySecretCountTests", "Platform.Encryption.PublishedKeySecretCountTests", PlatformMove.KeyRing),
            new("Scripts/test-timings/test-time-budget.json", "Lighthouse.Backend.Tests.Services.Implementation.Encryption.ReferencedKeyIdsTests", "Lighthouse.Backend.Tests.Platform.Encryption.ReferencedKeyIdsTests", PlatformMove.KeyRing),
            new("Scripts/test-timings/tests/test_check_budget_time_budgets.py", "Services.Implementation.Encryption.ReferencedKeyIdsTests", "Platform.Encryption.ReferencedKeyIdsTests", PlatformMove.KeyRing),
            new("Scripts/test-timings/test-time-budget.json", "Lighthouse.Backend.Tests.Services.Implementation.Encryption.ReadableSecretsNotOnTheActiveKeyTests", "Lighthouse.Backend.Tests.Platform.Encryption.ReadableSecretsNotOnTheActiveKeyTests", PlatformMove.KeyRing),
            new("Scripts/test-timings/tests/test_check_budget_time_budgets.py", "Services.Implementation.Encryption.ReadableSecretsNotOnTheActiveKeyTests", "Platform.Encryption.ReadableSecretsNotOnTheActiveKeyTests", PlatformMove.KeyRing),
        ];
        private static IEnumerable<PlatformLaneType> In(string area, string oldNamespace, params string[] names)
            => names.Select(name => new PlatformLaneType(name, area, oldNamespace));

        private static IEnumerable<TypeThatStays> Stays(string owner, string inNamespace, params string[] names)
            => names.Select(name => new TypeThatStays(name, inNamespace, owner));
    }
}
