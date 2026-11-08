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
    /// of the two lists of reasons, and only while it still starts a host; the contracts namespace holds the fixtures
    /// on the base and nothing else.
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

        private ModuleDefinition module = null!;
        private List<TypeDefinition> plantedTypes = [];

        private HostBuildingScan Scan { get; set; } = null!;

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestHostUseGuardTest).Assembly);
            Scan = new HostBuildingScan(module, RunsWhenAFixtureRuns);
            plantedTypes = [.. module.Types.SelectMany(TypeAndItsNestedTypes).Where(type => IsInside(type, Planted) || IsInside(type, PlantedInContracts))];
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
            => TopLevelTypeOf(type).Namespace == TheContractsNamespace;

        private static bool IsTheContractBasesOwnTest(TypeDefinition type) => TopLevelTypeOf(type).FullName == TheContractBasesOwnTest;

        private bool HasTests(TypeDefinition type)
            => Scan.TypeAndItsBases(type).SelectMany(each => each.Methods).Any(method => HasAnAttribute(method, TestAttributes));

        private static string HostNeedsAReason(IReadOnlyList<string> types)
            => $"{types.Count} type(s) start an application host without a reason:{Environment.NewLine}"
                + Lines([.. types])
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
                + "Move a fixture on WireContractTest into Lighthouse.Backend.Tests.Contracts, and a fixture there that is not on it out.";

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
