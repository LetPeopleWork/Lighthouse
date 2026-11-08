using Lighthouse.Backend.Tests.Contracts;
using Lighthouse.Backend.Tests.TestHelpers;
using Mono.Cecil;
using static Lighthouse.Backend.Tests.Architecture.TestHostScanning;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A test may start an application host only for a reason: as a skeleton of a user flow, or as a wire contract
    /// on <c>WireContractTest</c>. Everything else drives the application service through its port. This guard reads
    /// the compiled test assembly and judges every concrete class with tests, nested ones included, by what it runs
    /// in its tests, set-ups, tear-downs and constructors, on the class or on a base class. A type sits on at most one
    /// of the two lists of reasons, and only while it still starts a host; the contracts namespace and the namespaces
    /// below it hold the fixtures on the base and nothing else.
    /// </summary>
    [TestFixture]
    public class TestHostUseGuardTest
    {
        // This class and the classes it nests are exempt. It tests the contract base itself rather than pinning a wire
        // contract, so it lives beside the other test helpers, and its nested fixtures derive from the base outside the
        // contracts namespace for that reason.
        private static readonly string TheContractBasesOwnTest = typeof(WireContractTestTest).FullName!;

        private static readonly string TheContractBase = typeof(WireContractTest).FullName!;

        private static readonly string TheContractsNamespace = typeof(WireContractTest).Namespace!;

        private static readonly string Planted = KeyOf(typeof(PlantedFixtures));

        private static readonly string PlantedInContracts = KeyOf(typeof(PlantedContractFixtures));

        private static readonly string PlantedBelowContracts = KeyOf(typeof(Contracts.Planted.PlantedContractFixturesBelowTheNamespace));

        private static readonly string PlantedBesideContracts = KeyOf(typeof(ContractsLookalike.PlantedFixturesInALookalikeNamespace));

        private static readonly string[] PlantedOuterTypes = [Planted, PlantedInContracts, PlantedBelowContracts, PlantedBesideContracts];

        // Every class in this assembly that started a host when this guard was first switched on. They predate the rule
        // that a host needs a reason, so they carry one reason between them: each is still to be moved to the
        // application-service port, made a wire contract, or listed as a skeleton. The list only shrinks: a type that no
        // longer starts a host must leave it, and a new host user is fixed, not added. Classes that test the host itself
        // stay here for good.
        private static readonly HashSet<string> HostUsersStillToMove = new(StringComparer.Ordinal)
        {
            "Lighthouse.Backend.Tests.API.ForecastControllerCalendarDayAnchorTest",
            "Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesPortfolioReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ApiKeyControllerHttpSmokeTests",
            "Lighthouse.Backend.Tests.API.Integration.ApiVersioningRoutingTests",
            "Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryReadStabilityIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryStaleAggregateRaceIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryWriteRefusalIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.BehaviourSettings.Slice01PremiumRefusalTest",
            "Lighthouse.Backend.Tests.API.Integration.BehaviourSettings.Slice02OneListOfSwitchesTest",
            "Lighthouse.Backend.Tests.API.Integration.BehaviourSettings.Story6084HistoryFillAlwaysOnTest",
            "Lighthouse.Backend.Tests.API.Integration.BlackoutForecastShiftDeliveryIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.BlackoutForecastShiftItemPredictionIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.BlackoutForecastShiftTeamForecastIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.BlackoutPeriodsControllerAuthorizationTests",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedStalenessThresholdValidationTests",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice01DemoBackfillTeamHistoryTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice01RuleBasedBlockedTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice02BlockedDurationTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice02FeatureBlockedCaptureTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice03BlockedTrendTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice03HistoricPortfolioBlockedCountTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice04BlockedStalenessTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice04PortfolioBlockedDrillThroughTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice05DemoPortfolioBlockedHistoryTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice05PredefinedFieldTest",
            "Lighthouse.Backend.Tests.API.Integration.BlockedItems.Slice08BlockedDrilldownTest",
            "Lighthouse.Backend.Tests.API.Integration.CategoryListsHoldRawStatesSettingsIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.ClockIsolationScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice01JiraDependenciesFromANamedLinkScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice02NamedLinkThatMatchedNothingScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice03AzureDevOpsRelationTypeAcceptedScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice05aTeamsParentOverrideDefaultScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice05bReferencedFieldsAndOlderPayloadsScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice06DefaultChangeUpdatesTheFollowersScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice08PortfoliosParentOverrideDefaultScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice09PortfoliosDependencyFieldDefaultScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice10PortfoliosSizeEstimateFieldDefaultScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice11PortfoliosFeatureOwnerFieldDefaultScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice12EstimationDefaultScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange.Slice01FieldChangeUpdatesItsOwnersTest",
            "Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange.Slice01SavesThatStartNothingTest",
            "Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange.Slice02ConnectionsSayWhoUsesThemTest",
            "Lighthouse.Backend.Tests.API.Integration.ConnectionSecretStatePayloadTests",
            "Lighthouse.Backend.Tests.API.Integration.ConnectionValidationUnreadableSecretTests",
            "Lighthouse.Backend.Tests.API.Integration.CumulativeStateTimePortfolioReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.CumulativeStateTimeReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionOverAMappingIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionSettingsIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionValidityIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliveriesControllerIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliveryArchiveClosurePinIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliveryFeatureSelectionScopeIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliveryJointForecastIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliveryMetricSnapshotCascadeDeleteIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliveryMetricsHistoryReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliverySources.Slice01aDeliverySourcePreviewTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliverySources.Slice01bDeliveryBindingTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliverySources.Slice02SourceSyncTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliverySources.Slice03BrokenSourceTest",
            "Lighthouse.Backend.Tests.API.Integration.DeliverySources.Slice04ForecastPublishingTest",
            "Lighthouse.Backend.Tests.API.Integration.Dependencies.Slice01DependencyCountTest",
            "Lighthouse.Backend.Tests.API.Integration.Dependencies.Slice02DependencyDetailTest",
            "Lighthouse.Backend.Tests.API.Integration.Dependencies.Slice03ConnectorParityScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Dependencies.Slice04DependencySettingsTest",
            "Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting.Slice00OneForecastPerBatchScenarios",
            "Lighthouse.Backend.Tests.API.Integration.EncryptionControllerTests",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice01ForecastRoundsRunOnTheirOwnThreadsScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice01WalkingSkeletonScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice02OverviewReadsNamesAndCountsScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice03FeaturesListReadsScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice04OneListPerOwnerScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice05LighterRoundsScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice06PortfolioSizeScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice06SizeWarningInTheLogScenarios",
            "Lighthouse.Backend.Tests.API.Integration.FasterUpdates.Slice01UpdateLogSignalTest",
            "Lighthouse.Backend.Tests.API.Integration.FasterUpdates.Slice02JiraCloudTeamDeltaTest",
            "Lighthouse.Backend.Tests.API.Integration.FasterUpdates.Slice03JiraCloudPortfolioDeltaTest",
            "Lighthouse.Backend.Tests.API.Integration.FasterUpdates.Slice04JiraDataCenterDeltaTest",
            "Lighthouse.Backend.Tests.API.Integration.FasterUpdates.Slice05FetchFingerprintTest",
            "Lighthouse.Backend.Tests.API.Integration.FasterUpdates.Story5913AlwaysFasterUpdatesTest",
            "Lighthouse.Backend.Tests.API.Integration.FeatureMoveAuthOffIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.FlowEfficiencyPortfolioReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.FlowEfficiencyReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ForecastControllerAuthorizationTests",
            "Lighthouse.Backend.Tests.API.Integration.ForecastFilterTeamSettingsIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ForecastFilterThroughputChartIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck.Slice06AnOperatorSeesEveryRealityCheckTest",
            "Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates.Slice01SampledStartTest",
            "Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates.Slice01StartForecastTest",
            "Lighthouse.Backend.Tests.API.Integration.ForwardedHeadersIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.HealthCheckIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.InstanceDayForecastWindowIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.JwtBearerAuthIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.JwtBearerStandaloneGateTest",
            "Lighthouse.Backend.Tests.API.Integration.ManualSorting.Slice01FeaturesViewTest",
            "Lighthouse.Backend.Tests.API.Integration.ManualSorting.Slice02ManualRankTest",
            "Lighthouse.Backend.Tests.API.Integration.ManualSorting.Slice03RelativeMovesTest",
            "Lighthouse.Backend.Tests.API.Integration.McpInboundAuthIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.MetricsEndpointIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.MultiTeamJointForecastDeliveryIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeCumulativeScopeIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.NamedCycleTimePortfolioIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeTrendInfoApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.OAuthCallbackCsrfIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.OAuthControllerIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.OAuthProviderAbstractionIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates.Slice01NarrowToFeaturesTest",
            "Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates.Slice02OldestLinkWinsTest",
            "Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates.Slice03PortfolioOldestLinkTest",
            "Lighthouse.Backend.Tests.API.Integration.OrderedParentSources.AzureDevOpsParentSourcesScenarios",
            "Lighthouse.Backend.Tests.API.Integration.OrderedParentSources.ConnectionParentSourcesScenarios",
            "Lighthouse.Backend.Tests.API.Integration.OrderedParentSources.ParentSourcesRefreshScenarios",
            "Lighthouse.Backend.Tests.API.Integration.OrderedParentSources.ParentSourcesSettingsApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks.Slice03AmbiguityRefusalTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice01PercentilesOverTimeTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice02WorkItemAgePercentilesTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice03ThroughputProcessBehaviorTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice03bDateRangeTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice04ProcessBehaviorMetricTypesTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice05ReconstructCycleTimeHistoryTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice06EveryPercentileTabSpansTheSamePeriodTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice07WhereTheLimitsActuallyMovedTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice08NothingToShowTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice09TheFillShipsOptInTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice10OnlyWhatTheTeamStillKeepsTest",
            "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Story6084TheFillRunsEverywhereTest",
            "Lighthouse.Backend.Tests.API.Integration.PortfolioConcurrencyTokenIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.PortfolioDeleteSerialisationTests",
            "Lighthouse.Backend.Tests.API.Integration.PortfolioOwningTeamSaveIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.PortfolioStalenessThresholdSettingsIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.PortfolioTimeInStateReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ProjectsControllerAuthorizationTests",
            "Lighthouse.Backend.Tests.API.Integration.QuietWriteBack.Slice01WriteBackCollectionTest",
            "Lighthouse.Backend.Tests.API.Integration.QuietWriteBack.Slice04QuietWriteBackTest",
            "Lighthouse.Backend.Tests.API.Integration.RbacExceptionEndpointsAuthorizationTests",
            "Lighthouse.Backend.Tests.API.Integration.RbacGroupMappingConcurrencyTokenIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesAuthorizationTests",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesByDateForecastIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesChartOverlayIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesDeliveryIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesDownstreamParityIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesIntervalRuleIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesLifecycleIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesWeekendsForeverIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesWriteBackIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.NeedOverOneCycleTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice01RefinementStatesTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice02RefinementListTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice03StageRulesTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice04RefinementCadenceTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice05NeedAndVerdictTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice07BandPercentilesTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice09ClientNeedTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice09ClientNeedWithAnApiKeyTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice10SleYardstickTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice11CastAVoteTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice12CommentsTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice13ReadinessTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice15VotesWithAnAccountTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice15VotesWithoutRolesTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice16TakeBackTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice16TakeBackWithAnAccountTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice17ClientVotesTest",
            "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice17ClientVotesWithAnApiKeyTest",
            "Lighthouse.Backend.Tests.API.Integration.Reports.BulkReportsWithRolesNotEnforcedScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.BulkReportsWithoutSignInScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.CreateForSeveralTeamsScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.EveryReportInOnePlaceScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.PortfoliosJoinTheSelectionScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.RemoveReportsInBulkScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.ReportCreationTimingProbe",
            "Lighthouse.Backend.Tests.API.Integration.Reports.ReportWindowBoundaryScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.ReportsWithoutSignInScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice01TeamReportScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice02ThenInThePastScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice04ThroughputAndShownMetricsScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice05WipAndWorkItemAgeScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice05bCycleTime95thScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice06PortfolioReportScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice07NowLengthScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice08DeleteAndCommunityCapScenarios",
            "Lighthouse.Backend.Tests.API.Integration.Reports.Slice10EditReportScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates.Slice01PagesKeepAnsweringScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates.Slice02StopEndsAForecastScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates.Slice03BeyondTheHorizonScenarios",
            "Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates.Slice05CancelAllScenarios",
            "Lighthouse.Backend.Tests.API.Integration.RunChartPayloadContractIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.ServiceNowConnectionAcceptanceTest",
            "Lighthouse.Backend.Tests.API.Integration.ServiceNowTeamSyncAcceptanceTest",
            "Lighthouse.Backend.Tests.API.Integration.ShutdownIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts.Slice01AnnouncedAndDailyScenarios",
            "Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts.Slice01InputChangeScenarios",
            "Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts.Slice01QuietRoundsSkipScenarios",
            "Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts.Slice01UnfinishedRunsAndRestartScenarios",
            "Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts.Slice01UserPressScenarios",
            "Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts.Slice02ConfirmedNowScenarios",
            "Lighthouse.Backend.Tests.API.Integration.SleRisk.Slice01SleRiskReadTest",
            "Lighthouse.Backend.Tests.API.Integration.SpaFallbackRoutingTests",
            "Lighthouse.Backend.Tests.API.Integration.Staleness.Slice01ServerMarksStaleTest",
            "Lighthouse.Backend.Tests.API.Integration.Staleness.Slice01StaleSignalTest",
            "Lighthouse.Backend.Tests.API.Integration.SystemInfoAuthVisibilityCrossLayerTest",
            "Lighthouse.Backend.Tests.API.Integration.SystemInfoWireFormatRegressionTests",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice01FailedRefreshTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice02SeeWhatIsRunningTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice03HowLongHasItBeenGoingTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice04CancellationReachProbe",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice04StopARefreshTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice05AnyBrokenCredentialSaysSoTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice06TheWarningsWithoutTheLogTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice07TheQueueReadsLikeAQueueTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice08HealthThatChecksItselfTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.Story6055ActivityNamesTheWorkTest",
            "Lighthouse.Backend.Tests.API.Integration.TaskManager.TheFixtureClockStartsEveryScenarioAtTheSameInstantTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamConcurrencyTokenIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamDeleteSerialisationTests",
            "Lighthouse.Backend.Tests.API.Integration.TeamDeletionIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize.CommandCountIsolationTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize.Slice00OwnerScopedMetricsTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize.Slice01TeamFeatureSizeTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize.Slice02TeamFeatureSizePercentilesTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamStalenessThresholdSettingsIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamThroughputHistoryValidationIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.TeamsControllerAuthorizationTests",
            "Lighthouse.Backend.Tests.API.Integration.TerminologyControllerAuthorizationTests",
            "Lighthouse.Backend.Tests.API.Integration.TimeInStateReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.UsageDataVeto.Slice03VetoSettingTest",
            "Lighthouse.Backend.Tests.API.Integration.ViewerEmbedSessionJourneyTests",
            "Lighthouse.Backend.Tests.API.Integration.WorkItemAgePercentilesPortfolioReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.WorkItemAgePercentilesReadApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.WorkItemEstimateApiIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest",
            "Lighthouse.Backend.Tests.API.Integration.WriteBackMappingForNewAdditionalFieldIntegrationTest",
            "Lighthouse.Backend.Tests.API.Security.DeleteUnitRightsTests",
            "Lighthouse.Backend.Tests.API.Security.F_BE_1_GroupSnapshotInheritanceTests",
            "Lighthouse.Backend.Tests.API.Security.S13_ViewerEmbedHandshakeTests",
            "Lighthouse.Backend.Tests.API.Security.S14_ViewerEmbedSubjectValidatorTests",
            "Lighthouse.Backend.Tests.API.Security.S15_EmbedSurfaceOptInTests",
            "Lighthouse.Backend.Tests.API.Security.S16_ValidateEndpointAccessTests",
            "Lighthouse.Backend.Tests.API.Security.S17_LicenseDetailsTests",
            "Lighthouse.Backend.Tests.API.Security.S18_ApiKeyCreationScopeTests",
            "Lighthouse.Backend.Tests.API.Security.S1_AllowedOriginsEnvVarBindingTests",
            "Lighthouse.Backend.Tests.API.Security.S1_CorsFailClosedTests",
            "Lighthouse.Backend.Tests.API.Security.S2_ConnectionListPayloadShapeTests",
            "Lighthouse.Backend.Tests.API.Security.S3_UpdateNotificationHubAuthorizeTests",
            "Lighthouse.Backend.Tests.API.Security.S4_DeliveriesDeleteGuardInversionTests",
            "Lighthouse.Backend.Tests.API.Security.S5_ApiKeyScopesTests",
            "Lighthouse.Backend.Tests.API.Security.S6_RateLimitingTests",
            "Lighthouse.Backend.Tests.API.Security.S8_EmbedEntryPointTests",
            "Lighthouse.Backend.Tests.API.Security.S9_EmbedCookiePolicyTests",
            "Lighthouse.Backend.Tests.API.Security.S9b_EmbedCookieConcurrencyTests",
            "Lighthouse.Backend.Tests.API.Security.W1_VersionEndpointAuthorizationTests",
            "Lighthouse.Backend.Tests.Architecture.OptionalFeatureApplierSeamArchUnitTest",
            "Lighthouse.Backend.Tests.Architecture.TestHostIsReleasedAfterDisposeTest",
            "Lighthouse.Backend.Tests.Architecture.TestHostsRunWithoutRateLimitingByDefaultTest",
            "Lighthouse.Backend.Tests.Health.DataProtectionKeyRingTest",
            "Lighthouse.Backend.Tests.Integration.Containers.BulkReportOwnerDeletedMidRunOnPostgresTests",
            "Lighthouse.Backend.Tests.Integration.Containers.CatalogueBodiesOnPostgresTests",
            "Lighthouse.Backend.Tests.Integration.Containers.EmbedSessionSingleUseConcurrencyTests",
            "Lighthouse.Backend.Tests.Integration.Containers.LighterRoundsOnPostgresTests",
            "Lighthouse.Backend.Tests.Integration.Containers.ScalabilityTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.BulkReportsUsageEventTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.OptionalFeatureToggledEventTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.ParentSourcesUsageEventTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.ReportPdfExportUsageEventTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.ReportUsageEventsTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.Slice04ProductEventsTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.Story6193EveryEventSaysItsSourceTest",
            "Lighthouse.Backend.Tests.Integration.UsageData.Story6404FeaturesHeldUsageEventTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.TeamForecastRealityCheckRunEventTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.TeamRefinementNeedUsageEventsTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.TeamRefinementUsageEventsTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.TeamSizingUsageEventsTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataAskEndpointsTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataCollectorAddressTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataConsentEndpointsTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataConsentRateLimitTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataConsentRepositoryTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataEventCeilingTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataEventPipeTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.UsageDataVetoEmitTests",
            "Lighthouse.Backend.Tests.Integration.UsageData.WorkTrackingSystemFieldsChangedUsageEventTests",
            "Lighthouse.Backend.Tests.LighthouseAppContextConcurrencyTest",
            "Lighthouse.Backend.Tests.Models.FeatureDependencyReferenceStorageTest",
            "Lighthouse.Backend.Tests.Models.OAuth.OAuthCredentialTest",
            "Lighthouse.Backend.Tests.Repository.DeliveryRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.DeliveryMetricSnapshotRecordingHandlerTest",
            "Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.DomainEventDispatcherGoldTest",
            "Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.SnapshotRecordedDayInstanceZoneTest",
            "Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.TeamDataRefreshedGoldTest",
            "Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.WorkItemDomainEventsGoldTest",
            "Lighthouse.Backend.Tests.Services.Implementation.FeatureRankSeederTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Licensing.LicensingIntegrationTest",
            "Lighthouse.Backend.Tests.Services.Implementation.LighthouseClockTest",
            "Lighthouse.Backend.Tests.Services.Implementation.OAuth.OAuthProviderRegistryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.OAuth.OAuthStateSecretPersistenceTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.DeletionTests",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.DeliveryMetricSnapshotRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.EmbedSessionTokenRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.FeatureBlockedTransitionRepositoryTests",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.FeatureListReaderRegistrationTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.FeatureRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.LicenseInformationRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.PortfolioRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.RepositoryBaseTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.SizingLogRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.TeamRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.WorkItemBlockedTransitionRepositoryTests",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.WorkItemRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.WorkItemStateTransitionRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Repositories.WorkTrackingSystemConnectionRepositoryTest",
            "Lighthouse.Backend.Tests.Services.Implementation.Seeding.AppSettingSeederTests",
            "Lighthouse.Backend.Tests.Services.Implementation.Seeding.OptionalFeatureSeederTests",
            "Lighthouse.Backend.Tests.Services.Implementation.Seeding.TerminologySeederTests",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkItems.DemoDataStateEnteredSeamIntegrationTest",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkItems.WorkItemServiceCsvStateEnteredSeamIntegrationTest",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkItems.WorkItemServiceFeatureTransitionIntegrationTest",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkItems.WorkItemServiceTransitionFallbackIntegrationTest",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkItems.WorkItemServiceTransitionSyncIntegrationTest",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ConnectorsWithoutDependenciesTest",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Csv.CsvDemoJourneyWithStateMappingTest",
            "Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Csv.CsvWorkTrackingConnectorTest",
            "Lighthouse.Backend.Tests.Startup.EncryptionBootstrapOrderTests",
            "Lighthouse.Backend.Tests.Startup.RecentProblemsCapacityConfigurationTest",
            "Lighthouse.Backend.Tests.Startup.ServiceProviderValidationTest",
            "Lighthouse.Backend.Tests.TestClockBoundaryTest",
            "Lighthouse.Backend.Tests.TestHelpers.TestWebApplicationFactoryTest",
        };

        // The fixtures that start a host because they assert a business outcome only the route or the wiring can show,
        // each with the user flow it walks through.
        private static readonly Dictionary<string, string> SkeletonsByUserFlow = new(StringComparer.Ordinal)
        {
        };

        private ModuleDefinition module = null!;
        private List<TypeDefinition> everyType = [];
        private List<TypeDefinition> concreteClasses = [];
        private List<TypeDefinition> plantedTypes = [];

        private HostBuildingScan Scan { get; set; } = null!;

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestHostUseGuardTest).Assembly);
            Scan = new HostBuildingScan(module, RunsWhenAFixtureRuns);
            everyType = [.. module.Types.SelectMany(TypeAndItsNestedTypes)];
            concreteClasses = [.. everyType.Where(type => type is { IsClass: true, IsAbstract: false } && !type.Name.StartsWith('<'))];
            plantedTypes = [.. everyType.Where(type => PlantedOuterTypes.Any(outer => IsInside(type, outer)))];
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [TestCase(typeof(PlantedFixtures.StartsAHostInOneTimeSetUp))]
        [TestCase(typeof(PlantedFixtures.StartsAHostInAConstructor))]
        [TestCase(typeof(PlantedFixtures.StartsAHostInAHelperTheSetUpCalls))]
        [TestCase(typeof(PlantedFixtures.StartsAHostInTestsThatCarryTestCaseOnly))]
        public void A_fixture_that_starts_a_host_without_a_reason_is_named(Type planted)
        {
            Assert.That(HostUsersOnNeitherList(), Does.Contain(KeyOf(planted)));
        }

        [Test]
        public void A_host_from_a_base_class_set_up_names_the_concrete_fixture_and_not_the_base()
        {
            var named = HostUsersOnNeitherList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Does.Contain(Planted + "/StartsAHostThroughItsBase"));
                Assert.That(named, Does.Not.Contain(Planted + "/AHostingBaseWithNoTests"));
            }
        }

        [Test]
        public void A_nested_fixture_is_named_by_its_outer_and_inner_type()
        {
            Assert.That(HostUsersOnNeitherList(), Does.Contain(Planted + "/AnOuterClass/AnInnerFixtureThatStartsAHost"));
        }

        [Test]
        public void A_copy_of_the_contract_bases_own_test_elsewhere_is_not_exempt()
        {
            Assert.That(HostUsersOnNeitherList(), Does.Contain(Planted + "/WireContractTestTest/OneHostForThreeScenarios"));
        }

        [Test]
        public void The_message_names_the_type_and_the_three_ways_out_and_says_not_to_add_it()
        {
            var message = HostNeedsAReason(["Some.Namespace.SomeHostedTest"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Does.Contain("Some.Namespace.SomeHostedTest"));
                Assert.That(message, Does.Contain("move it to the application-service port"));
                Assert.That(message, Does.Contain("list it as a skeleton with its user flow"));
                Assert.That(message, Does.Contain("make it a wire contract on WireContractTest"));
                Assert.That(message, Does.Contain("Fix the type; do not add it to the list of host users still to move"));
            }
        }

        [Test]
        public void A_listed_type_that_reaches_no_host_is_red_and_its_line_goes_in_this_commit()
        {
            var aPortFixture = KeyOf(typeof(PlantedFixtures.APortFixtureOnTheHarness));

            var named = StaleEntries(plantedTypes, StillToMove(aPortFixture), NoSkeletons());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Is.EqualTo(new[] { aPortFixture }));
                Assert.That(StaleEntryMessage(named), Does.Contain(aPortFixture));
                Assert.That(StaleEntryMessage(named), Does.Contain("Its line goes in this commit"));
            }
        }

        [Test]
        public void A_listed_type_that_no_longer_exists_is_red()
        {
            const string aRemovedType = "Lighthouse.Backend.Tests.Gone.ARemovedFixture";

            var named = StaleEntries(plantedTypes, StillToMove(), Skeletons(aRemovedType));

            Assert.That(named, Is.EqualTo(new[] { aRemovedType }));
        }

        [Test]
        public void A_type_on_both_lists_is_named()
        {
            var listedTwice = KeyOf(typeof(PlantedFixtures.StartsAHostInOneTimeSetUp));

            var named = OnMoreThanOneList(plantedTypes, StillToMove(listedTwice), Skeletons(listedTwice));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Is.EqualTo(new[] { listedTwice }));
                Assert.That(OnMoreThanOneListMessage(named), Does.Contain(listedTwice));
            }
        }

        [Test]
        public void A_type_on_the_contract_base_and_on_a_list_is_named()
        {
            var onTheBase = KeyOf(typeof(PlantedContractFixtures.AWireContractInItsNamespace));

            var named = OnMoreThanOneList(plantedTypes, StillToMove(onTheBase), NoSkeletons());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Is.EqualTo(new[] { onTheBase }));
                Assert.That(OnMoreThanOneListMessage(named), Does.Contain(onTheBase));
            }
        }

        [TestCase(typeof(PlantedFixtures.AWireContractOutsideItsNamespace))]
        [TestCase(typeof(PlantedContractFixtures.AFixtureInTheContractsNamespaceOffTheBase))]
        [TestCase(typeof(PlantedFixtures.WireContractTestTest.OneHostOnTheBaseForThreeScenarios))]
        [TestCase(typeof(ContractsLookalike.PlantedFixturesInALookalikeNamespace.AWireContractInANamespaceThatOnlyStartsLikeIt))]
        public void A_contract_base_and_its_namespace_that_do_not_go_together_are_named(Type planted)
        {
            var named = ContractsWithoutTheBase(plantedTypes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Does.Contain(KeyOf(planted)));
                Assert.That(ContractsWithoutTheBaseMessage(named), Does.Contain(KeyOf(planted)));
            }
        }

        [Test]
        public void The_contract_bases_own_test_passes_the_contracts_rule_only_through_its_exemption()
        {
            var itsFixtures = module.Types.Single(type => type.FullName == TheContractBasesOwnTest)
                .NestedTypes.Where(HasTests).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(itsFixtures, Is.Not.Empty);
                Assert.That(itsFixtures.Where(type => !OnTheBase(type) || InTheContractsNamespace(type)), Is.Empty);
                Assert.That(ContractsWithoutTheBase(itsFixtures), Is.Empty);
            }
        }

        [Test]
        public void A_port_fixture_on_the_harness_reads_as_no_host_and_no_rule_names_it()
        {
            var aPortFixture = plantedTypes.Single(type => type.FullName == KeyOf(typeof(PlantedFixtures.APortFixtureOnTheHarness)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HasTests(aPortFixture), Is.True);
                Assert.That(Scan.FixtureReachesAHost(aPortFixture), Is.False);
                Assert.That(HostUsersOnNeitherList(), Does.Not.Contain(aPortFixture.FullName));
                Assert.That(EveryRuleNames(StillToMove(), NoSkeletons()), Does.Not.Contain(aPortFixture.FullName));
            }
        }

        [Test]
        public void A_fixture_on_the_base_in_the_contracts_namespace_passes_every_rule()
        {
            var aWireContract = KeyOf(typeof(PlantedContractFixtures.AWireContractInItsNamespace));

            Assert.That(EveryRuleNames(StillToMove(), NoSkeletons()), Does.Not.Contain(aWireContract));
        }

        [Test]
        public void A_fixture_on_the_base_in_a_namespace_below_contracts_passes_every_rule()
        {
            var aWireContract = KeyOf(typeof(Contracts.Planted.PlantedContractFixturesBelowTheNamespace.AWireContractBelowItsNamespace));

            Assert.That(EveryRuleNames(StillToMove(), NoSkeletons()), Does.Not.Contain(aWireContract));
        }

        [Test]
        public void Every_concrete_class_in_the_test_assembly_keeps_every_rule()
        {
            var withoutAReason = HostUsersWithoutAReason(concreteClasses, HostUsersStillToMove, SkeletonsByUserFlow);
            var stale = StaleEntries(everyType, HostUsersStillToMove, SkeletonsByUserFlow);
            var onMoreThanOneList = OnMoreThanOneList(concreteClasses, HostUsersStillToMove, SkeletonsByUserFlow);
            var contractsWithoutTheBase = ContractsWithoutTheBase(concreteClasses);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutAReason, Is.Empty, () => HostNeedsAReason(withoutAReason));
                Assert.That(stale, Is.Empty, () => StaleEntryMessage(stale));
                Assert.That(onMoreThanOneList, Is.Empty, () => OnMoreThanOneListMessage(onMoreThanOneList));
                Assert.That(contractsWithoutTheBase, Is.Empty, () => ContractsWithoutTheBaseMessage(contractsWithoutTheBase));
            }
        }

        [Test]
        public void A_listed_skeleton_passes_every_rule()
        {
            var aSkeleton = KeyOf(typeof(PlantedFixtures.StartsAHostInOneTimeSetUp));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HostUsersOnNeitherList(), Does.Contain(aSkeleton));
                Assert.That(EveryRuleNames(StillToMove(), Skeletons(aSkeleton)), Does.Not.Contain(aSkeleton));
            }
        }

        [Test]
        public void A_host_user_still_to_move_passes_every_rule()
        {
            var stillToMove = KeyOf(typeof(PlantedFixtures.StartsAHostInAConstructor));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HostUsersOnNeitherList(), Does.Contain(stillToMove));
                Assert.That(EveryRuleNames(StillToMove(stillToMove), NoSkeletons()), Does.Not.Contain(stillToMove));
            }
        }

        private List<string> HostUsersOnNeitherList() => HostUsersWithoutAReason(plantedTypes, StillToMove(), NoSkeletons());

        private List<string> EveryRuleNames(HashSet<string> stillToMove, Dictionary<string, string> skeletons)
            => [.. HostUsersWithoutAReason(plantedTypes, stillToMove, skeletons),
                .. StaleEntries(plantedTypes, stillToMove, skeletons),
                .. OnMoreThanOneList(plantedTypes, stillToMove, skeletons),
                .. ContractsWithoutTheBase(plantedTypes)];

        private static HashSet<string> StillToMove(params string[] types) => new(types, StringComparer.Ordinal);

        private static Dictionary<string, string> Skeletons(params string[] types)
            => types.ToDictionary(type => type, _ => "a user flow", StringComparer.Ordinal);

        private static Dictionary<string, string> NoSkeletons() => Skeletons();

        /// <summary>
        /// The fixtures among the given types that can start a host and have no reason to: not a wire contract on
        /// the base, and not the contract base's own test or a class nested in it.
        /// </summary>
        private List<string> HostUsersWithoutAReason(List<TypeDefinition> types, HashSet<string> stillToMove, Dictionary<string, string> skeletons)
            => [.. types.Where(HasTests)
                .Where(type => !IsTheContractBasesOwnTest(type))
                .Where(type => !OnTheBase(type))
                .Where(type => !stillToMove.Contains(type.FullName) && !skeletons.ContainsKey(type.FullName))
                .Where(Scan.FixtureReachesAHost)
                .Select(type => type.FullName)
                .Order(StringComparer.Ordinal)];

        /// <summary>The listed types that no longer exist among the given types, or that start no host.</summary>
        private List<string> StaleEntries(List<TypeDefinition> types, HashSet<string> stillToMove, Dictionary<string, string> skeletons)
        {
            var byName = types.ToDictionary(type => type.FullName, StringComparer.Ordinal);

            return [.. stillToMove.Concat(skeletons.Keys)
                .Where(name => !byName.TryGetValue(name, out var type) || !Scan.FixtureReachesAHost(type))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];
        }

        /// <summary>The types given more than one reason to start a host: both lists, or the contract base and a list.</summary>
        private List<string> OnMoreThanOneList(List<TypeDefinition> types, HashSet<string> stillToMove, Dictionary<string, string> skeletons)
        {
            var onBothLists = stillToMove.Where(skeletons.ContainsKey);
            var onTheBaseAndListed = types.Where(OnTheBase)
                .Select(type => type.FullName)
                .Where(name => stillToMove.Contains(name) || skeletons.ContainsKey(name));

            return [.. onBothLists.Concat(onTheBaseAndListed).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        }

        /// <summary>
        /// The fixtures on the contract base outside the contracts namespace, and those in it off the base. The contract
        /// base's own test is the one exception.
        /// </summary>
        private List<string> ContractsWithoutTheBase(List<TypeDefinition> types)
            => [.. types.Where(HasTests)
                .Where(type => !IsTheContractBasesOwnTest(type))
                .Where(type => OnTheBase(type) != InTheContractsNamespace(type))
                .Select(type => type.FullName)
                .Order(StringComparer.Ordinal)];

        private bool OnTheBase(TypeDefinition type) => Scan.DerivesFrom(type, TheContractBase);

        private static bool InTheContractsNamespace(TypeDefinition type)
        {
            var itsNamespace = TopLevelTypeOf(type).Namespace;

            return itsNamespace == TheContractsNamespace || itsNamespace.StartsWith(TheContractsNamespace + ".", StringComparison.Ordinal);
        }

        private static bool IsTheContractBasesOwnTest(TypeDefinition type) => TopLevelTypeOf(type).FullName == TheContractBasesOwnTest;

        private bool HasTests(TypeDefinition type)
            => Scan.TypeAndItsBases(type).SelectMany(each => each.Methods).Any(method => HasAnAttribute(method, TestAttributes));

        private static string HostNeedsAReason(List<string> types)
            => $"{types.Count} type(s) start an application host without a reason:{Environment.NewLine}"
                + Lines(types)
                + "A test may start a host only for a reason. For each type, do one of three things: "
                + "move it to the application-service port (build the service with ServicePortHarness, or with new for up to three collaborators, under Tests/Acceptance/<Feature>/); "
                + "list it as a skeleton with its user flow, if it asserts a business outcome that needs the route or the wiring; "
                + "or make it a wire contract on WireContractTest in Tests/Contracts/. "
                + "Fix the type; do not add it to the list of host users still to move, which only shrinks.";

        private static string StaleEntryMessage(List<string> types)
            => $"{types.Count} listed type(s) no longer exist or no longer start an application host:{Environment.NewLine}"
                + Lines(types)
                + "Its line goes in this commit: take it off the list, which names only types that still start a host.";

        private static string OnMoreThanOneListMessage(List<string> types)
            => $"{types.Count} type(s) have more than one reason to start a host, on both lists or on WireContractTest and on a list:{Environment.NewLine}"
                + Lines(types)
                + "Keep each on one list only, or on WireContractTest and on no list.";

        private static string ContractsWithoutTheBaseMessage(List<string> types)
            => $"{types.Count} type(s) break the rule that Tests/Contracts holds the fixtures on WireContractTest and nothing else:{Environment.NewLine}"
                + Lines(types)
                + "Move a fixture on WireContractTest into Lighthouse.Backend.Tests.Contracts or a namespace below it, and a fixture there that is not on it out.";

        private static string Lines(List<string> types) => string.Concat(types.Select(type => $"  {type}{Environment.NewLine}"));

        private static TypeDefinition TopLevelTypeOf(TypeDefinition type)
            => type.DeclaringType is null ? type : TopLevelTypeOf(type.DeclaringType);

        private static bool IsInside(TypeDefinition type, string outer) => type.FullName.StartsWith(outer + "/", StringComparison.Ordinal);

        private static string KeyOf(Type type) => type.FullName!.Replace('+', '/');

        // Fixtures that exist only so the rules can be pointed at something. They are abstract because NUnit runs
        // any class with a test method, and these have nothing to run; the rules judge only concrete classes when
        // they read the real assembly.
        internal static class PlantedFixtures
        {
            internal abstract class StartsAHostInOneTimeSetUp
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class StartsAHostInAConstructor
            {
                protected StartsAHostInAConstructor() => _ = new TestWebApplicationFactory<Program>();

                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class AHostingBaseWithNoTests
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class StartsAHostThroughItsBase : AHostingBaseWithNoTests
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class StartsAHostInAHelperTheSetUpCalls
            {
                [OneTimeSetUp]
                public void Prepare() => BuildTheHost();

                [Test]
                public void Serve() => Assert.Pass();

                private static TestWebApplicationFactory<Program> BuildTheHost() => new();
            }

            internal abstract class StartsAHostInTestsThatCarryTestCaseOnly
            {
                [TestCase(1)]
                public void Serve(int scenario)
                {
                    _ = new TestWebApplicationFactory<Program>();
                    Assert.That(scenario, Is.Positive);
                }
            }

            internal static class AnOuterClass
            {
                internal abstract class AnInnerFixtureThatStartsAHost
                {
                    [OneTimeSetUp]
                    public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                    [Test]
                    public void Serve() => Assert.Pass();
                }
            }

            internal abstract class APortFixtureOnTheHarness
            {
                [Test]
                public void Serve()
                {
                    using var harness = new ServicePortHarness();
                    Assert.That(harness, Is.Not.Null);
                }
            }

            internal abstract class AWireContractOutsideItsNamespace : WireContractTest
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            // The contract base's own test, copied by name and shape, once off the base and once on it.
            internal static class WireContractTestTest
            {
                internal abstract class OneHostOnTheBaseForThreeScenarios : WireContractTest
                {
                    [TestCase(1)]
                    [TestCase(2)]
                    [TestCase(3)]
                    public void Serve(int scenario) => Assert.That(scenario, Is.Positive);
                }

                internal abstract class OneHostForThreeScenarios
                {
                    [OneTimeSetUp]
                    public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                    [TestCase(1)]
                    [TestCase(2)]
                    [TestCase(3)]
                    public void Serve(int scenario) => Assert.That(scenario, Is.Positive);
                }
            }
        }
    }
}

