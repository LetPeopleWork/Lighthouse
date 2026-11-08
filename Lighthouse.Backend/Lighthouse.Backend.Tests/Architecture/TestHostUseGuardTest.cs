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
    public class TestHostUseGuardTest
    {
        // This class and the classes it nests are exempt. It tests the contract base itself rather than pinning a wire
        // contract, so it lives beside the other test helpers, and its nested fixtures derive from the base outside the
        // contracts namespace for that reason.
        internal static readonly string TheContractBasesOwnTest = typeof(WireContractTestTest).FullName!;

        private ModuleDefinition module = null!;

        internal HostBuildingScan Scan { get; private set; } = null!;

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(TestHostUseGuardTest).Assembly);
            Scan = new HostBuildingScan(module, RunsWhenAFixtureRuns);
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();
    }
}
