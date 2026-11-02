using Lighthouse.Backend.Tests.TestHelpers;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Starting an application host costs hundreds of milliseconds. A fixture that starts one for every test
    /// pays that again and again; a fixture that starts one in its one-time set-up pays it once and resets what
    /// differs per test in its per-test set-up. This guard reads the compiled test assembly and lists every type
    /// whose per-test set-up or test bodies (and anything those call inside the test assembly) start a host. The
    /// list below is a ratchet: it may only shrink. A new type that starts a host per test fails the first test;
    /// a type that was fixed and is still listed fails the second.
    /// </summary>
    [TestFixture]
    public class TestHostPerFixtureGuardTest
    {
        private const string NotMovedYet = "not moved yet: starts an application host for every test";
        private const string OnAPostgresServer = "needs a Postgres server (or a Redis store) and a host of its own per test; those tests are not part of this move";
        private const string HasOneTestOnly = "has a single test, so a host per fixture would save no host";
        private const string ChecksTheTestHostItself = "tests how the test host starts and stops, so it has to start hosts itself";
        private const string KeepsProblemsNothingCanEmpty = "reads the problems the host keeps in a buffer that no public call empties, so each test needs a host of its own";
        private const string StopsAndRestartsItsHostInOneTest = "one test stops the host the way a service manager does and starts another, so the fixture builds a host again inside a test";

        // Fixing a type lowers this number in the same commit as it removes the type's line. It never goes up.
        private const int MostTypesTheListMayHold = 114;

        // Keys are full type names, so two types with the same simple name in different namespaces stay apart.
        private static readonly IReadOnlyDictionary<string, string> TypesAllowedToBuildAHostPerTest = new Dictionary<string, string>
        {
            ["Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesPortfolioReadApiIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryReadStabilityIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryStaleAggregateRaceIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryWriteRefusalIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.BehaviourSettings.BehaviourSettingsAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.BlackoutForecastShiftItemPredictionIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.BlackoutForecastShiftTeamForecastIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedItemsAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedStalenessThresholdValidationTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.BlockedItems.PortfolioBlockedHistoryAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice01JiraDependenciesFromANamedLinkScenarios"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice02NamedLinkThatMatchedNothingScenarios"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice03AzureDevOpsRelationTypeAcceptedScenarios"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection.Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ConnectionSecretStatePayloadTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ConnectionValidationUnreadableSecretTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionSettingsIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionValidityIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.DeliveryArchiveClosurePinIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.DeliveryMetricsHistoryReadApiIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.DeliverySources.DeliverySourcesAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.Dependencies.DependenciesAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting.Slice00OneForecastPerBatchScenarios"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.EncryptionControllerTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.Slice01ForecastRoundsRunOnTheirOwnThreadsScenarios"] = StopsAndRestartsItsHostInOneTest,
            ["Lighthouse.Backend.Tests.API.Integration.ForecastFilterTeamSettingsIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ForecastFilterThroughputChartIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck.ForecastRealityCheckAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ForwardedHeadersIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.HealthCheckIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.JwtBearerAuthIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ManualSorting.ManualSortingAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.McpInboundAuthIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.MetricsEndpointIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.OAuthCallbackCsrfIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.OAuthControllerIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.OAuthProviderAbstractionIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates.Slice01NarrowToFeaturesTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates.Slice02OldestLinkWinsTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates.Slice03PortfolioOldestLinkTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks.Slice03AmbiguityRefusalTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.PercentilesOverTimeAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioDeleteSerialisationTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioStalenessThresholdSettingsIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioTimeInStateReadApiIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.QuietWriteBack.QuietWriteBackAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RbacGroupMappingConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesByDateForecastIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesChartOverlayIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesDeliveryIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesDownstreamParityIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesIntervalRuleIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesLifecycleIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesWeekendsForeverIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesWriteBackIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.Reports.ReportCreationTimingProbe"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.RunChartPayloadContractIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ServiceNowConnectionAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ShutdownIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.SleRisk.SleRiskAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.Staleness.StaleWorkItemsAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.SystemInfoAuthVisibilityCrossLayerTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.SystemInfoWireFormatRegressionTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.TaskManager.Slice06TheWarningsWithoutTheLogTest"] = KeepsProblemsNothingCanEmpty,
            ["Lighthouse.Backend.Tests.API.Integration.TaskManager.TaskManagerAcceptanceTest"] = KeepsProblemsNothingCanEmpty,
            ["Lighthouse.Backend.Tests.API.Integration.TeamConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.TeamDeleteSerialisationTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize.TeamFeatureSizeAcceptanceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.TeamStalenessThresholdSettingsIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.TeamThroughputHistoryValidationIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.TimeInStateReadApiIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.ViewerEmbedSessionJourneyTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.WorkItemEstimateApiIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Integration.WriteBackMappingForNewAdditionalFieldIntegrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.F_BE_1_GroupSnapshotInheritanceTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S13_ViewerEmbedHandshakeTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S14_ViewerEmbedSubjectValidatorTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S15_EmbedSurfaceOptInTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S1_AllowedOriginsEnvVarBindingTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S1_CorsFailClosedTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S3_UpdateNotificationHubAuthorizeTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S4_DeliveriesDeleteGuardInversionTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S5_ApiKeyScopesTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S6_RateLimitingTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S8_EmbedEntryPointTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S9b_EmbedCookieConcurrencyTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.S9_EmbedCookiePolicyTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.API.Security.W1_VersionEndpointAuthorizationTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Architecture.OptionalFeatureApplierSeamArchUnitTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Architecture.TestHostIsReleasedAfterDisposeTest"] = ChecksTheTestHostItself,
            ["Lighthouse.Backend.Tests.Architecture.TestHostsRunWithoutRateLimitingByDefaultTest"] = ChecksTheTestHostItself,
            ["Lighthouse.Backend.Tests.Health.DataProtectionKeyRingTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Integration.Containers.BulkReportOwnerDeletedMidRunOnPostgresTests"] = OnAPostgresServer,
            ["Lighthouse.Backend.Tests.Integration.Containers.CatalogueBodiesOnPostgresTests"] = OnAPostgresServer,
            ["Lighthouse.Backend.Tests.Integration.Containers.EmbedSessionSingleUseConcurrencyTests"] = OnAPostgresServer,
            ["Lighthouse.Backend.Tests.Integration.Containers.LighterRoundsOnPostgresTests"] = OnAPostgresServer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ScalabilityTests"] = OnAPostgresServer,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataCollectorAddressTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataConsentRateLimitTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataEventCeilingTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.LighthouseAppContextConcurrencyTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.DeliveryMetricSnapshotRecordingHandlerTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.SnapshotRecordedDayInstanceZoneTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Services.Implementation.LighthouseClockTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Services.Implementation.OAuth.OAuthProviderRegistryTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Services.Implementation.OAuth.OAuthStateSecretPersistenceTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Services.Implementation.Repositories.FeatureListReaderRegistrationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Startup.EncryptionBootstrapOrderTests"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Startup.RecentProblemsCapacityConfigurationTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.Startup.ServiceProviderValidationTest"] = HasOneTestOnly,
            ["Lighthouse.Backend.Tests.TestClockBoundaryTest"] = NotMovedYet,
            ["Lighthouse.Backend.Tests.TestHelpers.TestWebApplicationFactoryTest"] = ChecksTheTestHostItself,
        };

        private ModuleDefinition module = null!;
        private HostBuildingScan scan = null!;
        private IReadOnlyCollection<string> typesFoundBuildingAHostPerTest = [];

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestHostPerFixtureGuardTest).Assembly);
            scan = new HostBuildingScan(module);
            typesFoundBuildingAHostPerTest = scan.OffendingRoots(root => TopLevelTypeOf(root.DeclaringType) != typeof(TestHostPerFixtureGuardTest).FullName)
                .Select(root => TopLevelTypeOf(root.DeclaringType))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [Test]
        public void NoTypeStartsAnApplicationHostForEveryTestWithoutBeingOnTheList()
        {
            var comparison = Compare(TypesAllowedToBuildAHostPerTest.Keys, typesFoundBuildingAHostPerTest);

            Assert.That(
                comparison.Newcomers,
                Is.Empty,
                "These types start an application host in a per-test set-up or in a test body: " + string.Join(", ", comparison.Newcomers) +
                ". Build the host in [OneTimeSetUp] and dispose it in [OneTimeTearDown]; reset what differs per test in [SetUp] through " +
                "objects the fixture owns. If a type genuinely needs a host of its own per test, add it to the list with the reason.");
        }

        [Test]
        public void EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest()
        {
            var comparison = Compare(TypesAllowedToBuildAHostPerTest.Keys, typesFoundBuildingAHostPerTest);

            Assert.That(
                comparison.Stale,
                Is.Empty,
                "These types are on the list but no longer start a host per test: " + string.Join(", ", comparison.Stale) +
                ". Remove them from the list so it cannot grow back unseen.");
        }

        [Test]
        public void TheListOnlyShrinks()
        {
            Assert.That(
                TypesAllowedToBuildAHostPerTest,
                Has.Count.LessThanOrEqualTo(MostTypesTheListMayHold),
                "The list of types allowed to start a host for every test can only get shorter. Fix the type instead of adding it, " +
                "and when a type leaves the list, lower the number above by the same amount.");
        }

        [Test]
        public void ANewcomerOutsideTheListIsReportedAndNothingElse()
        {
            var comparison = Compare(["Alpha", "Beta"], ["Alpha", "Beta", "Gamma"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(comparison.Newcomers, Is.EqualTo(Names("Gamma")));
                Assert.That(comparison.Stale, Is.Empty);
            }
        }

        [Test]
        public void AStaleEntryOnTheListIsReportedAndNothingElse()
        {
            var comparison = Compare(["Alpha", "Beta", "Gamma"], ["Alpha", "Gamma"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(comparison.Stale, Is.EqualTo(Names("Beta")));
                Assert.That(comparison.Newcomers, Is.Empty);
            }
        }

        [Test]
        public void ListsThatAgreeReportNothingWhateverTheOrder()
        {
            var comparison = Compare(["Beta", "Alpha"], ["Alpha", "Beta"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(comparison.Newcomers, Is.Empty);
                Assert.That(comparison.Stale, Is.Empty);
            }
        }

        [Test]
        public void ANewcomerAndAStaleEntryAreReportedTogether()
        {
            var comparison = Compare(["Alpha", "Old"], ["Alpha", "New"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(comparison.Newcomers, Is.EqualTo(Names("New")));
                Assert.That(comparison.Stale, Is.EqualTo(Names("Old")));
            }
        }

        [TestCase(nameof(PlantedFixtures.StartsAHostInPerTestSetUp), true)]
        [TestCase(nameof(PlantedFixtures.StartsAHostInATestBody), true)]
        [TestCase(nameof(PlantedFixtures.StartsAHostInAHelperTheSetUpCalls), true)]
        [TestCase(nameof(PlantedFixtures.StartsAHostInALambda), true)]
        [TestCase(nameof(PlantedFixtures.StartsAHostAfterAnAwait), true)]
        [TestCase(nameof(PlantedFixtures.DerivesAHostInATestBody), true)]
        [TestCase(nameof(PlantedFixtures.StartsAHostFromAFactoryThreeStepsBelowTheNamedOne), true)]
        [TestCase(nameof(PlantedFixtures.StartsAHostOnlyThroughAVirtualCall), true)]
        [TestCase(nameof(PlantedFixtures.CallsAnUnrelatedMethodWithTheSameNameAsAHostSink), false)]
        [TestCase(nameof(PlantedFixtures.StartsAHostOnlyInOneTimeSetUp), false)]
        [TestCase(nameof(PlantedFixtures.StartsNoHostAtAll), false)]
        public void TheScanSeesAHostStartedForEveryTestHoweverItIsReached(string plantedType, bool shouldBeFound)
        {
            var found = scan.OffendingRoots(root => root.DeclaringType.DeclaringType?.Name == nameof(PlantedFixtures))
                .Select(root => root.DeclaringType.Name)
                .ToHashSet(StringComparer.Ordinal);

            Assert.That(found.Contains(plantedType), Is.EqualTo(shouldBeFound));
        }

        private static string[] Names(params string[] names) => names;

        private static ListComparison Compare(IEnumerable<string> listed, IEnumerable<string> found)
        {
            var listedNames = listed.ToHashSet(StringComparer.Ordinal);
            var foundNames = found.ToHashSet(StringComparer.Ordinal);

            return new ListComparison(
                [.. foundNames.Except(listedNames).Order(StringComparer.Ordinal)],
                [.. listedNames.Except(foundNames).Order(StringComparer.Ordinal)]);
        }

        private static string TopLevelTypeOf(TypeDefinition type)
            => type.DeclaringType is null ? type.FullName : TopLevelTypeOf(type.DeclaringType);

        private sealed record ListComparison(IReadOnlyList<string> Newcomers, IReadOnlyList<string> Stale);

        /// <summary>
        /// Walks the call tree below every set-up, test and test-case method in an assembly and answers which of
        /// them can reach the start of an application host. Calls are followed into methods and constructors
        /// that are written in the same assembly, into the bodies the compiler makes for lambdas, local
        /// functions and async methods, and into overrides of a virtual method that is called.
        /// </summary>
        private sealed class HostBuildingScan
        {
            private static readonly HashSet<string> PerTestAttributes =
            [
                "NUnit.Framework.SetUpAttribute",
                "NUnit.Framework.TestAttribute",
                "NUnit.Framework.TestCaseAttribute",
                "NUnit.Framework.TestCaseSourceAttribute",
                "NUnit.Framework.TheoryAttribute",
            ];

            private static readonly string[] FactoryTypeNamePrefixes = ["TestWebApplicationFactory", "WebApplicationFactory"];

            private readonly List<MethodDefinition> allMethods;
            private readonly Dictionary<string, List<MethodDefinition>> methodsByKey;
            private readonly Dictionary<string, TypeDefinition> typesByName;
            private readonly HashSet<string> factoryTypeNames;
            private readonly HashSet<MethodDefinition> knownToStartNoHost = [];

            public HostBuildingScan(ModuleDefinition module)
            {
                var allTypes = module.Types.SelectMany(TypeAndItsNestedTypes).ToList();
                allMethods = [.. allTypes.SelectMany(type => type.Methods)];
                methodsByKey = allMethods.GroupBy(KeyOf).ToDictionary(group => group.Key, group => group.ToList());
                typesByName = allTypes.ToDictionary(type => type.FullName, type => type);
                factoryTypeNames = FactoryTypesIn(allTypes);
            }

            /// <summary>The per-test methods, among those the filter picks, from which a host start can be reached.</summary>
            public IReadOnlyList<MethodDefinition> OffendingRoots(Func<MethodDefinition, bool> pickRoots)
                => [.. allMethods.Where(IsPerTestMethod).Where(pickRoots).Where(ReachesAHostStart)];

            private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
                => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

            private static string KeyOf(MethodDefinition method)
                => method.DeclaringType.FullName + "::" + method.Name + "/" + method.Parameters.Count;

            private static string KeyOf(MethodReference method)
            {
                var declared = method is GenericInstanceMethod generic ? generic.ElementMethod : method;
                return declared.DeclaringType.GetElementType().FullName + "::" + declared.Name + "/" + declared.Parameters.Count;
            }

            private static bool IsPerTestMethod(MethodDefinition method)
                => !method.DeclaringType.Name.StartsWith('<')
                    && method.CustomAttributes.Any(attribute => PerTestAttributes.Contains(attribute.AttributeType.FullName));

            private static bool NamesAFactory(TypeReference type)
                => FactoryTypeNamePrefixes.Any(prefix => type.GetElementType().Name.StartsWith(prefix, StringComparison.Ordinal));

            private static HashSet<string> FactoryTypesIn(List<TypeDefinition> allTypes)
            {
                var factories = allTypes.Where(type => type.BaseType is not null && NamesAFactory(type.BaseType))
                    .Select(type => type.FullName)
                    .ToHashSet(StringComparer.Ordinal);

                bool grew;

                do
                {
                    grew = false;

                    foreach (var type in allTypes.Where(type => type.BaseType is not null && factories.Contains(type.BaseType.GetElementType().FullName)))
                    {
                        grew |= factories.Add(type.FullName);
                    }
                }
                while (grew);

                return factories;
            }

            private bool IsAFactoryType(TypeReference type)
                => NamesAFactory(type) || factoryTypeNames.Contains(type.GetElementType().FullName);

            private bool IsAHostStart(Instruction instruction)
                => instruction.Operand is MethodReference called
                    && IsAFactoryType(called.DeclaringType)
                    && (instruction.OpCode.Code == Code.Newobj
                        || instruction.OpCode.Code is Code.Call or Code.Callvirt && called.Name is "WithWebHostBuilder" or "WithTestAuthentication");

            private bool ReachesAHostStart(MethodDefinition root)
            {
                var visited = new HashSet<MethodDefinition>();
                var pending = new Stack<MethodDefinition>([root]);

                while (pending.Count > 0)
                {
                    var method = pending.Pop();

                    if (!visited.Add(method) || knownToStartNoHost.Contains(method))
                    {
                        continue;
                    }

                    if (method.HasBody && method.Body.Instructions.Any(IsAHostStart))
                    {
                        return true;
                    }

                    foreach (var next in MethodsRunBy(method))
                    {
                        pending.Push(next);
                    }
                }

                knownToStartNoHost.UnionWith(visited);
                return false;
            }

            private IEnumerable<MethodDefinition> MethodsRunBy(MethodDefinition method)
                => CompilerMadeBodiesOf(method).Concat(
                    method.HasBody
                        ? method.Body.Instructions.SelectMany(MethodsStartedBy)
                        : []);

            // An async method's own body lives in the MoveNext of a type the compiler names "<Method>d__N".
            private static IEnumerable<MethodDefinition> CompilerMadeBodiesOf(MethodDefinition method)
                => method.DeclaringType.NestedTypes
                    .Where(nested => nested.Name.StartsWith("<" + method.Name + ">", StringComparison.Ordinal))
                    .SelectMany(nested => nested.Methods);

            private IEnumerable<MethodDefinition> MethodsStartedBy(Instruction instruction)
            {
                if (instruction.Operand is not MethodReference called
                    || instruction.OpCode.Code is not (Code.Call or Code.Callvirt or Code.Newobj or Code.Ldftn or Code.Ldvirtftn)
                    || NamesAFactory(called.DeclaringType)
                    || factoryTypeNames.Contains(called.DeclaringType.GetElementType().FullName))
                {
                    return [];
                }

                var declared = methodsByKey.GetValueOrDefault(KeyOf(called)) ?? [];

                var calledOnATypeWrittenHere = typesByName.ContainsKey(called.DeclaringType.GetElementType().FullName);

                return instruction.OpCode.Code is Code.Callvirt or Code.Ldvirtftn && calledOnATypeWrittenHere
                    ? declared.Concat(OverridesOf(called))
                    : declared;
            }

            private IEnumerable<MethodDefinition> OverridesOf(MethodReference called)
                => allMethods.Where(candidate => candidate.IsVirtual
                    && candidate.Name == called.Name
                    && candidate.Parameters.Count == called.Parameters.Count
                    && DerivesFrom(candidate.DeclaringType, called.DeclaringType.GetElementType().FullName));

            private bool DerivesFrom(TypeDefinition type, string baseTypeName)
            {
                var current = type.BaseType;

                while (current is not null)
                {
                    var name = current.GetElementType().FullName;

                    if (name == baseTypeName)
                    {
                        return true;
                    }

                    current = typesByName.GetValueOrDefault(name)?.BaseType;
                }

                return false;
            }
        }

        // Fixtures that exist only so the scan can be pointed at something. They are abstract because NUnit runs
        // any class with a [Test] method, and these have nothing to run; the scan skips them when it reads the
        // real assembly.
        internal static class PlantedFixtures
        {
            internal abstract class StartsAHostInPerTestSetUp
            {
                [SetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class StartsAHostInATestBody
            {
                [Test]
                public void Serve()
                {
                    _ = new TestWebApplicationFactory<Program>();
                    Assert.Pass();
                }
            }

            internal abstract class StartsAHostInAHelperTheSetUpCalls
            {
                [SetUp]
                public void Prepare() => BuildTheHost();

                private static TestWebApplicationFactory<Program> BuildTheHost() => new();
            }

            internal abstract class StartsAHostInALambda
            {
                [SetUp]
                public void Prepare()
                {
                    Func<object> makeIt = () => new TestWebApplicationFactory<Program>();
                    _ = makeIt();
                }
            }

            internal abstract class StartsAHostAfterAnAwait
            {
                [SetUp]
                public async Task Prepare()
                {
                    await Task.Yield();
                    _ = new TestWebApplicationFactory<Program>();
                }
            }

            internal abstract class DerivesAHostInATestBody
            {
                [Test]
                public void Serve()
                {
                    Derive(null!);
                    Assert.Pass();
                }

                private static void Derive(TestWebApplicationFactory<Program> root) => _ = root.WithWebHostBuilder(_ => { });
            }

            // Declared from the bottom up on purpose: a scan that walks the types once, in order, meets each
            // factory before it knows its parent is one, so only a scan that repeats finds the third step.
#pragma warning disable S2094 // The classes are empty on purpose: only their place in the inheritance chain is scanned.
            internal class FactoryThreeStepsBelowTheNamedOne : FactoryTwoStepsBelowTheNamedOne;

            internal class FactoryTwoStepsBelowTheNamedOne : FactoryOneStepBelowTheNamedOne;

            internal class FactoryOneStepBelowTheNamedOne : TestWebApplicationFactory<Program>;
#pragma warning restore S2094

            internal abstract class StartsAHostFromAFactoryThreeStepsBelowTheNamedOne
            {
                [SetUp]
                public void Prepare() => _ = new FactoryThreeStepsBelowTheNamedOne();
            }

            internal abstract class PrepareStep
            {
                public abstract void Run();
            }

            internal sealed class PrepareStepThatStartsAHost : PrepareStep
            {
                public override void Run() => _ = new TestWebApplicationFactory<Program>();
            }

            // The set-up only sees the base type; the host is started by an override it never names.
            internal abstract class StartsAHostOnlyThroughAVirtualCall
            {
                [SetUp]
                public void Prepare() => step.Run();

                private readonly PrepareStep step = new PrepareStepThatStartsAHost();
            }

            internal static class SomethingThatIsNotAHostFactory
            {
                internal static void WithWebHostBuilder() => _ = 1 + 1;
            }

            internal abstract class CallsAnUnrelatedMethodWithTheSameNameAsAHostSink
            {
                [SetUp]
                public void Prepare() => SomethingThatIsNotAHostFactory.WithWebHostBuilder();
            }

            internal abstract class StartsAHostOnlyInOneTimeSetUp
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [SetUp]
                public void ResetWhatDiffersPerTest() => _ = 1 + 1;

                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class StartsNoHostAtAll
            {
                [Test]
                public void Add() => Assert.Pass();
            }
        }
    }
}
