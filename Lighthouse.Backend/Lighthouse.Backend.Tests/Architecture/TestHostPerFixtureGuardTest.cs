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
        private const string OnAPostgresServer = "needs a Postgres server and a host of its own per test; those tests are not part of this move";
        private const string ChecksTheTestHostItself = "tests how the test host starts and stops, so it has to start hosts itself";

        private static readonly IReadOnlyDictionary<string, string> TypesAllowedToBuildAHostPerTest = new Dictionary<string, string>
        {
            ["AgeInStatePercentilesPortfolioReadApiIntegrationTest"] = NotMovedYet,
            ["ArchivedDeliveryReadStabilityIntegrationTest"] = NotMovedYet,
            ["ArchivedDeliveryStaleAggregateRaceIntegrationTest"] = NotMovedYet,
            ["ArchivedDeliveryWriteRefusalIntegrationTest"] = NotMovedYet,
            ["BehaviourSettingsAcceptanceTest"] = NotMovedYet,
            ["BlackoutForecastShiftItemPredictionIntegrationTest"] = NotMovedYet,
            ["BlackoutForecastShiftTeamForecastIntegrationTest"] = NotMovedYet,
            ["BlockedItemsAcceptanceTest"] = NotMovedYet,
            ["BlockedStalenessThresholdValidationTests"] = NotMovedYet,
            ["BulkReportOwnerDeletedMidRunOnPostgresTests"] = OnAPostgresServer,
            ["CatalogueBodiesOnPostgresTests"] = OnAPostgresServer,
            ["ClusterSubstrateProbeRegistrationTest"] = NotMovedYet,
            ["ConnectionDefaultsAcceptanceTest"] = NotMovedYet,
            ["ConnectionFieldChangeAcceptanceTest"] = NotMovedYet,
            ["ConnectionSecretStatePayloadTests"] = NotMovedYet,
            ["ConnectionValidationUnreadableSecretTests"] = NotMovedYet,
            ["CycleTimeDefinitionSettingsIntegrationTest"] = NotMovedYet,
            ["CycleTimeDefinitionValidityIntegrationTest"] = NotMovedYet,
            ["DataProtectionKeyRingTest"] = NotMovedYet,
            ["DeliveryArchiveClosurePinIntegrationTest"] = NotMovedYet,
            ["DeliveryMetricSnapshotRecordingHandlerTest"] = NotMovedYet,
            ["DeliveryMetricsHistoryReadApiIntegrationTest"] = NotMovedYet,
            ["DeliverySourcesAcceptanceTest"] = NotMovedYet,
            ["DependenciesAcceptanceTest"] = NotMovedYet,
            ["EmbedSessionSingleUseConcurrencyTests"] = NotMovedYet,
            ["EncryptionBootstrapOrderTests"] = NotMovedYet,
            ["EncryptionControllerTests"] = NotMovedYet,
            ["F_BE_1_GroupSnapshotInheritanceTests"] = NotMovedYet,
            ["FastWithManyFeaturesAcceptanceTest"] = NotMovedYet,
            ["FasterUpdatesAcceptanceTest"] = NotMovedYet,
            ["FeatureListReaderRegistrationTest"] = NotMovedYet,
            ["ForecastFilterTeamSettingsIntegrationTest"] = NotMovedYet,
            ["ForecastFilterThroughputChartIntegrationTest"] = NotMovedYet,
            ["ForecastRealityCheckAcceptanceTest"] = NotMovedYet,
            ["ForwardedHeadersIntegrationTest"] = NotMovedYet,
            ["HealthCheckIntegrationTest"] = NotMovedYet,
            ["JwtBearerAuthIntegrationTest"] = NotMovedYet,
            ["LighterRoundsOnPostgresTests"] = OnAPostgresServer,
            ["LighthouseAppContextConcurrencyTest"] = NotMovedYet,
            ["LighthouseClockTest"] = NotMovedYet,
            ["ManualSortingAcceptanceTest"] = NotMovedYet,
            ["McpInboundAuthIntegrationTest"] = NotMovedYet,
            ["MetricsEndpointIntegrationTest"] = NotMovedYet,
            ["OAuthCallbackCsrfIntegrationTest"] = NotMovedYet,
            ["OAuthControllerIntegrationTest"] = NotMovedYet,
            ["OAuthProviderAbstractionIntegrationTest"] = NotMovedYet,
            ["OAuthProviderRegistryTest"] = NotMovedYet,
            ["OAuthStateSecretPersistenceTest"] = NotMovedYet,
            ["OptionalFeatureApplierSeamArchUnitTest"] = NotMovedYet,
            ["PercentilesOverTimeAcceptanceTest"] = NotMovedYet,
            ["PortfolioBlockedHistoryAcceptanceTest"] = NotMovedYet,
            ["PortfolioConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["PortfolioDeleteSerialisationTests"] = NotMovedYet,
            ["PortfolioStalenessThresholdSettingsIntegrationTest"] = NotMovedYet,
            ["PortfolioTimeInStateReadApiIntegrationTest"] = NotMovedYet,
            ["QuietWriteBackAcceptanceTest"] = NotMovedYet,
            ["RbacGroupMappingConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["RecentProblemsCapacityConfigurationTest"] = NotMovedYet,
            ["ReconstructOverTimeHistoryAcceptanceTest"] = NotMovedYet,
            ["RecurringBlackoutRulesByDateForecastIntegrationTest"] = NotMovedYet,
            ["RecurringBlackoutRulesChartOverlayIntegrationTest"] = NotMovedYet,
            ["RecurringBlackoutRulesDeliveryIntegrationTest"] = NotMovedYet,
            ["RecurringBlackoutRulesDownstreamParityIntegrationTest"] = NotMovedYet,
            ["RecurringBlackoutRulesIntervalRuleIntegrationTest"] = NotMovedYet,
            ["RecurringBlackoutRulesLifecycleIntegrationTest"] = NotMovedYet,
            ["RecurringBlackoutRulesWeekendsForeverIntegrationTest"] = NotMovedYet,
            ["RecurringBlackoutRulesWriteBackIntegrationTest"] = NotMovedYet,
            ["ReportCreationTimingProbe"] = NotMovedYet,
            ["RunChartPayloadContractIntegrationTest"] = NotMovedYet,
            ["S13_ViewerEmbedHandshakeTests"] = NotMovedYet,
            ["S14_ViewerEmbedSubjectValidatorTests"] = NotMovedYet,
            ["S15_EmbedSurfaceOptInTests"] = NotMovedYet,
            ["S1_AllowedOriginsEnvVarBindingTests"] = NotMovedYet,
            ["S1_CorsFailClosedTests"] = NotMovedYet,
            ["S3_UpdateNotificationHubAuthorizeTests"] = NotMovedYet,
            ["S4_DeliveriesDeleteGuardInversionTests"] = NotMovedYet,
            ["S5_ApiKeyScopesTests"] = NotMovedYet,
            ["S6_RateLimitingTests"] = NotMovedYet,
            ["S8_EmbedEntryPointTests"] = NotMovedYet,
            ["S9_EmbedCookiePolicyTests"] = NotMovedYet,
            ["S9b_EmbedCookieConcurrencyTests"] = NotMovedYet,
            ["ScalabilityTests"] = NotMovedYet,
            ["ServiceNowConnectionAcceptanceTest"] = NotMovedYet,
            ["ServiceProviderValidationTest"] = NotMovedYet,
            ["ShutdownIntegrationTest"] = NotMovedYet,
            ["SleRiskAcceptanceTest"] = NotMovedYet,
            ["Slice00OneForecastPerBatchScenarios"] = NotMovedYet,
            ["Slice01ForecastRoundsRunOnTheirOwnThreadsScenarios"] = NotMovedYet,
            ["Slice01JiraDependenciesFromANamedLinkScenarios"] = NotMovedYet,
            ["Slice01NarrowToFeaturesTest"] = NotMovedYet,
            ["Slice02NamedLinkThatMatchedNothingScenarios"] = NotMovedYet,
            ["Slice02OldestLinkWinsTest"] = NotMovedYet,
            ["Slice02SeeWhatIsRunningTest"] = NotMovedYet,
            ["Slice03AmbiguityRefusalTest"] = NotMovedYet,
            ["Slice03AzureDevOpsRelationTypeAcceptedScenarios"] = NotMovedYet,
            ["Slice03PortfolioOldestLinkTest"] = NotMovedYet,
            ["Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios"] = NotMovedYet,
            ["Slice04StopARefreshTest"] = NotMovedYet,
            ["Slice05AnyBrokenCredentialSaysSoTest"] = NotMovedYet,
            ["Slice05CancelAllScenarios"] = NotMovedYet,
            ["Slice06TheWarningsWithoutTheLogTest"] = NotMovedYet,
            ["Slice08HealthThatChecksItselfTest"] = NotMovedYet,
            ["SnapshotRecordedDayInstanceZoneTest"] = NotMovedYet,
            ["StaleWorkItemsAcceptanceTest"] = NotMovedYet,
            ["SystemInfoAuthVisibilityCrossLayerTest"] = NotMovedYet,
            ["SystemInfoWireFormatRegressionTests"] = NotMovedYet,
            ["TaskManagerAcceptanceTest"] = NotMovedYet,
            ["TeamConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["TeamDeleteSerialisationTests"] = NotMovedYet,
            ["TeamFeatureSizeAcceptanceTest"] = NotMovedYet,
            ["TeamStalenessThresholdSettingsIntegrationTest"] = NotMovedYet,
            ["TeamThroughputHistoryValidationIntegrationTest"] = NotMovedYet,
            ["TestClockBoundaryTest"] = NotMovedYet,
            ["TestHostIsReleasedAfterDisposeTest"] = ChecksTheTestHostItself,
            ["TestHostsRunWithoutRateLimitingByDefaultTest"] = ChecksTheTestHostItself,
            ["TestWebApplicationFactoryTest"] = ChecksTheTestHostItself,
            ["TimeInStateReadApiIntegrationTest"] = NotMovedYet,
            ["UsageDataCollectorAddressTests"] = NotMovedYet,
            ["UsageDataCollectorObservationTest"] = NotMovedYet,
            ["UsageDataConsentRateLimitTests"] = NotMovedYet,
            ["UsageDataEventCeilingTests"] = NotMovedYet,
            ["ViewerEmbedSessionJourneyTests"] = NotMovedYet,
            ["W1_VersionEndpointAuthorizationTests"] = NotMovedYet,
            ["WorkItemEstimateApiIntegrationTest"] = NotMovedYet,
            ["WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest"] = NotMovedYet,
            ["WriteBackMappingForNewAdditionalFieldIntegrationTest"] = NotMovedYet,
        };

        private ModuleDefinition module = null!;
        private HostBuildingScan scan = null!;
        private IReadOnlyCollection<string> typesFoundBuildingAHostPerTest = [];

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestHostPerFixtureGuardTest).Assembly);
            scan = new HostBuildingScan(module);
            typesFoundBuildingAHostPerTest = scan.OffendingRoots(root => TopLevelTypeOf(root.DeclaringType) != typeof(TestHostPerFixtureGuardTest).Name)
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
            => type.DeclaringType is null ? type.Name : TopLevelTypeOf(type.DeclaringType);

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

                while (allTypes.Any(type => type.BaseType is not null && factories.Contains(type.BaseType.GetElementType().FullName) && factories.Add(type.FullName)))
                {
                    // Repeats until a type that derives from a factory that derives from a factory has been found too.
                }

                return factories;
            }

            private bool IsAFactoryType(TypeReference type)
                => NamesAFactory(type) || factoryTypeNames.Contains(type.GetElementType().FullName);

            private bool IsAHostStart(Instruction instruction)
                => instruction.Operand is MethodReference called
                    && (instruction.OpCode.Code == Code.Newobj
                        ? IsAFactoryType(called.DeclaringType)
                        : instruction.OpCode.Code is Code.Call or Code.Callvirt && called.Name is "WithWebHostBuilder" or "WithTestAuthentication");

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

        // Fixtures that exist only so the scan can be pointed at something. They are internal and carry no
        // [TestFixture], so NUnit never runs them; the scan skips them when it reads the real assembly.
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
