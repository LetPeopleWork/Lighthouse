using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Services.Implementation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Lighthouse.Backend.Modules.Forecasting.Application.WriteBack;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A write-back reaches the tracker only through the write-back service, which is where a connection whose
    /// OAuth sign-in holds no write consent is stopped. A second caller of the tracker's write would send writes
    /// past that check, so the compiled code is read for every call of it. Implementing the write is not calling it.
    /// </summary>
    [TestFixture]
    public class WriteBackSinglePathArchUnitTest
    {
        private const string TheWrite = nameof(IWriteBackTarget.WriteFieldsToWorkItems);

        private static readonly string TheTarget = typeof(IWriteBackTarget).FullName!;

        private ModuleDefinition production = null!;

        [OneTimeSetUp]
        public void ReadTheProductionCode() => production = IlDependencies.ReadModule(typeof(IWriteBackTarget).Assembly);

        [OneTimeTearDown]
        public void ReleaseTheProductionCode() => production.Dispose();

        [Test]
        public void Only_the_write_back_service_sends_a_write_back_to_the_tracker()
        {
            var callers = TypesThatCallTheWrite();

            Assert.That(callers, Is.EquivalentTo(new[] { typeof(WriteBackService).FullName }),
                $"WHAT: the tracker's write-back is called by [{string.Join(", ", callers)}] among the {production.Types.Count} top-level types of " +
                $"Lighthouse.Backend, expected by {typeof(WriteBackService).FullName} only. WHY: the write-back service is where a connection " +
                "without write consent is stopped, so any other caller writes past that check; an empty list means the call was not found at all. " +
                "HOW: send every write-back through the write-back service instead of calling the tracker's write directly.");
        }

        private List<string> TypesThatCallTheWrite()
            => production.Types
                .SelectMany(TypeAndItsNestedTypes)
                .Where(type => type.Methods.Any(CallsTheWrite))
                .Select(type => IlDependencies.DeclaringTypeOf(type.FullName.Replace('/', '+')))
                .Distinct(StringComparer.Ordinal)
                .ToList();

        private static bool CallsTheWrite(MethodDefinition method)
            => method.HasBody && method.Body.Instructions.Any(instruction =>
                (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                && instruction.Operand is MethodReference called
                && called.Name == TheWrite
                && called.DeclaringType.FullName == TheTarget);

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];
    }
}
