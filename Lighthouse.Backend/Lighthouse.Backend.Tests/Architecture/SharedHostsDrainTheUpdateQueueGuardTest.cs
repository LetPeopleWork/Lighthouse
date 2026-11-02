using Lighthouse.Backend.Tests.TestHelpers;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A fixture that builds its application host once in [OneTimeSetUp] serves every test with it. A test that
    /// starts a refresh leaves work on the host's update queue, and that work would run against the next test's
    /// data unless something waits for the queue to empty. This guard reads the compiled test assembly and fails
    /// for every type that builds a host in [OneTimeSetUp] (itself or through the methods it calls) and has no
    /// [TearDown], its own or inherited, that waits for the queue: through SharedHostCleanup or by asking the
    /// update status store whether work is active. Fixtures whose tests never put work on the queue are listed
    /// below with the reason.
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
            ["Lighthouse.Backend.Tests.API.Integration.Reports.ReportsAcceptanceTest"] = NeverStartsARefresh,
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
            ["Lighthouse.Backend.Tests.Startup.EncryptionBootstrapOrderTests"] = NeverStartsARefresh,
        };

        private static readonly HashSet<string> QueueWaits =
        [
            "SharedHostCleanup::WaitUntilTheQueueIsIdle",
            "SharedHostCleanup::WaitUntilTheQueueIsIdleOrFail",
            "IUpdateStatusStore::HasActiveWork",
        ];

        private static readonly string PlantedPrefix = typeof(PlantedFixtures).FullName!.Replace('+', '/');

        private static readonly string[] FactoryTypeNamePrefixes = ["TestWebApplicationFactory", "WebApplicationFactory"];

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
            var offenders = typesFoundNotDrainingTheQueue.Except(TypesAllowedToKeepAHostWithoutDrainingTheQueue.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

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
            var stale = TypesAllowedToKeepAHostWithoutDrainingTheQueue.Keys.Except(typesFoundNotDrainingTheQueue, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

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
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndAsksTheStatusStore), false)]
        [TestCase(nameof(PlantedFixtures.KeepsAHostAndInheritsTheWait), false)]
        [TestCase(nameof(PlantedFixtures.KeepsNoHost), false)]
        public void TheScanSeesAHostKeptWithoutWaitingForTheQueueHoweverTheTearDownIsWritten(string plantedType, bool shouldBeFound)
        {
            var found = new SharedHostScan(module).TypesNotDrainingTheQueue()
                .Where(name => name.StartsWith(PlantedPrefix, StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);

            Assert.That(found.Contains(PlantedPrefix + "/" + plantedType), Is.EqualTo(shouldBeFound));
        }

        /// <summary>
        /// Finds the types that declare a [OneTimeSetUp] from which a host start can be reached, and keeps those
        /// that have no [TearDown], their own or inherited, from which a wait for the update queue can be reached.
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

            private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
                => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

            private static string KeyOf(MethodDefinition method)
                => method.DeclaringType.FullName + "::" + method.Name + "/" + method.Parameters.Count;

            private static string KeyOf(MethodReference method)
            {
                var declared = method is GenericInstanceMethod generic ? generic.ElementMethod : method;
                return declared.DeclaringType.GetElementType().FullName + "::" + declared.Name + "/" + declared.Parameters.Count;
            }

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

            private static bool IsAQueueWait(Instruction instruction)
                => instruction.Operand is MethodReference called && QueueWaits.Contains(called.DeclaringType.GetElementType().Name + "::" + called.Name);

            private bool ReachesAHostStart(MethodDefinition root) => Reaches(root, IsAHostStart);

            private bool ReachesAQueueWait(MethodDefinition root) => Reaches(root, IsAQueueWait);

            private bool Reaches(MethodDefinition root, Func<Instruction, bool> isTheTarget)
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

                    if (method.HasBody && method.Body.Instructions.Any(isTheTarget))
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
                    ? methodsByKey.GetValueOrDefault(KeyOf(called)) ?? []
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

                private static Task<bool> WaitForTheQueue() => SharedHostCleanup.WaitUntilTheQueueIsIdle(null!, TimeSpan.FromSeconds(1));
            }

            internal abstract class KeepsAHostAndAsksTheStatusStore
            {
                [OneTimeSetUp]
                public void StartTheHost() => _ = new TestWebApplicationFactory<Program>();

                [TearDown]
                public bool WaitForTheQueue() => ((Lighthouse.Backend.Services.Interfaces.Update.IUpdateStatusStore)null!).HasActiveWork();
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
