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
    /// in its tests, set-ups, tear-downs and constructors, on the class or on a base class.
    /// </summary>
    [TestFixture]
    public class TestHostUseGuardTest
    {
        // This class and the classes it nests are exempt. It tests the contract base itself rather than pinning a wire
        // contract, so it lives beside the other test helpers, and its nested fixtures derive from the base outside the
        // contracts namespace for that reason.
        private static readonly string TheContractBasesOwnTest = typeof(WireContractTestTest).FullName!;

        private static readonly string TheContractBase = typeof(WireContractTest).FullName!;

        private static readonly string Planted = KeyOf(typeof(PlantedFixtures));

        private ModuleDefinition module = null!;
        private List<TypeDefinition> plantedTypes = [];

        private HostBuildingScan Scan { get; set; } = null!;

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestHostUseGuardTest).Assembly);
            Scan = new HostBuildingScan(module, RunsWhenAFixtureRuns);
            plantedTypes = [.. module.Types.SelectMany(TypeAndItsNestedTypes).Where(type => type.FullName.StartsWith(Planted + "/", StringComparison.Ordinal))];
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [TestCase(typeof(PlantedFixtures.StartsAHostInOneTimeSetUp))]
        [TestCase(typeof(PlantedFixtures.StartsAHostInAConstructor))]
        [TestCase(typeof(PlantedFixtures.StartsAHostInAHelperTheSetUpCalls))]
        [TestCase(typeof(PlantedFixtures.StartsAHostInTestsThatCarryTestCaseOnly))]
        public void A_fixture_that_starts_a_host_without_a_reason_is_named(Type planted)
        {
            Assert.That(HostUsersWithoutAReason(plantedTypes), Does.Contain(KeyOf(planted)));
        }

        [Test]
        public void A_host_from_a_base_class_set_up_names_the_concrete_fixture_and_not_the_base()
        {
            var named = HostUsersWithoutAReason(plantedTypes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Does.Contain(Planted + "/StartsAHostThroughItsBase"));
                Assert.That(named, Does.Not.Contain(Planted + "/AHostingBaseWithNoTests"));
            }
        }

        [Test]
        public void A_nested_fixture_is_named_by_its_outer_and_inner_type()
        {
            Assert.That(HostUsersWithoutAReason(plantedTypes), Does.Contain(Planted + "/AnOuterClass/AnInnerFixtureThatStartsAHost"));
        }

        [Test]
        public void A_copy_of_the_contract_bases_own_test_elsewhere_is_not_exempt()
        {
            Assert.That(HostUsersWithoutAReason(plantedTypes), Does.Contain(Planted + "/WireContractTestTest/OneHostForThreeScenarios"));
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

        /// <summary>
        /// The fixtures among the given types that can start a host and have no reason to: not a wire contract on
        /// the base, and not the contract base's own test or a class nested in it.
        /// </summary>
        private List<string> HostUsersWithoutAReason(IEnumerable<TypeDefinition> types)
            => [.. types.Where(HasTests)
                .Where(type => TopLevelTypeOf(type) != TheContractBasesOwnTest)
                .Where(type => !Scan.DerivesFrom(type, TheContractBase))
                .Where(Scan.FixtureReachesAHost)
                .Select(type => type.FullName)
                .Order(StringComparer.Ordinal)];

        private bool HasTests(TypeDefinition type)
            => Scan.TypeAndItsBases(type).SelectMany(each => each.Methods).Any(method => HasAnAttribute(method, TestAttributes));

        private static string HostNeedsAReason(IReadOnlyList<string> types)
            => $"{types.Count} type(s) start an application host without a reason:{Environment.NewLine}"
                + string.Concat(types.Select(type => $"  {type}{Environment.NewLine}"))
                + "A test may start a host only for a reason. For each type, do one of three things: "
                + "move it to the application-service port (build the service with ServicePortHarness, or with new for up to three collaborators, under Tests/Acceptance/<Feature>/); "
                + "list it as a skeleton with its user flow, if it asserts a business outcome that needs the route or the wiring; "
                + "or make it a wire contract on WireContractTest in Tests/Contracts/. "
                + "Fix the type; do not add it to the list of host users still to move, which only shrinks.";

        private static string TopLevelTypeOf(TypeDefinition type)
            => type.DeclaringType is null ? type.FullName : TopLevelTypeOf(type.DeclaringType);

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

            // The contract base's own test, copied by name and shape but not derived from the base.
            internal static class WireContractTestTest
            {
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
