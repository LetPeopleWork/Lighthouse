namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>Where a Models type stands: in the shared kernel, or owned by a context or the platform lane.</summary>
    internal enum KernelStanding
    {
        Kernel,
        Owned,
    }

    /// <summary>
    /// Why an owned type still sits in the Models. Each one is measured from the compiled code or the database model, never
    /// read off a name.
    /// </summary>
    internal enum KernelBlocker
    {
        /// <summary>The database model stores it, and both migration snapshots name it.</summary>
        Entity,

        /// <summary>A controller action or a public property of an API DTO reaches it, directly or through other types.</summary>
        Wire,

        /// <summary>A Models type that stays references it, and the Models may not reach into a module.</summary>
        HeldBy,

        /// <summary>Types of two or more contexts use it.</summary>
        Shared,

        /// <summary>The data layer, a repository or the composition root names it.</summary>
        InfraReached,

        /// <summary>A file outside the backend tests reads its source file by path.</summary>
        PathPinned,

        /// <summary>It is a domain event, which lives with the context that publishes it.</summary>
        Event,

        /// <summary>Its file holds a type that stays or moves to another owner; a file moves whole, and no file is split.</summary>
        SharesAFile,
    }

    /// <summary>
    /// One top-level Models type, named after the Models namespace, with its standing, its owner, every blocker that keeps
    /// it in the Models, and the Story expected to free it. An owned row with no blocker is a free type: it is on the
    /// movers list until it has moved.
    /// </summary>
    internal sealed record CatalogueRow(string Type, KernelStanding Standing, string Owner, IReadOnlyList<KernelBlocker> Blockers, string FreedBy)
    {
        public const string TheKernel = "the shared kernel";

        public const string ThisStory = "this Story moves it";

        public static CatalogueRow Kernel(string type) => new(type, KernelStanding.Kernel, TheKernel, [], "stays");

        public static CatalogueRow Owned(string type, string owner, string freedBy, params KernelBlocker[] blockers)
            => new(type, KernelStanding.Owned, owner, blockers, freedBy);

        public static CatalogueRow Free(string type, string owner) => new(type, KernelStanding.Owned, owner, [], ThisStory);
    }

    /// <summary>
    /// The catalogue of the shared kernel: one row for every top-level type in the Models namespaces, and the free types
    /// that move to their owner, each with its Models namespace and the namespace it moves to. Both lists only shrink.
    /// </summary>
    internal static class KernelCatalogue
    {
        // A property, not a constant: a rule check reads every string constant of the test assembly as a rule pattern.
        public static string TheModelsRoot => "Lighthouse.Backend.Models";

        private const string Entity = "S12c or S13 move the entities";

        private const string Wire = "S12c makes the wire types pure mapping";

        private const string Shared = "S12c puts it in its owner's contract";

        private const string Infra = "S05 or S10 move the data layer and the composition root";

        private const string Pinned = "the consumer that reads its path changes first";

        private const string Held = "it moves with the Models type that holds it";

        private const string MovesWithItsFile = "it moves with its file, once every type in the file is free";

        /// <summary>
        /// The only types the shared kernel may hold today: the domain-event base type and the two entity markers. Lighthouse has
        /// no shared id or shared value object; a type joins this list only when it is one.
        /// </summary>
        public static IReadOnlyList<string> TheKernelTypes { get; } = ["Events.IDomainEvent", "IEntity", "IConcurrencyTokenEntity"];

        public static IReadOnlyList<CatalogueRow> Rows { get; } =
        [
            CatalogueRow.Kernel("Events.IDomainEvent"),
            CatalogueRow.Kernel("IEntity"),
            CatalogueRow.Kernel("IConcurrencyTokenEntity"),
            CatalogueRow.Owned("AdditionalFieldDefinition", "TrackerIntegration", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("AppSetting", "OrganisationSetup", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("AppSettings.RefreshSettings", "OrganisationSetup", Wire, KernelBlocker.Wire, KernelBlocker.Shared),
            CatalogueRow.Owned("AppSettings.SurveyNudgeAction", "OrganisationSetup", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Auth.ApiKey", "Access", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Auth.ApiKeyCreationResult", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Auth.ApiKeyInfo", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Auth.ApiKeyScopeDto", "Access", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Auth.AuthMode", "Access", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("Auth.AuthSessionStatus", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Auth.AuthenticationConfiguration", "Access", Infra, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Auth.AuthorizationConfiguration", "Access", Infra, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Auth.CreateApiKeyRequest", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Auth.EmbedSessionToken", "Access", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Auth.RuntimeAuthStatus", "Access", Wire, KernelBlocker.Wire, KernelBlocker.Shared),
            CatalogueRow.Owned("Auth.UserProfile", "Access", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Authorization.ApiKeyPermission", "Access", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Authorization.AppliesTo", "Access", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Authorization.EveryoneSignedInRequest", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.EveryoneSignedInSetting", "Access", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Authorization.PermissionRule", "Access", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Authorization.PermissionScopeType", "Access", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Authorization.RbacGroupMapping", "Access", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Authorization.RbacGroupMappingRequest", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.RbacGroupMappingRoleUpdateRequest", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.RbacGroupMappingSummary", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.RbacScopedMemberSummary", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.RbacStatus", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.RbacUserSummary", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.ScopedMemberRoleRequest", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.SystemWideRule", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.UserAuthorizationSummary", "Access", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Authorization.UserPermission", "Access", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Authorization.UserRole", "Access", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("BlackoutDaysExtensions", "OrganisationSetup", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("BlackoutPeriod", "OrganisationSetup", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("BlackoutPeriodDto", "OrganisationSetup", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("BlockedCountSnapshot", "FlowMetrics", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("ConnectionDefaultSetting", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("ConnectionHealth.ConnectionHealthDto", "TrackerIntegration", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("ConnectionHealth.ConnectionHealthState", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("ConnectionHealth.ConnectionHealthVerdict", "TrackerIntegration", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("CycleTimeDefinition", "FlowMetrics", Infra, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Delivery", "Promises", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("DeliveryArchivedException", "Promises", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("DeliveryClosureRecord", "Promises", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("DeliveryFeatureMetric", "Promises", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("DeliveryMetricSnapshot", "Promises", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("DeliveryMetricsProjection", "Promises", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("DeliveryNote", "Promises", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("DeliverySelectionMode", "Promises", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("DeliverySourceBoundException", "Promises", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("DeliverySources.DeliveryForecastPublication", "TrackerIntegration", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("DeliverySources.DeliveryForecastPublishResult", "TrackerIntegration", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("DeliverySources.DeliverySourceDescriptor", "TrackerIntegration", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("DeliverySources.DeliverySourceResolution", "TrackerIntegration", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("DeliverySources.DeliverySourceSnapshot", "TrackerIntegration", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("DeliverySources.DeliverySourceSweepPurpose", "TrackerIntegration", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("DeliverySources.DeliverySourceUnavailableReason", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("DeliverySources.SourceOptionBlockReason", "TrackerIntegration", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("DeliveryWhenPercentile", "Promises", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("DemoData.DemoDataScenario", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Dependencies.DependencySource", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Dependencies.DependencyVerdict", "WorkItems", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Dependencies.ForecastWaits", "WorkItems", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("Dependencies.HonouredDependencies", "WorkItems", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Dependencies.NotHonouredReason", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Distribution.DistributionInfo", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("EffectiveSetting", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Encryption.ConnectionSecretSummary", "Platform", Held, KernelBlocker.HeldBy, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("Encryption.EncryptionKey", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Encryption.EncryptionKeyRing", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Encryption.KeyCustody", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Encryption.SecretMoveOutcome", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Encryption.SecretReadResult", "Platform", Infra, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Encryption.SecretReadabilityReport", "Platform", MovesWithItsFile, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("Encryption.SecretState", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Encryption.StoredSecretRecord", "Platform", Held, KernelBlocker.HeldBy, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("EstimationBundle", "OrganisationSetup", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Feature", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("FeatureBlockedTransition", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("FeatureComparer", "OrganisationSetup", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("FeatureDependencyReference", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("FeatureOrderingPolicy", "OrganisationSetup", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("FeatureStateTransition", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("FeatureWork", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Forecast.AggregatedWhenForecast", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.CellOutcome", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.ComonotonicCompletionDistribution", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.CompletionHistogram", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.CurrentSettingStanding", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.DeliveryCompletionForecast", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.DeliveryForecastRow", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.Determination", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.FeatureCandidateDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.FeatureForecastWindow", "Forecasting", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("Forecast.FeatureStart", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.ForecastBase", "Forecasting", Entity, KernelBlocker.Entity, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Forecast.ForecastInputCandidatesDto", "Forecasting", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Forecast.ForecastPercentile", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.ForecastPredictabilityScore", "Forecasting", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Forecast.ForecastSimulationLimits", "Forecasting", Infra, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Forecast.HowManyForecast", "Forecasting", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("Forecast.IndividualSimulationResult", "Forecasting", Entity, KernelBlocker.Entity, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Forecast.JointCompletionDistribution", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.LevelReading", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.NotTestedReason", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckCellDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckDenominatorDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckForecastDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckLevelCoverageDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckLevelOutcomeDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckResultDto", "Forecasting", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Forecast.RealityCheckScoredPeriodDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckSoundWindowDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.RealityCheckSufficiencyDto", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.StartDateSource", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.StartForecast", "Forecasting", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Forecast.SufficiencyReason", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Forecast.WhenForecast", "Forecasting", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("IWorkItemQueryOwner", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("IWorkTrackingSystemOptionsOwner", "OrganisationSetup", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("IncomingSetting", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("InstanceCalendar", "OrganisationSetup", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("LegacyListShape", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("LicenseInformation", "Licensing", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("LighthouseRelease", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("LighthouseReleaseAsset", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("ListEntryKind", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Logging.RecentProblem", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Logging.RefreshSubject", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("MetricType", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Metrics.AgeInStatePercentilesDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.ArrivalsInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.BaselineStatus", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.CumulativeStateTimeCandidateRowDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.CumulativeStateTimeCandidatesDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.CumulativeStateTimeDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.CumulativeStateTimeItemDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.CumulativeStateTimeItemsDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.CumulativeStateTimeStateRowDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.CycleTimePercentilesInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.EstimationVsCycleTimeDataPoint", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.EstimationVsCycleTimeDiagnostics", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.EstimationVsCycleTimeResponse", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.EstimationVsCycleTimeStatus", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.FeatureEstimationDataPoint", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.FeatureSizeEstimationResponse", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.FeatureSizePercentilesInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.FeaturesWorkedOnInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.FlowEfficiencyInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.InfoWidgetComparisonDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.NamedCycleTimeValue", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.PercentileValue", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("Metrics.PercentileValueDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.PredictabilityScoreInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.ProcessBehaviourChart", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.ProcessBehaviourChartDataPoint", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.RunChartData", "FlowMetrics", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("Metrics.SleRiskDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.Shared),
            CatalogueRow.Owned("Metrics.SpecialCauseType", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.ThroughputFilterMode", "FlowMetrics", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("Metrics.ThroughputInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.TotalWorkItemAgeInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.TrendDetailRowDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.WipOverviewInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.WithinSleDayDto", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.WithinSleInfoDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.WithinSleOverTimeDto", "FlowMetrics", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Metrics.WithinSleVerdict", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Metrics.XAxisKind", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("NoteSignature", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("OAuth.OAuthCompleteResult", "TrackerIntegration", Pinned, KernelBlocker.PathPinned),
            CatalogueRow.Owned("OAuth.OAuthCredential", "TrackerIntegration", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.Shared, KernelBlocker.InfraReached, KernelBlocker.PathPinned),
            CatalogueRow.Owned("OAuth.OAuthCredentialStatus", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.PathPinned),
            CatalogueRow.Owned("OAuth.WorkItemRights", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.PathPinned),
            CatalogueRow.Owned("OptionalFeatures.OptionalFeature", "OrganisationSetup", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("OwnerType", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("ParentCandidate", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("ParentChoice", "WorkItems", Shared, KernelBlocker.Shared, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentNarrowing", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("ParentNotPlacedReason", "WorkItems", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("ParentPlacement", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("ParentPlacementOutcome", "WorkItems", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("ParentPlacementRule", "WorkItems", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("ParentReading", "TrackerIntegration", Pinned, KernelBlocker.HeldBy, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentReadingAt", "TrackerIntegration", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentReadingKind", "TrackerIntegration", Pinned, KernelBlocker.HeldBy, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentReadings", "TrackerIntegration", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentResolution", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentSourceEntry", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentSourceKind", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentSources", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentSourcesOutcome", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.PathPinned),
            CatalogueRow.Owned("ParentSourcesViolation", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.PathPinned),
            CatalogueRow.Owned("PercentilesOverTimeSnapshot", "FlowMetrics", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Portfolio", "OrganisationSetup", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("ProcessBehaviorMetricType", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("ProcessBehaviorSnapshot", "FlowMetrics", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("RecurringBlackoutRule", "OrganisationSetup", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("RecurringBlackoutRuleDto", "OrganisationSetup", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Refinement.DiscussionRules", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.NeedUnavailableReason", "Refinement", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Refinement.ReadinessSetting", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.RefinementBand", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.RefinementCadence", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.RefinementSettings", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Refinement.RefinementStage", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.RefinementStateSetting", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.RefinementVerdict", "Refinement", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Refinement.RowReadiness", "Refinement", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Refinement.SizingAnswer", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.SizingChannel", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.SizingEntryKind", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.SizingLogEntry", "Refinement", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Refinement.StageRules", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Refinement.YardstickSource", "Refinement", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("RefreshFinding", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("RefreshFindingKind", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("RefreshFindingSetting", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("RefreshLog", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("RefreshType", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("RemoteRecordStamp", "TrackerIntegration", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("Reports.CapturedValue", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.MetricRead", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.NewReport", "Reports", MovesWithItsFile, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("Reports.NewThenAndNow", "Reports", Held, KernelBlocker.HeldBy, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("Reports.Report", "Reports", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Reports.ReportBulkCreation", "Reports", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Reports.ReportBulkRemoval", "Reports", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Reports.ReportBulkRun", "Reports", MovesWithItsFile, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("Reports.ReportCreationOptions", "Reports", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Reports.ReportCreationOwner", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportCreationStatus", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportCreationTemplate", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportList", "Reports", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Reports.ReportNote", "Reports", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Reports.ReportOutcomeStatus", "Reports", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportOutcome`1", "Reports", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportOwner", "Reports", Infra, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Reports.ReportOwnerKind", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("Reports.ReportOwnerOutcome", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportRead", "Reports", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Reports.ReportRemovalOutcome", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportRemovalStatus", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportSummary", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportTemplateMetricRead", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportTemplateRead", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportTemplatesRead", "Reports", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Reports.ReportWindow", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ReportsAcrossOwners", "Reports", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Reports.ThenAndNowPayload", "Reports", MovesWithItsFile, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("Reports.ThenAndNowRead", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Reports.ThenAndNowSummary", "Reports", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("SettingSource", "OrganisationSetup", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("SimulationResult", "Forecasting", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("Staleness.StaleReasonKind", "WorkItems", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("StateCategories", "OrganisationSetup", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("StateCategoryLists", "OrganisationSetup", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("StateMapping", "WorkItems", Infra, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("StoredEstimation", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("StoredFeatures", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("StoredSetting", "OrganisationSetup", Held, KernelBlocker.HeldBy),
            CatalogueRow.Owned("SupportedConnectionDefaults", "OrganisationSetup", MovesWithItsFile, KernelBlocker.SharesAFile),
            CatalogueRow.Owned("SyncMode", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("SystemInfo", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Team", "OrganisationSetup", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("TerminologyEntry", "OrganisationSetup", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("ThroughputSettings", "OrganisationSetup", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("UpdateType", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("UsageData.UsageDataConsent", "Platform", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("UsageData.UsageDataDecision", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("UsageData.UsageDataEventName", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataFeaturesHeld", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataOptionalFeature", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataRefinementVerdict", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataReportTemplate", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataRouteKey", "Platform", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.PathPinned),
            CatalogueRow.Owned("UsageData.UsageDataRoutePatterns", "Platform", Pinned, KernelBlocker.PathPinned),
            CatalogueRow.Owned("UsageData.UsageDataSizingMoment", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataSource", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataState", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataUpdates", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("UsageData.UsageDataWorkTrackingSystem", "Platform", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("Validation.ConnectionValidationResult", "TrackerIntegration", Wire, KernelBlocker.Wire),
            CatalogueRow.Owned("WithinSleSnapshot", "FlowMetrics", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.InfraReached),
            CatalogueRow.Owned("WithinSleSnapshotOrigin", "FlowMetrics", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("WorkItem", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("WorkItemBase", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached, KernelBlocker.PathPinned),
            CatalogueRow.Owned("WorkItemBlockedTransition", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("WorkItemRules.AdditionalFieldRuleHealing", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WorkItemRules.RuleOperators", "WorkItems", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WorkItemRules.WorkItemRuleCondition", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WorkItemRules.WorkItemRuleFieldDefinition", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WorkItemRules.WorkItemRuleSchema", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WorkItemRules.WorkItemRuleSet", "WorkItems", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WorkItemRules.WorkItemRuleSetJson", "WorkItems", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("WorkItemStateTransition", "WorkItems", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached),
            CatalogueRow.Owned("WorkTrackingSystemConnection", "TrackerIntegration", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached, KernelBlocker.PathPinned),
            CatalogueRow.Owned("WorkTrackingSystemConnectionOption", "TrackerIntegration", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached, KernelBlocker.PathPinned),
            CatalogueRow.Owned("WorkTrackingSystemOptionsOwner", "OrganisationSetup", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.InfraReached, KernelBlocker.PathPinned),
            CatalogueRow.Owned("WorkTrackingSystems", "TrackerIntegration", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.Shared, KernelBlocker.PathPinned),
            CatalogueRow.Owned("WriteBack.NotificationSuppression", "Forecasting", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WriteBack.WriteBackAppliesTo", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("WriteBack.WriteBackFieldUpdate", "Forecasting", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WriteBack.WriteBackItemResult", "Forecasting", Shared, KernelBlocker.HeldBy, KernelBlocker.Shared),
            CatalogueRow.Owned("WriteBack.WriteBackMappingDefinition", "Forecasting", Entity, KernelBlocker.Entity, KernelBlocker.Wire, KernelBlocker.HeldBy, KernelBlocker.InfraReached),
            CatalogueRow.Owned("WriteBack.WriteBackResult", "Forecasting", Shared, KernelBlocker.Shared),
            CatalogueRow.Owned("WriteBack.WriteBackTargetValueType", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
            CatalogueRow.Owned("WriteBack.WriteBackValueSource", "Forecasting", Wire, KernelBlocker.Wire, KernelBlocker.HeldBy),
        ];

        /// <summary>
        /// The owner the design names for a Models area or type, read only for a free type no context uses, whose owner the
        /// compiled code cannot show. The platform lane's areas move to Platform/Area.
        /// </summary>
        public static IReadOnlyDictionary<string, string> OwnersByDesign { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppSettings"] = "OrganisationSetup",
            ["Auth"] = "Access",
            ["Authorization"] = "Access",
            ["Encryption"] = "Platform",
            ["Forecast"] = "Forecasting",
            ["Logging"] = "Platform",
            ["Metrics"] = "FlowMetrics",
            ["OptionalFeatures"] = "OrganisationSetup",
            ["PortfolioSize"] = "OrganisationSetup",
            ["Reports"] = "Reports",
            ["Staleness"] = "WorkItems",
            ["SupportedConnectionDefaults"] = "OrganisationSetup",
            ["UsageData"] = "Platform",
            ["WorkItemRules"] = "WorkItems",
        };

        public static IReadOnlyList<MovingType> Movers { get; } = [];

        public static string FreedByFor(IReadOnlyCollection<KernelBlocker> blockers)
            => blockers.Contains(KernelBlocker.Entity) ? Entity
                : blockers.Contains(KernelBlocker.Wire) ? Wire
                : blockers.Contains(KernelBlocker.Shared) ? Shared
                : blockers.Contains(KernelBlocker.InfraReached) ? Infra
                : blockers.Contains(KernelBlocker.PathPinned) ? Pinned
                : blockers.Contains(KernelBlocker.HeldBy) ? Held
                : blockers.Contains(KernelBlocker.SharesAFile) ? MovesWithItsFile
                : CatalogueRow.ThisStory;
    }
}
