using Lighthouse.Backend.Tests.Integration.Containers;
using Lighthouse.Backend.Tests.TestHelpers;
using Mono.Cecil;
using static Lighthouse.Backend.Tests.Architecture.TestHostScanning;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The fast test loop leaves out what sits on <see cref="IntegrationFixture"/>, so a fixture that starts an
    /// application host belongs on that base. This guard reads the compiled test assembly and judges every concrete
    /// class with tests, nested ones included: anything such a class runs, in its set-up, tear-down, constructors,
    /// field initialisers or tests, on the class or on a base class, counts as the class's own. The base must also
    /// stay honest: nothing sits on it without a reason, nothing else writes its category, and a hosted fixture
    /// carries no connector category, because CI selects live tests by that category. Live and container fixtures
    /// belong on the base as well, and a fixture carrying the canary category also carries a connector.
    /// </summary>
    [TestFixture]
    public class TestTierGuardTest
    {
        private const string IntegrationCategory = "Integration";
        private const string CanaryCategory = "UsageDataCanary";
        private const string ContainerNamespace = "Lighthouse.Backend.Tests.Integration.Containers";
        private const string RunsAgainstAContainer = "runs against a Postgres or Redis container and has not moved to the base yet";
        private const string SitsAmongTheContainerTests = "sits in the container namespace, runs on SQLite with no container, and has not moved to the base yet";
        private const string StartsAHostBesideAContainer = "runs against a Postgres or Redis container, starts a host of its own and has not moved to the base yet";

        // Keys are full type names. Moving a class to the base removes its line in the same commit; nothing is added here.
        private static readonly IReadOnlyDictionary<string, string> FixturesNotYetOnTheBase = new Dictionary<string, string>
        {
            ["Lighthouse.Backend.Tests.Integration.Containers.BlockedCountSnapshotMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.BlockedDurationMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.BlockedRuleSetMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.BlockedStalenessThresholdMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.CancelAllMultiReplicaTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.CategoryListNormalisationSeederIsolationTests"] = SitsAmongTheContainerTests,
            ["Lighthouse.Backend.Tests.Integration.Containers.CategoryListNormalisationSeederOnBothProvidersTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ClusterSubstrateHealthCheckTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ConcurrentStartupMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ConnectionDefaultsUpgradeMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.DeliveryMetricSnapshotDayKeyMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.EmbedSessionSingleUseConcurrencyTests"] = StartsAHostBesideAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.FeatureDependencyReferenceMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.FeatureListParityOnPostgresTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ForecastInputFingerprintAcrossProvidersTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ForecastInputsCompareAndSetOnPostgresTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ForecastOnTheLaneIdentityOnPostgresTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.LongSecretRoundTripTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ParentSourcesColumnConversionTests"] = SitsAmongTheContainerTests,
            ["Lighthouse.Backend.Tests.Integration.Containers.ParentSourcesUpgradeMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.PercentilesOverTimeSnapshotMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.PostgresUpdateExecutionLockTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.PredefinedAdditionalFieldMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ProcessBehaviorSnapshotMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ReEncryptionCompareAndSwapProbeTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.RefreshLogSkippedColumnMigrationTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ScalabilityTests"] = StartsAHostBesideAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.SubstrateProbeTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.TaskManagerCancellationMultiReplicaTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.TaskManagerMomentsMultiReplicaTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.TaskManagerMultiReplicaTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.TaskManagerQueueOrderMultiReplicaTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.UpdateStatusStoreContainerTests"] = RunsAgainstAContainer,
            ["Lighthouse.Backend.Tests.Integration.Containers.ViewerEmbedStorageGuaranteeTests"] = RunsAgainstAContainer,
        };

        // A permanent list, not a baseline. Every test of these classes carries the connector category, and only the
        // tests that write to a real tracker are in the integration tier. On the base, the offline tests would count
        // as live too, and CI would stop running them.
        private static readonly IReadOnlyDictionary<string, string> LiveClassesWithOfflineTests = new Dictionary<string, string>
        {
            ["Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps.AzureDevOpsWriteBackTest"] =
                "writes to a real Azure DevOps, but its empty-update and bad-id tests touch no tracker and must run in every CI build",
            ["Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira.JiraWriteBackTest"] =
                "writes to a real Jira, but its empty-update test touches no tracker and must run in every CI build",
        };

        private static readonly HashSet<string> ConnectorCategories = [.. TestSelectionClauseTest.Connectors];

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

        private static readonly string[] PiecesOfTheList = ["a", " b", "c "];
        private static readonly string[] PiecesOfTheShortList = ["a", " b"];

        private ModuleDefinition module = null!;
        private HostBuildingScan scan = null!;
        private List<TypeDefinition> allTypes = [];
        private List<TypeDefinition> concreteFixtures = [];
        private IReadOnlyList<string> hostedFixturesOffTheBase = [];

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestTierGuardTest).Assembly);
            scan = new HostBuildingScan(module, RunsWhenAFixtureRuns);
            allTypes = [.. module.Types.SelectMany(TypeAndItsNestedTypes)];
            concreteFixtures = [.. allTypes.Where(IsAConcreteClassWithTests)];
            hostedFixturesOffTheBase = HostedAndOffTheBase(concreteFixtures);
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [Test]
        public void EveryFixtureThatStartsAHostIsOnTheBaseOrOnTheList()
        {
            var newcomers = Compare(FixturesNotYetOnTheBase.Keys, hostedFixturesOffTheBase).Newcomers;

            Assert.That(newcomers, Is.Empty, MustDeriveFromTheBase(newcomers));
        }

        [Test]
        public void EveryTypeOnTheListStillOwesAMoveToTheBase()
        {
            var stale = Compare(FixturesNotYetOnTheBase.Keys, OwedAMoveToTheBase(concreteFixtures)).Stale;

            Assert.That(
                stale,
                Is.Empty,
                "These types are on the list but no longer owe a move to the base: " + string.Join(", ", stale) +
                ". Remove them from the list, so it can only shrink.");
        }

        [TestCase(nameof(PlantedFixtures.HostInOneTimeSetUp), true)]
        [TestCase(nameof(PlantedFixtures.HostInAnAsyncOneTimeSetUpAfterAnAwait), true)]
        [TestCase(nameof(PlantedFixtures.HostInAConstructor), true)]
        [TestCase(nameof(PlantedFixtures.HostInAFieldInitialiser), true)]
        [TestCase(nameof(PlantedFixtures.HostInABaseClassSetUp), true)]
        [TestCase(nameof(PlantedFixtures.HostThroughAHelper), true)]
        [TestCase(nameof(PlantedFixtures.HostInATestBody), true)]
        [TestCase(nameof(PlantedFixtures.HostInATestCaseBody), true)]
        [TestCase(nameof(PlantedFixtures.HostInATestCaseSourceBody), true)]
        [TestCase(nameof(PlantedFixtures.HostInATheoryBody), true)]
        [TestCase(nameof(PlantedFixtures.HostInASetUp), true)]
        [TestCase(nameof(PlantedFixtures.HostInATearDown), true)]
        [TestCase(nameof(PlantedFixtures.HostInAOneTimeTearDown), true)]
        [TestCase(nameof(PlantedFixtures.HostInAStaticConstructor), true)]
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
                OwedAMoveToTheBase([nowOnTheBase, stillOffTheBase]));

            Assert.That(comparison.Stale, Is.EqualTo(new[] { nowOnTheBase.FullName }));
        }

        [Test]
        public void AListedContainerFixtureNowOnTheBaseIsStale()
        {
            var nowOnTheBase = Planted(nameof(PlantedContainerFixtures.ContainerOnTheBaseWithNoHost));
            var stillOffTheBase = Planted(nameof(PlantedContainerFixtures.ContainerOffTheBase));

            var comparison = Compare(
                [nowOnTheBase.FullName, stillOffTheBase.FullName],
                OwedAMoveToTheBase([nowOnTheBase, stillOffTheBase]));

            Assert.That(comparison.Stale, Is.EqualTo(new[] { nowOnTheBase.FullName }));
        }

        [Test]
        public void TheMessageNamesTheTypeAndTheBaseItMustDeriveFrom()
        {
            var message = MustDeriveFromTheBase(["Some.Namespace.SomeFixture"]);

            Assert.That(message, Does.Contain("Some.Namespace.SomeFixture").And.Contain("must derive from IntegrationFixture"));
        }

        [Test]
        public void NoFixtureSitsOnTheBaseWithoutAReason()
        {
            var stale = OnTheBaseForNoReason(concreteFixtures);

            Assert.That(stale, Is.Empty, MustLeaveTheBase(stale));
        }

        [Test]
        public void NoFixtureWritesTheIntegrationCategoryByHand()
        {
            var places = PlacesThatWriteTheCategory(concreteFixtures);

            Assert.That(places, Is.Empty, MustNotWriteTheCategory(places));
        }

        [Test]
        public void NoHostedFixtureCarriesAConnectorCategory()
        {
            var hosted = HostedWithAConnectorCategory(concreteFixtures);

            Assert.That(hosted, Is.Empty, MustDropTheConnectorCategory(hosted));
        }

        [Test]
        public void EveryLiveOrContainerFixtureIsOnTheBaseOrOnTheList()
        {
            var newcomers = Compare(FixturesNotYetOnTheBase.Keys, LiveOrContainerOffTheBase(concreteFixtures)).Newcomers;

            Assert.That(newcomers, Is.Empty, LiveAndContainerMustDeriveFromTheBase(newcomers));
        }

        [Test]
        public void EveryFixtureCarryingTheCanaryCarriesAConnector()
        {
            var canaries = CanariesWithNoConnector(concreteFixtures);

            Assert.That(canaries, Is.Empty, CanaryMustCarryAConnector(canaries));
        }

        [Test]
        public void EveryLiveClassWithOfflineTestsStillWritesTheCategoryOnAMethod()
        {
            var writers = allTypes.Where(type => type.Methods.Any(WritesTheIntegrationCategory)).Select(type => type.FullName);

            var stale = Compare(LiveClassesWithOfflineTests.Keys, writers).Stale;

            Assert.That(stale, Is.Empty, "These classes no longer write the category on a test: " + string.Join(", ", stale) + ". Remove them from the list.");
        }

        [TestCase(nameof(PlantedFixtures.OnTheBaseWithNoHost), true)]
        [TestCase(nameof(PlantedFixtures.OnTheBaseWithAHost), false)]
        [TestCase(nameof(PlantedFixtures.LiveOnTheBase), false)]
        [TestCase(nameof(PlantedFixtures.LiveThatCallsAMethodNamedLikeAHostSink), false)]
        [TestCase(nameof(PlantedContainerFixtures.ContainerOnTheBaseWithNoHost), false)]
        [TestCase(nameof(PlantedFixtures.RegularWithNoHost), false)]
        public void AFixtureOnTheBaseWithoutAReasonIsFound(string plantedType, bool shouldBeFound)
        {
            var found = OnTheBaseForNoReason([Planted(plantedType)]);

            Assert.That(found, shouldBeFound ? Is.EqualTo(new[] { Planted(plantedType).FullName }) : Is.Empty);
        }

        [TestCase(nameof(PlantedFixtures.CategoryOnTheClass), nameof(PlantedFixtures.CategoryOnTheClass), "")]
        [TestCase(nameof(PlantedFixtures.CategoryInAnotherLetterCase), nameof(PlantedFixtures.CategoryInAnotherLetterCase), "")]
        [TestCase(nameof(PlantedFixtures.CategoryWithSpacesAround), nameof(PlantedFixtures.CategoryWithSpacesAround), "")]
        [TestCase(nameof(PlantedFixtures.ChildOfACategoryOnTheClass), nameof(PlantedFixtures.CategoryOnTheClass), "")]
        [TestCase(nameof(PlantedFixtures.CategoryOnAMethod), nameof(PlantedFixtures.CategoryOnAMethod), "::Serve")]
        [TestCase(nameof(PlantedFixtures.CategoryOnATestCase), nameof(PlantedFixtures.CategoryOnATestCase), "::Serve")]
        public void ACategoryWrittenByHandIsFoundWhereItIsWritten(string plantedType, string writtenOn, string method)
        {
            var found = PlacesThatWriteTheCategory([Planted(plantedType)]);

            Assert.That(found, Is.EqualTo(new[] { Planted(writtenOn).FullName + method }));
        }

        [TestCase(nameof(PlantedFixtures.OnTheBaseWithAHost))]
        [TestCase(nameof(PlantedFixtures.LiveOnTheBase))]
        [TestCase(nameof(PlantedFixtures.CategoryAfterACommaAndASpace))]
        public void TheCategoryOnTheBaseIsNotWrittenByHand(string plantedType)
        {
            Assert.That(PlacesThatWriteTheCategory([Planted(plantedType)]), Is.Empty);
        }

        [TestCase(nameof(PlantedFixtures.HostedWithAConnector), true)]
        [TestCase(nameof(PlantedFixtures.HostedWithAConnectorOnAMethod), true)]
        [TestCase(nameof(PlantedFixtures.OnTheBaseWithAHost), false)]
        [TestCase(nameof(PlantedFixtures.LiveOnTheBase), false)]
        [TestCase(nameof(PlantedFixtures.LiveThatCallsAMethodNamedLikeAHostSink), false)]
        public void AHostedFixtureWithAConnectorCategoryIsFound(string plantedType, bool shouldBeFound)
        {
            var found = HostedWithAConnectorCategory([Planted(plantedType)]);

            Assert.That(found, shouldBeFound ? Is.EqualTo(new[] { Planted(plantedType).FullName }) : Is.Empty);
        }

        [TestCase(nameof(PlantedContainerFixtures.ContainerOffTheBase), true)]
        [TestCase(nameof(PlantedFixtures.ConnectorOffTheBase), true)]
        [TestCase(nameof(PlantedFixtures.ConnectorOnAMethodOffTheBase), true)]
        [TestCase(nameof(PlantedFixtures.ConnectorOnATestCaseOffTheBase), true)]
        [TestCase(nameof(PlantedFixtures.ChildOfAConnectorOffTheBase), true)]
        [TestCase(nameof(PlantedFixtures.ConnectorWithASpaceOffTheBase), true)]
        [TestCase(nameof(PlantedContainerFixtures.ContainerOnTheBaseWithNoHost), false)]
        [TestCase(nameof(PlantedFixtures.LiveOnTheBase), false)]
        [TestCase(nameof(PlantedFixtures.RegularWithNoHost), false)]
        [TestCase(nameof(PlantedFixtures.ConnectorInAnotherLetterCaseOffTheBase), false)]
        [TestCase(nameof(PlantedFixtures.ConnectorAfterACommaAndASpaceOffTheBase), false)]
        public void ALiveOrContainerFixtureOffTheBaseIsFound(string plantedType, bool shouldBeFound)
        {
            var found = LiveOrContainerOffTheBase([Planted(plantedType)]);

            Assert.That(found, shouldBeFound ? Is.EqualTo(new[] { Planted(plantedType).FullName }) : Is.Empty);
        }

        [Test]
        public void ALiveClassNamedForItsOfflineTestsMayStayOffTheBase()
        {
            var named = allTypes.Where(type => LiveClassesWithOfflineTests.ContainsKey(type.FullName)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.Select(type => type.FullName), Is.EquivalentTo(LiveClassesWithOfflineTests.Keys));
                Assert.That(named.Where(type => !CarriesAConnectorCategory(type) || IsOnTheBase(type)), Is.Empty);
                Assert.That(LiveOrContainerOffTheBase(named), Is.Empty);
            }
        }

        [TestCase(nameof(PlantedFixtures.CanaryWithNoConnector), true)]
        [TestCase(nameof(PlantedFixtures.CanaryOnAMethodWithNoConnector), true)]
        [TestCase(nameof(PlantedFixtures.CanaryOnATestCaseWithNoConnector), true)]
        [TestCase(nameof(PlantedFixtures.ChildOfACanaryWithNoConnector), true)]
        [TestCase(nameof(PlantedFixtures.CanaryOnTheBaseWithNoConnector), true)]
        [TestCase(nameof(PlantedFixtures.CanaryWithAConnector), false)]
        [TestCase(nameof(PlantedFixtures.CanaryInAnotherLetterCase), false)]
        [TestCase(nameof(PlantedFixtures.RegularWithNoHost), false)]
        public void ACanaryWithNoConnectorIsFound(string plantedType, bool shouldBeFound)
        {
            var found = CanariesWithNoConnector([Planted(plantedType)]);

            Assert.That(found, shouldBeFound ? Is.EqualTo(new[] { Planted(plantedType).FullName }) : Is.Empty);
        }

        [Test]
        public void TheGuardReadsACategoryTheWayNUnitDoes()
        {
            const string NameWithSpaces = " Integration ";
            const string ListWithSpaces = "a, b,,c ";

            var nameByNUnit = new CategoryAttribute(NameWithSpaces).Name;
            var listByNUnit = new TestCaseAttribute(1) { Category = ListWithSpaces }.Properties[nameof(TestCaseAttribute.Category)];
            var shortListByNUnit = new TestCaseAttribute(1) { Category = "a, b" }.Properties[nameof(TestCaseAttribute.Category)];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nameByNUnit, Is.EqualTo("Integration"));
                Assert.That(listByNUnit, Is.EqualTo(PiecesOfTheList));
                Assert.That(shortListByNUnit, Is.EqualTo(PiecesOfTheShortList));
                Assert.That(CategoryNamedAlone(NameWithSpaces), Is.EqualTo(nameByNUnit));
                Assert.That(CategoriesInAList(ListWithSpaces), Is.EqualTo(listByNUnit));
            }
        }

        [Test]
        public void TheListExcusesAFixtureFromBeingOffTheBaseAndFromNothingElse()
        {
            var fixture = Planted(nameof(PlantedContainerFixtures.ContainerOffTheBaseThatBreaksOtherRules));
            var itsName = new[] { fixture.FullName };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(LiveOrContainerOffTheBase([fixture]), Is.EqualTo(itsName));
                Assert.That(Compare(itsName, LiveOrContainerOffTheBase([fixture])).Newcomers, Is.Empty);
                Assert.That(PlacesThatWriteTheCategory([fixture]), Is.EqualTo(itsName));
                Assert.That(CanariesWithNoConnector([fixture]), Is.EqualTo(itsName));
            }
        }

        [Test]
        public void TheMessageForAStaleMarkerNamesTheTypeAndSaysItMustLeaveTheBase()
        {
            var message = MustLeaveTheBase(["Some.Namespace.SomeFixture"]);

            Assert.That(message, Does.Contain("Some.Namespace.SomeFixture").And.Contain("must leave IntegrationFixture"));
        }

        [Test]
        public void TheMessageForAHandWrittenCategoryNamesThePlaceAndSaysToUseTheBase()
        {
            var message = MustNotWriteTheCategory(["Some.Namespace.SomeFixture::SomeTest"]);

            Assert.That(message, Does.Contain("Some.Namespace.SomeFixture::SomeTest").And.Contain("derive from IntegrationFixture"));
        }

        [Test]
        public void TheMessageForAHostedConnectorSaysCiWouldReadItAsLiveAndDropIt()
        {
            var message = MustDropTheConnectorCategory(["Some.Namespace.SomeFixture"]);

            Assert.That(
                message,
                Does.Contain("Some.Namespace.SomeFixture").And.Contain("CI would read each as live and drop it").And.Contain("Remove the connector category"));
        }

        [Test]
        public void TheMessageForALiveOrContainerFixtureSaysTheFastLoopWouldRunItAndNamesTheBase()
        {
            var message = LiveAndContainerMustDeriveFromTheBase(["Some.Namespace.SomeFixture"]);

            Assert.That(
                message,
                Does.Contain("Some.Namespace.SomeFixture").And.Contain("the fast test loop would run").And.Contain("must derive from IntegrationFixture"));
        }

        [Test]
        public void TheMessageForACanarySaysTheClauseSelectsByExclusionSoItWouldRunInCi()
        {
            var message = CanaryMustCarryAConnector(["Some.Namespace.SomeFixture"]);

            Assert.That(
                message,
                Does.Contain("Some.Namespace.SomeFixture").And.Contain("selects by exclusion").And.Contain("a canary with no connector would run in CI"));
        }

        private static bool RunsWhenAFixtureRuns(MethodDefinition method)
            => method.IsConstructor || HasAnAttribute(method, TestAttributes) || HasAnAttribute(method, SetUpAndTearDownAttributes);

        private static bool HasAnAttribute(MethodDefinition method, HashSet<string> attributeNames)
            => method.CustomAttributes.Any(attribute => attributeNames.Contains(attribute.AttributeType.FullName));

        private static string MustDeriveFromTheBase(IEnumerable<string> typeNames)
            => "These fixtures start an application host, so the fast test loop would run them. Each must derive from " +
                nameof(IntegrationFixture) + ": " + string.Join(", ", typeNames) + ".";

        private static string MustLeaveTheBase(IEnumerable<string> typeNames)
            => "These fixtures start no application host, carry no connector category and are no container tests, so the " +
                "fast test loop leaves them out for no reason. Each must leave " + nameof(IntegrationFixture) + ": " +
                string.Join(", ", typeNames) + ".";

        private static string MustNotWriteTheCategory(IEnumerable<string> places)
            => "These write the Integration category by hand. Delete the attribute and derive from " + nameof(IntegrationFixture) +
                " instead, so the tier comes from one place: " + string.Join(", ", places) + ".";

        private static string MustDropTheConnectorCategory(IEnumerable<string> typeNames)
            => "These fixtures start an application host and carry a connector category, so CI would read each as live and " +
                "drop it from its ordinary runs. Remove the connector category: " + string.Join(", ", typeNames) + ".";

        private static string LiveAndContainerMustDeriveFromTheBase(IEnumerable<string> typeNames)
            => "These live or container fixtures are off the base. Off it, the fast test loop would run a container test, and " +
                "CI would run a live test against its real tracker in every build. Each must derive from " +
                nameof(IntegrationFixture) + ": " + string.Join(", ", typeNames) + ".";

        private static string CanaryMustCarryAConnector(IEnumerable<string> typeNames)
            => "These fixtures carry " + CanaryCategory + " and no connector category. CI's clause selects by exclusion, so " +
                "a canary with no connector would run in CI. Give each its connector category: " + string.Join(", ", typeNames) + ".";

        private bool IsAConcreteClassWithTests(TypeDefinition type)
            => type is { IsClass: true, IsAbstract: false }
                && !type.Name.StartsWith('<')
                && scan.TypeAndItsBases(type).Any(self => self.Methods.Any(method => HasAnAttribute(method, TestAttributes)));

        private static List<string> SortedNames(IEnumerable<TypeDefinition> fixtures)
            => [.. fixtures.Select(fixture => fixture.FullName).Order(StringComparer.Ordinal)];

        private List<string> HostedAndOffTheBase(IEnumerable<TypeDefinition> fixtures)
            => SortedNames(fixtures.Where(IsHostedOffTheBase));

        private bool IsHostedOffTheBase(TypeDefinition fixture)
            => !IsOnTheBase(fixture) && scan.FixtureReachesAHost(fixture);

        private List<string> OnTheBaseForNoReason(IEnumerable<TypeDefinition> fixtures)
            => SortedNames(fixtures.Where(IsOnTheBaseForNoReason));

        private bool IsOnTheBaseForNoReason(TypeDefinition fixture)
            => IsOnTheBase(fixture) && !scan.FixtureReachesAHost(fixture) && !CarriesAConnectorCategory(fixture) && !IsAContainerFixture(fixture);

        private bool IsOnTheBase(TypeDefinition fixture) => scan.DerivesFrom(fixture, typeof(IntegrationFixture).FullName!);

        // A nested type has no namespace of its own in the assembly; its full name still starts with the outer type's.
        private static bool IsAContainerFixture(TypeDefinition fixture)
            => fixture.FullName.StartsWith(ContainerNamespace + ".", StringComparison.Ordinal);

        private List<string> OwedAMoveToTheBase(IReadOnlyCollection<TypeDefinition> fixtures)
            => [.. HostedAndOffTheBase(fixtures).Union(LiveOrContainerOffTheBase(fixtures), StringComparer.Ordinal)];

        private List<string> LiveOrContainerOffTheBase(IEnumerable<TypeDefinition> fixtures)
            => SortedNames(fixtures.Where(IsLiveOrContainerOffTheBase));

        private bool IsLiveOrContainerOffTheBase(TypeDefinition fixture)
            => !IsOnTheBase(fixture)
                && !LiveClassesWithOfflineTests.ContainsKey(fixture.FullName)
                && (CarriesAConnectorCategory(fixture) || IsAContainerFixture(fixture));

        private List<string> CanariesWithNoConnector(IEnumerable<TypeDefinition> fixtures)
            => SortedNames(fixtures.Where(IsACanaryWithNoConnector));

        private bool IsACanaryWithNoConnector(TypeDefinition fixture)
            => CarriesACategory(fixture, category => category == CanaryCategory) && !CarriesAConnectorCategory(fixture);

        private List<string> HostedWithAConnectorCategory(IEnumerable<TypeDefinition> fixtures)
            => SortedNames(fixtures.Where(fixture => scan.FixtureReachesAHost(fixture) && CarriesAConnectorCategory(fixture)));

        private bool CarriesAConnectorCategory(TypeDefinition fixture) => CarriesACategory(fixture, ConnectorCategories.Contains);

        private bool CarriesACategory(TypeDefinition fixture, Func<string, bool> isWanted)
            => scan.TypeAndItsBases(fixture).SelectMany(PlacesACategoryCanBeWritten).SelectMany(CategoriesWrittenOn).Any(isWanted);

        private static IEnumerable<ICustomAttributeProvider> PlacesACategoryCanBeWritten(TypeDefinition type)
            => type.Methods.Cast<ICustomAttributeProvider>().Prepend(type);

        private List<string> PlacesThatWriteTheCategory(IEnumerable<TypeDefinition> fixtures)
            => [.. fixtures
                .SelectMany(scan.TypeAndItsBases)
                .Where(IsNeitherTheBaseNorALiveClassWithOfflineTests)
                .SelectMany(PlacesOnTheType)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];

        private static bool IsNeitherTheBaseNorALiveClassWithOfflineTests(TypeDefinition type)
            => type.FullName != typeof(IntegrationFixture).FullName && !LiveClassesWithOfflineTests.ContainsKey(type.FullName);

        private static IEnumerable<string> PlacesOnTheType(TypeDefinition type)
        {
            var methods = type.Methods.Where(WritesTheIntegrationCategory).Select(method => type.FullName + "::" + method.Name);

            return WritesTheIntegrationCategory(type) ? methods.Prepend(type.FullName) : methods;
        }

        // Test selection matches a category exactly, so "integration" reads like the tier and puts the test in none.
        private static bool WritesTheIntegrationCategory(ICustomAttributeProvider place)
            => CategoriesWrittenOn(place).Any(category => string.Equals(category, IntegrationCategory, StringComparison.OrdinalIgnoreCase));

        private static IEnumerable<string> CategoriesWrittenOn(ICustomAttributeProvider place)
            => place.CustomAttributes.SelectMany(CategoriesIn);

        // [TestCase] and [TestFixture] take categories too, as one comma-separated Category argument.
        private static IEnumerable<string> CategoriesIn(CustomAttribute attribute)
        {
            var listed = attribute.Properties
                .Where(property => property.Name == nameof(TestCaseAttribute.Category))
                .SelectMany(property => CategoriesInAList((string)property.Argument.Value));

            return attribute.AttributeType.FullName == typeof(CategoryAttribute).FullName
                ? listed.Concat(attribute.ConstructorArguments.Select(argument => CategoryNamedAlone((string)argument.Value)))
                : listed;
        }

        private static string CategoryNamedAlone(string name) => name.Trim();

        // NUnit keeps the spaces here, so " b" in "a, b" is a category of its own that a filter on "b" does not select.
        private static string[] CategoriesInAList(string list) => list.Split(',', StringSplitOptions.RemoveEmptyEntries);

        private TypeDefinition Planted(string name)
            => allTypes.Single(type => type.Name == name && type.DeclaringType?.Name is nameof(PlantedFixtures) or nameof(PlantedContainerFixtures));

        // Fixtures that exist only so the rules can be pointed at something. They are abstract because NUnit runs
        // any concrete class with a [Test] method, and the real pass looks only at concrete classes. The guard that
        // lists hosts started per test reads abstract classes too, and leaves this class out by name.
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

            internal abstract class HostInATestBody
            {
                [Test]
                public void Serve()
                {
                    _ = new TestWebApplicationFactory<Program>();
                    Assert.Pass();
                }
            }

            internal abstract class HostInATestCaseBody
            {
                [TestCase(1)]
                public void Serve(int value)
                {
                    _ = new TestWebApplicationFactory<Program>();
                    Assert.That(value, Is.Positive);
                }
            }

            internal abstract class HostInATestCaseSourceBody
            {
                private static readonly int[] Values = [1];

                [TestCaseSource(nameof(Values))]
                public void Serve(int value)
                {
                    _ = new TestWebApplicationFactory<Program>();
                    Assert.That(value, Is.Positive);
                }
            }

            internal abstract class HostInATheoryBody
            {
                [Theory]
                public void Serve(int value)
                {
                    _ = new TestWebApplicationFactory<Program>();
                    Assert.That(value, Is.Positive);
                }
            }

            internal abstract class HostInASetUp
            {
                [SetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class HostInATearDown
            {
                [TearDown]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class HostInAOneTimeTearDown
            {
                [OneTimeTearDown]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class HostInAStaticConstructor
            {
                static HostInAStaticConstructor() => _ = new TestWebApplicationFactory<Program>();
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

            internal abstract class OnTheBaseWithNoHost : IntegrationFixture
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            [Category("JiraIntegration")]
            internal abstract class LiveOnTheBase : IntegrationFixture
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal static class SomethingThatIsNotAHostFactory
            {
                internal static void WithWebHostBuilder() => _ = 1 + 1;
            }

            [Category("JiraIntegration")]
            internal abstract class LiveThatCallsAMethodNamedLikeAHostSink : IntegrationFixture
            {
                [OneTimeSetUp]
                public void Prepare() => SomethingThatIsNotAHostFactory.WithWebHostBuilder();
            }

            [Category("Integration")]
            internal abstract class CategoryOnTheClass
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class ChildOfACategoryOnTheClass : CategoryOnTheClass;

            [Category("integration")]
            internal abstract class CategoryInAnotherLetterCase
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            [Category(" Integration ")]
            internal abstract class CategoryWithSpacesAround
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class CategoryAfterACommaAndASpace
            {
                [TestCase(1, Category = "some-slice, Integration")]
                public void Serve(int value) => Assert.That(value, Is.Positive);
            }

            internal abstract class CategoryOnAMethod
            {
                [Test]
                [Category("Integration")]
                public void Serve() => Assert.Pass();
            }

            internal abstract class CategoryOnATestCase
            {
                [TestCase(1, Category = "some-slice,Integration")]
                public void Serve(int value) => Assert.That(value, Is.Positive);
            }

            [Category("AdoIntegration")]
            internal abstract class HostedWithAConnector : IntegrationFixture
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class HostedWithAConnectorOnAMethod : IntegrationFixture
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [Test]
                [Category("GithubIntegration")]
                public void Serve() => Assert.Pass();
            }

            [Category("LinearIntegration")]
            internal abstract class ConnectorOffTheBase
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class ChildOfAConnectorOffTheBase : ConnectorOffTheBase;

            internal abstract class ConnectorOnAMethodOffTheBase
            {
                [Test]
                [Category("ServiceNowIntegration")]
                public void Serve() => Assert.Pass();
            }

            internal abstract class ConnectorOnATestCaseOffTheBase
            {
                [TestCase(1, Category = "some-slice,AdoIntegration")]
                public void Serve(int value) => Assert.That(value, Is.Positive);
            }

            [Category(" JiraIntegration")]
            internal abstract class ConnectorWithASpaceOffTheBase
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class ConnectorAfterACommaAndASpaceOffTheBase
            {
                [TestCase(1, Category = "some-slice, AdoIntegration")]
                public void Serve(int value) => Assert.That(value, Is.Positive);
            }

            [Category("jiraintegration")]
            internal abstract class ConnectorInAnotherLetterCaseOffTheBase
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            [Category("UsageDataCanary")]
            internal abstract class CanaryWithNoConnector
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            internal abstract class ChildOfACanaryWithNoConnector : CanaryWithNoConnector;

            internal abstract class CanaryOnAMethodWithNoConnector
            {
                [Test]
                [Category("UsageDataCanary")]
                public void Serve() => Assert.Pass();
            }

            internal abstract class CanaryOnATestCaseWithNoConnector
            {
                [TestCase(1, Category = "some-slice,UsageDataCanary")]
                public void Serve(int value) => Assert.That(value, Is.Positive);
            }

            [Category("UsageDataCanary")]
            internal abstract class CanaryOnTheBaseWithNoConnector : IntegrationFixture
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            [Category("UsageDataCanary")]
            [Category("JiraIntegration")]
            internal abstract class CanaryWithAConnector : IntegrationFixture
            {
                [Test]
                public void Serve() => Assert.Pass();
            }

            [Category("usagedatacanary")]
            internal abstract class CanaryInAnotherLetterCase
            {
                [Test]
                public void Serve() => Assert.Pass();
            }
        }
    }
}

// The guard allows a container fixture on the base by its namespace, so this planted one has to live in that namespace.
namespace Lighthouse.Backend.Tests.Integration.Containers
{
    internal static class PlantedContainerFixtures
    {
        internal abstract class ContainerOnTheBaseWithNoHost : IntegrationFixture
        {
            [Test]
            public void Serve() => Assert.Pass();
        }

        internal abstract class ContainerOffTheBase
        {
            [Test]
            public void Serve() => Assert.Pass();
        }

        [Category("Integration")]
        [Category("UsageDataCanary")]
        internal abstract class ContainerOffTheBaseThatBreaksOtherRules
        {
            [Test]
            public void Serve() => Assert.Pass();
        }
    }
}
