using Lighthouse.Backend.Tests.TestHelpers;
using Mono.Cecil;
using static Lighthouse.Backend.Tests.Architecture.TestHostScanning;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The fast test loop leaves out what sits on <see cref="IntegrationFixture"/>, so a fixture that starts an
    /// application host belongs on that base. This guard reads the compiled test assembly and judges every concrete
    /// class with tests, nested ones included: anything such a class runs, in its set-up, tear-down, constructors,
    /// field initialisers or tests, on the class or on a base class, counts as the class's own.
    /// </summary>
    [TestFixture]
    public class TestTierGuardTest
    {
        private const string StartsOneHostForTheFixture = "starts one host for the whole fixture and has not moved to the base yet";
        private const string StartsAHostInItsTests = "starts a host in its tests or its per-test set-up and has not moved to the base yet";
        private const string InheritsItsHostFromTheUsageDataRoot = "takes its host from UsageDataCollectorObservationTest, which has not moved to the base yet; one line on that class moves all of these";
        private const string StartsAHostBesideAContainer = "runs against a Postgres or Redis container, starts a host of its own and has not moved to the base yet";
        private const string ChecksTheTestHostItself = "tests how the test host starts and stops and has not moved to the base yet";

        // Keys are full type names. Moving a class to the base removes its line in the same commit; nothing is added here.
        private static readonly IReadOnlyDictionary<string, string> HostedFixturesNotYetOnTheBase = new Dictionary<string, string>
        {
            ["Lighthouse.Backend.Tests.API.ForecastControllerCalendarDayAnchorTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesPortfolioReadApiIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryReadStabilityIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryStaleAggregateRaceIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ArchivedDeliveryWriteRefusalIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedStalenessThresholdValidationTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.CategoryListsHoldRawStatesSettingsIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ConnectionSecretStatePayloadTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.ConnectionValidationUnreadableSecretTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.CumulativeStateTimePortfolioReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.CumulativeStateTimeReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionOverAMappingIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionSettingsIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.CycleTimeDefinitionValidityIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.DeliveryArchiveClosurePinIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.DeliveryFeatureSelectionScopeIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.DeliveryMetricsHistoryReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting.Slice00OneForecastPerBatchScenarios"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.EncryptionControllerTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.FeatureMoveAuthOffIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.FlowEfficiencyPortfolioReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.FlowEfficiencyReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ForecastFilterTeamSettingsIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ForecastFilterThroughputChartIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ForwardedHeadersIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.HealthCheckIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.InstanceDayForecastWindowIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.JwtBearerAuthIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.McpInboundAuthIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.MetricsEndpointIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeCumulativeScopeIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimePortfolioIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeTrendInfoApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.OAuthCallbackCsrfIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.OAuthControllerIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.OAuthProviderAbstractionIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.OrderedParentSources.ParentSourcesSettingsApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks.Slice03AmbiguityRefusalTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioConcurrencyTokenIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioDeleteSerialisationTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioOwningTeamSaveIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioStalenessThresholdSettingsIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.PortfolioTimeInStateReadApiIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.RbacGroupMappingConcurrencyTokenIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesChartOverlayIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.RecurringBlackoutRulesWriteBackIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.RunChartPayloadContractIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.ServiceNowConnectionAcceptanceTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.ShutdownIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.SystemInfoAuthVisibilityCrossLayerTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.SystemInfoWireFormatRegressionTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.TeamConcurrencyTokenIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.TeamDeleteSerialisationTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.TeamStalenessThresholdSettingsIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.TeamThroughputHistoryValidationIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.TimeInStateReadApiIntegrationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Integration.ViewerEmbedSessionJourneyTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.WorkItemAgePercentilesPortfolioReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.WorkItemAgePercentilesReadApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.WorkItemEstimateApiIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Integration.WriteBackMappingForNewAdditionalFieldIntegrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.DeleteUnitRightsTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.F_BE_1_GroupSnapshotInheritanceTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S13_ViewerEmbedHandshakeTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S14_ViewerEmbedSubjectValidatorTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S15_EmbedSurfaceOptInTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S16_ValidateEndpointAccessTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S17_LicenseDetailsTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S18_ApiKeyCreationScopeTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S1_AllowedOriginsEnvVarBindingTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Security.S1_CorsFailClosedTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.API.Security.S2_ConnectionListPayloadShapeTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S5_ApiKeyScopesTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S6_RateLimitingTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S8_EmbedEntryPointTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S9_EmbedCookiePolicyTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.S9b_EmbedCookieConcurrencyTests"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.API.Security.W1_VersionEndpointAuthorizationTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Architecture.OptionalFeatureApplierSeamArchUnitTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.Architecture.TestHostIsReleasedAfterDisposeTest"] = ChecksTheTestHostItself,
            ["Lighthouse.Backend.Tests.Architecture.TestHostsRunWithoutRateLimitingByDefaultTest"] = ChecksTheTestHostItself,
            ["Lighthouse.Backend.Tests.Health.DataProtectionKeyRingTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Integration.Containers.EmbedSessionSingleUseConcurrencyTests"] = StartsAHostBesideAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ScalabilityTests"] = StartsAHostBesideAContainer,
            ["Lighthouse.Backend.Tests.Integration.UsageData.BulkReportsUsageEventTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.OptionalFeatureToggledEventTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.ParentSourcesUsageEventTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.ReportPdfExportUsageEventTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.ReportUsageEventsTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.Slice04ProductEventsTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.Story6193EveryEventSaysItsSourceTest"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.Story6404FeaturesHeldUsageEventTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.TeamForecastRealityCheckRunEventTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.TeamRefinementNeedUsageEventsTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.TeamRefinementUsageEventsTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.TeamSizingUsageEventsTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataCollectorAddressTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataConsentRateLimitTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataEventCeilingTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataEventPipeTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.UsageDataVetoEmitTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.Integration.UsageData.WorkTrackingSystemFieldsChangedUsageEventTests"] = InheritsItsHostFromTheUsageDataRoot,
            ["Lighthouse.Backend.Tests.LighthouseAppContextConcurrencyTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.DeliveryMetricSnapshotRecordingHandlerTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.Services.Implementation.DomainEvents.SnapshotRecordedDayInstanceZoneTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Services.Implementation.LighthouseClockTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Services.Implementation.OAuth.OAuthProviderRegistryTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Services.Implementation.OAuth.OAuthStateSecretPersistenceTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Services.Implementation.Repositories.FeatureListReaderRegistrationTest"] = StartsOneHostForTheFixture,
            ["Lighthouse.Backend.Tests.Startup.EncryptionBootstrapOrderTests"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Startup.RecentProblemsCapacityConfigurationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.Startup.ServiceProviderValidationTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.TestClockBoundaryTest"] = StartsAHostInItsTests,
            ["Lighthouse.Backend.Tests.TestHelpers.TestWebApplicationFactoryTest"] = ChecksTheTestHostItself,
        };

        private static readonly HashSet<string> TestAttributes =
        [
            "NUnit.Framework.TestAttribute",
            "NUnit.Framework.TestCaseAttribute",
            "NUnit.Framework.TestCaseSourceAttribute",
            "NUnit.Framework.TheoryAttribute",
        ];

        private static readonly HashSet<string> SetUpAndTearDownAttributes =
        [
            "NUnit.Framework.OneTimeSetUpAttribute",
            "NUnit.Framework.OneTimeTearDownAttribute",
            "NUnit.Framework.SetUpAttribute",
            "NUnit.Framework.TearDownAttribute",
        ];

        private ModuleDefinition module = null!;
        private HostBuildingScan scan = null!;
        private List<TypeDefinition> allTypes = [];
        private IReadOnlyList<string> hostedFixturesOffTheBase = [];

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestTierGuardTest).Assembly);
            scan = new HostBuildingScan(module, RunsWhenAFixtureRuns);
            allTypes = [.. module.Types.SelectMany(TypeAndItsNestedTypes)];
            hostedFixturesOffTheBase = HostedAndOffTheBase(allTypes.Where(IsAConcreteClassWithTests));
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [Test]
        public void EveryFixtureThatStartsAHostIsOnTheBaseOrOnTheList()
        {
            var newcomers = Compare(HostedFixturesNotYetOnTheBase.Keys, hostedFixturesOffTheBase).Newcomers;

            Assert.That(newcomers, Is.Empty, MustDeriveFromTheBase(newcomers));
        }

        [Test]
        public void EveryTypeOnTheListIsStillAHostedFixtureOffTheBase()
        {
            var stale = Compare(HostedFixturesNotYetOnTheBase.Keys, hostedFixturesOffTheBase).Stale;

            Assert.That(
                stale,
                Is.Empty,
                "These types are on the list but are not hosted fixtures off the base any more: " + string.Join(", ", stale) +
                ". Remove them from the list, so it can only shrink.");
        }

        [TestCase(nameof(PlantedFixtures.HostInOneTimeSetUp), true)]
        [TestCase(nameof(PlantedFixtures.HostInAnAsyncOneTimeSetUpAfterAnAwait), true)]
        [TestCase(nameof(PlantedFixtures.HostInAConstructor), true)]
        [TestCase(nameof(PlantedFixtures.HostInAFieldInitialiser), true)]
        [TestCase(nameof(PlantedFixtures.HostInABaseClassSetUp), true)]
        [TestCase(nameof(PlantedFixtures.HostThroughAHelper), true)]
        [TestCase(nameof(PlantedFixtures.OnTheBaseWithAHost), false)]
        [TestCase(nameof(PlantedFixtures.RegularWithNoHost), false)]
        public void AFixtureOffTheBaseIsFoundHoweverItReachesAHost(string plantedType, bool shouldBeFound)
        {
            var found = HostedAndOffTheBase([Planted(plantedType)]);

            Assert.That(found, shouldBeFound ? Is.EqualTo(new[] { Planted(plantedType).FullName }) : Is.Empty);
        }

        [Test]
        public void AListedTypeNowOnTheBaseIsStale()
        {
            var nowOnTheBase = Planted(nameof(PlantedFixtures.OnTheBaseWithAHost));
            var stillOffTheBase = Planted(nameof(PlantedFixtures.HostInOneTimeSetUp));

            var comparison = Compare(
                [nowOnTheBase.FullName, stillOffTheBase.FullName],
                HostedAndOffTheBase([nowOnTheBase, stillOffTheBase]));

            Assert.That(comparison.Stale, Is.EqualTo(new[] { nowOnTheBase.FullName }));
        }

        [Test]
        public void TheMessageNamesTheTypeAndTheBaseItMustDeriveFrom()
        {
            var message = MustDeriveFromTheBase(["Some.Namespace.SomeFixture"]);

            Assert.That(message, Does.Contain("Some.Namespace.SomeFixture").And.Contain("must derive from IntegrationFixture"));
        }

        private static bool RunsWhenAFixtureRuns(MethodDefinition method)
            => method.IsConstructor || HasAnAttribute(method, TestAttributes) || HasAnAttribute(method, SetUpAndTearDownAttributes);

        private static bool HasAnAttribute(MethodDefinition method, HashSet<string> attributeNames)
            => method.CustomAttributes.Any(attribute => attributeNames.Contains(attribute.AttributeType.FullName));

        private static string MustDeriveFromTheBase(IEnumerable<string> typeNames)
            => "These fixtures start an application host, so the fast test loop would run them. Each must derive from " +
                nameof(IntegrationFixture) + ": " + string.Join(", ", typeNames) + ".";

        private bool IsAConcreteClassWithTests(TypeDefinition type)
            => type is { IsClass: true, IsAbstract: false }
                && !type.Name.StartsWith('<')
                && scan.TypeAndItsBases(type).Any(self => self.Methods.Any(method => HasAnAttribute(method, TestAttributes)));

        private List<string> HostedAndOffTheBase(IEnumerable<TypeDefinition> fixtures)
            => [.. fixtures.Where(IsHostedOffTheBase).Select(fixture => fixture.FullName).Order(StringComparer.Ordinal)];

        private bool IsHostedOffTheBase(TypeDefinition fixture)
            => !scan.DerivesFrom(fixture, typeof(IntegrationFixture).FullName!) && scan.FixtureReachesAHost(fixture);

        private TypeDefinition Planted(string name)
            => allTypes.Single(type => type.Name == name && type.DeclaringType?.Name == nameof(PlantedFixtures));

        // Fixtures that exist only so the rules can be pointed at something. They are abstract because NUnit runs
        // any concrete class with a [Test] method, and the real pass looks only at concrete classes. None starts its
        // host in a test or a per-test set-up: the guard that lists hosts started per test would read them as real.
        internal static class PlantedFixtures
        {
            internal abstract class HostInOneTimeSetUp
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class HostInAnAsyncOneTimeSetUpAfterAnAwait
            {
                [OneTimeSetUp]
                public async Task StartTheHost()
                {
                    await Task.Yield();
                    _ = new TestWebApplicationFactory<Program>();
                }
            }

            internal abstract class HostInAConstructor
            {
                protected HostInAConstructor() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class HostInAFieldInitialiser
            {
                private readonly object host = new TestWebApplicationFactory<Program>();

                public override string ToString() => host.GetType().Name;
            }

            internal abstract class ParentThatStartsAHost
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class HostInABaseClassSetUp : ParentThatStartsAHost
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class HostThroughAHelper
            {
                [OneTimeSetUp]
                public void Prepare() => BuildTheHost();

                private static TestWebApplicationFactory<Program> BuildTheHost() => new();
            }

            internal abstract class OnTheBaseWithAHost : IntegrationFixture
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class RegularWithNoHost
            {
                [Test]
                public void Add() => Assert.Pass();
            }
        }
    }
}