namespace Lighthouse.Backend.Tests.Contracts
{
    // Fixtures planted in the contracts namespace, so the guard's rule for that namespace has something to judge.
    // Abstract for the same reason as the guard's other planted fixtures.
    internal static class PlantedContractFixtures
    {
        internal abstract class AWireContractInItsNamespace : WireContractTest
        {
            [Test]
            public void Serve() => Assert.Pass();
        }

        internal abstract class AFixtureInTheContractsNamespaceOffTheBase
        {
            [Test]
            public void Serve() => Assert.Pass();
        }
    }
}

namespace Lighthouse.Backend.Tests.Contracts.Planted
{
    // A wire contract one namespace below the contracts namespace, where the real contracts sit by area.
    internal static class PlantedContractFixturesBelowTheNamespace
    {
        internal abstract class AWireContractBelowItsNamespace : WireContractTest
        {
            [Test]
            public void Serve() => Assert.Pass();
        }
    }
}

namespace Lighthouse.Backend.Tests.ContractsLookalike
{
    // A namespace whose name begins with the contracts namespace's name but is not inside it.
    internal static class PlantedFixturesInALookalikeNamespace
    {
        internal abstract class AWireContractInANamespaceThatOnlyStartsLikeIt : WireContractTest
        {
            [Test]
            public void Serve() => Assert.Pass();
        }
    }
}
