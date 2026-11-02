using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Lighthouse.Backend.Tests.Architecture.TestHostScanning;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A fixture that builds its application host once in [OneTimeSetUp] serves every test with it. A test that
    /// starts a refresh leaves work on the host's update queue, and that work would run against the next test's
    /// data unless something waits for the queue to empty. This guard reads the compiled test assembly and fails
    /// for every type that builds a host in [OneTimeSetUp] (itself or through the methods it calls) and has no
    /// [TearDown], its own or inherited, that waits for the queue: through SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail,
    /// or by asking the update status store whether work is active inside a loop. A single look at the store, or a
    /// wait whose answer is thrown away, does not hold anything back. Fixtures whose tests never put work on the
    /// queue are listed below with the reason.
    /// </summary>
    [TestFixture]
    public class SharedHostsDrainTheUpdateQueueGuardTest
    {
        private const string NeverStartsARefresh = "its tests never start a refresh, so nothing is left on the update queue";

        // Keys are full type names, so two types with the same simple name in different namespaces stay apart.
        private static readonly IReadOnlyDictionary<string, string> TypesAllowedToKeepAHostWithoutDrainingTheQueue = new Dictionary<string, string>
        {
            ["Lighthouse.Backend.Tests.API.ForecastControllerCalendarDayAnchorTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.AgeInStatePercentilesReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.CumulativeStateTimePortfolioReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.CumulativeStateTimeReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.EncryptionControllerTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures.FastWithManyFeaturesAcceptanceTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.FlowEfficiencyPortfolioReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.FlowEfficiencyReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.InstanceDayForecastWindowIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.JwtBearerAuthIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.McpInboundAuthIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeCumulativeScopeIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimePortfolioIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.NamedCycleTimeTrendInfoApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.Refinement.RefinementAcceptanceTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.ViewerEmbedSessionJourneyTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.WorkItemAgePercentilesPortfolioReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Integration.WorkItemAgePercentilesReadApiIntegrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S13_ViewerEmbedHandshakeTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S14_ViewerEmbedSubjectValidatorTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S15_EmbedSurfaceOptInTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S4_DeliveriesDeleteGuardInversionTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S6_RateLimitingTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S8_EmbedEntryPointTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S9_EmbedCookiePolicyTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.API.Security.S9b_EmbedCookieConcurrencyTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.Architecture.OptionalFeatureApplierSeamArchUnitTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.Architecture.TestHostIsReleasedAfterDisposeTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.Services.Implementation.Repositories.FeatureListReaderRegistrationTest"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.Integration.Containers.CatalogueBodiesOnPostgresTests"] = NeverStartsARefresh,
            ["Lighthouse.Backend.Tests.Startup.EncryptionBootstrapOrderTests"] = NeverStartsARefresh,
        };

        private static readonly string PlantedPrefix = typeof(PlantedFixtures).FullName!.Replace('+', '/');

        private ModuleDefinition module = null!;
        private IReadOnlyList<string> typesFoundNotDrainingTheQueue = [];

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(SharedHostsDrainTheUpdateQueueGuardTest).Assembly);
            typesFoundNotDrainingTheQueue = new SharedHostScan(module).TypesNotDrainingTheQueue()
                .Where(name => !name.Contains("/Planted", StringComparison.Ordinal))
                .ToList();
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [Test]
        public void EveryTypeThatKeepsAHostForItsWholeRunWaitsForTheUpdateQueueAfterEachTest()
        {
            var offenders = Compare(TypesAllowedToKeepAHostWithoutDrainingTheQueue.Keys, typesFoundNotDrainingTheQueue).Newcomers;

            Assert.That(
                offenders,
                Is.Empty,
                "These types build an application host in [OneTimeSetUp] and have no [TearDown] that waits for the update queue to empty: " +
                string.Join(", ", offenders) +
                ". Work a test leaves on the queue would run against the next test's data. Add a [TearDown] that calls " +
                "SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(factory.Services, ...). If its tests never start a refresh, add the type to the list with that reason.");
        }

        [Test]
        public void EveryTypeOnTheListStillKeepsAHostWithoutDrainingTheQueue()
        {
            var stale = Compare(TypesAllowedToKeepAHostWithoutDrainingTheQueue.Keys, typesFoundNotDrainingTheQueue).Stale;

            Assert.That(
                stale,
                Is.Empty,
                "These types are on the list but either no longer build a host in [OneTimeSetUp] or now wait for the queue: " +
                string.Join(", ", stale) + ". Remove them from the list so it cannot hide a new offender.");
        }

        [TestCase(nameof(PlantedFixtures.KeepsAHostAndNeverWaits), true)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostThroughAHelperAndNeverWaits), true)]
        [TestCase(nameof(PlantedFixtures.TearsDownWithoutWaiting), true)]
        [TestCase(nameof(PlantedFixtures.WaitsOnlyInOneTimeTearDown), true)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndWaitsInTearDown), false)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndWaitsThroughAHelper), false)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndAsksTheStatusStore), true)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndDiscardsTheResultOfTheWait), true)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndReadsTheStatusStoreBesideAnUnrelatedLoop), true)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndPollsTheStatusStoreInALoop), false)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndPollsTheStatusStoreInAnAsyncLoop), false)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndInheritsTheWait), false)]
        [TestCase(nameof(PlantedFixtures.KeepsNoHost), false)]
        public void TheScanSeesAHostKeptWithoutWaitingForTheQueueHoweverTheTearDownIsWritten(string plantedType, bool shouldBeFound)
        {
            var found = new SharedHostScan(module).TypesNotDrainingTheQueue()
                .Where(name => name.StartsWith(PlantedPrefix, StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);

            Assert.That(found.Contains(PlantedPrefix + "/" + plantedType), Is.EqualTo(shouldBeFound));
        }

        private static string[] Names(params string[] names) => names;

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

        /// <summary>
        /// Finds the types that declare a [OneTimeSetUp] from which a host start can be reached, and keeps those
        /// that have no [TearDown], their own or inherited, from which a wait that holds can be reached: a call to
        /// SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail, or a look at the update status store inside a loop. The methods
        /// of SharedHostCleanup are not entered, so the loop in its own non-failing wait does not count.
        /// Calls are followed into methods written in the same assembly and into the bodies the compiler makes
        /// for lambdas and async methods.
        /// </summary>
        private sealed class SharedHostScan
        {
            private readonly List<TypeDefinition> allTypes;
            private readonly Dictionary<string, List<MethodDefinition>> methodsByKey;
            private readonly Dictionary<string, TypeDefinition> typesByName;
            private readonly HashSet<string> factoryTypeNames;

            public SharedHostScan(ModuleDefinition module)
            {
                allTypes = [.. module.Types.SelectMany(TypeAndItsNestedTypes)];
                methodsByKey = allTypes.SelectMany(type => type.Methods).GroupBy(KeyOf).ToDictionary(group => group.Key, group => group.ToList());
                typesByName = allTypes.ToDictionary(type => type.FullName, type => type);
                factoryTypeNames = FactoryTypesIn(allTypes);
            }

            public IEnumerable<string> TypesNotDrainingTheQueue()
                => allTypes
                    .Where(type => !type.Name.StartsWith('<'))
                    .Where(type => MethodsWith(type, "NUnit.Framework.OneTimeSetUpAttribute").Any(ReachesAHostStart))
                    .Where(type => !TypeAndItsBases(type).SelectMany(candidate => MethodsWith(candidate, "NUnit.Framework.TearDownAttribute")).Any(ReachesAQueueWait))
                    .Select(type => type.FullName)
                    .Order(StringComparer.Ordinal);

            private static IEnumerable<MethodDefinition> MethodsWith(TypeDefinition type, string attribute)
                => type.Methods.Where(method => method.CustomAttributes.Any(candidate => candidate.AttributeType.FullName == attribute));

            private IEnumerable<TypeDefinition> TypeAndItsBases(TypeDefinition type)
            {
                for (TypeDefinition? current = type; current is not null; current = current.BaseType is null ? null : typesByName.GetValueOrDefault(current.BaseType.GetElementType().FullName))
                {
                    yield return current;
                }
            }

            private static bool IsTheFailingWait(Instruction instruction)
                => instruction.Operand is MethodReference called
                    && called.DeclaringType.GetElementType().Name == nameof(SharedHostCleanup)
                    && called.Name == nameof(SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail);

            private static bool IsALookAtTheStatusStore(Instruction instruction)
                => instruction.Operand is MethodReference called
                    && called.DeclaringType.GetElementType().Name == nameof(IUpdateStatusStore)
                    && called.Name == nameof(IUpdateStatusStore.HasActiveWork);

            // A branch back to a point at or before the look, taken from a point at or after it, puts the look in a loop.
            private static bool IsInALoop(Instruction look, IEnumerable<Instruction> instructions)
                => instructions.Any(branch => branch.Offset >= look.Offset && TargetsOf(branch).Any(target => target.Offset <= look.Offset));

            private static Instruction[] TargetsOf(Instruction branch)
                => branch.Operand switch
                {
                    Instruction target => [target],
                    Instruction[] targets => targets,
                    _ => [],
                };

            private static bool Holds(MethodDefinition method)
                => method.HasBody
                    && (method.Body.Instructions.Any(IsTheFailingWait)
                        || method.Body.Instructions.Where(IsALookAtTheStatusStore).Any(look => IsInALoop(look, method.Body.Instructions)));

            private bool StartsAHost(MethodDefinition method)
                => method.HasBody && method.Body.Instructions.Any(instruction => IsAHostStart(instruction, factoryTypeNames));

            private bool ReachesAHostStart(MethodDefinition root) => Reaches(root, StartsAHost);

            private bool ReachesAQueueWait(MethodDefinition root) => Reaches(root, Holds);

            private bool Reaches(MethodDefinition root, Func<MethodDefinition, bool> isTheTarget)
            {
                var visited = new HashSet<MethodDefinition>();
                var pending = new Stack<MethodDefinition>([root]);

                while (pending.Count > 0)
                {
                    var method = pending.Pop();

                    if (!visited.Add(method))
                    {
                        continue;
                    }

                    if (isTheTarget(method))
                    {
                        return true;
                    }

                    foreach (var next in MethodsRunBy(method))
                    {
                        pending.Push(next);
                    }
                }

                return false;
            }

            private IEnumerable<MethodDefinition> MethodsRunBy(MethodDefinition method)
                => CompilerMadeBodiesOf(method).Concat(method.HasBody ? method.Body.Instructions.SelectMany(MethodsStartedBy) : []);

            // An async method's own body lives in the MoveNext of a type the compiler names "<Method>d__N".
            private static IEnumerable<MethodDefinition> CompilerMadeBodiesOf(MethodDefinition method)
                => method.DeclaringType.NestedTypes
                    .Where(nested => nested.Name.StartsWith("<" + method.Name + ">", StringComparison.Ordinal))
                    .SelectMany(nested => nested.Methods);

            private IEnumerable<MethodDefinition> MethodsStartedBy(Instruction instruction)
                => instruction.Operand is MethodReference called
                    && instruction.OpCode.Code is Code.Call or Code.Callvirt or Code.Newobj or Code.Ldftn or Code.Ldvirtftn
                    ? (methodsByKey.GetValueOrDefault(KeyOf(called)) ?? []).Where(method => method.DeclaringType.Name != nameof(SharedHostCleanup))
                    : [];
        }

        // Fixtures that exist only so the scan can be pointed at something. They are abstract because NUnit runs
        // any class with a [Test] method, and these have nothing to run; the scan skips them when it reads the
        // real assembly.
        internal static class PlantedFixtures
        {
            internal abstract class KeepsAHostAndNeverWaits
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public void ResetWhatDiffersPerTest() => _ = 1 + 1;
            }

            internal abstract class KeepsAHostThroughAHelperAndNeverWaits
            {
                [OneTimeSetUp]
                public void Prepare() => BuildTheHost();

                private static TestWebApplicationFactory<Program> BuildTheHost() => new();
            }

            internal abstract class TearsDownWithoutWaiting
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public void ForgetOwners() => _ = 1 + 1;
            }

            internal abstract class WaitsOnlyInOneTimeTearDown
            {
                private readonly TestWebApplicationFactory<Program> factory = new();

                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [OneTimeTearDown]
                public Task WaitAtTheEnd() => SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(factory.Services, TimeSpan.FromSeconds(1));
            }

            internal abstract class KeepsAHostAndWaitsInTearDown
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public Task WaitForTheQueue() => SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(null!, TimeSpan.FromSeconds(1));
            }

            internal abstract class KeepsAHostAndWaitsThroughAHelper
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public async Task CleanUp()
                {
                    await Task.Yield();
                    await WaitForTheQueue();
                }

                private static Task WaitForTheQueue() => SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(null!, TimeSpan.FromSeconds(1));
            }

            internal abstract class KeepsAHostAndDiscardsTheResultOfTheWait
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public async Task CleanUp() => _ = await SharedHostCleanup.WaitUntilTheQueueIsIdle(null!, TimeSpan.FromSeconds(1));
            }

            internal abstract class KeepsAHostAndReadsTheStatusStoreBesideAnUnrelatedLoop
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public bool CleanUp()
                {
                    for (var count = 0; count < 3; count++)
                    {
                        _ = count + 1;
                    }

                    return ((IUpdateStatusStore)null!).HasActiveWork();
                }
            }

            internal abstract class KeepsAHostAndPollsTheStatusStoreInALoop
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public void CleanUp()
                {
                    while (((IUpdateStatusStore)null!).HasActiveWork())
                    {
                        Thread.Sleep(1);
                    }
                }
            }

            internal abstract class KeepsAHostAndPollsTheStatusStoreInAnAsyncLoop
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public async Task CleanUp()
                {
                    while (((IUpdateStatusStore)null!).HasActiveWork())
                    {
                        await Task.Delay(1);
                    }
                }
            }

            internal abstract class KeepsAHostAndAsksTheStatusStore
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public bool WaitForTheQueue() => ((IUpdateStatusStore)null!).HasActiveWork();
            }

            internal abstract class AnyBaseThatWaits
            {
                [TearDown]
                public Task WaitForTheQueue() => SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(null!, TimeSpan.FromSeconds(1));
            }

            internal abstract class KeepsAHostAndInheritsTheWait : AnyBaseThatWaits
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();
            }

            internal abstract class KeepsNoHost
            {
                [OneTimeSetUp]
                public void PrepareSomethingElse() => _ = 1 + 1;
            }
        }
    }
}
