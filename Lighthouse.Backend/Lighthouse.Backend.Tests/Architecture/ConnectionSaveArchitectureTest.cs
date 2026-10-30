using Lighthouse.Backend.API;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    // Read straight from the compiled code rather than through the architecture loader: a Release build moves an
    // async method's body into a nested helper type, and the loader does not see what that helper uses.
    [TestFixture]
    public class ConnectionSaveArchitectureTest
    {
        private static readonly Type[] LeftToTheConnectionSave = [typeof(IDomainEventDispatcher), typeof(FetchFingerprint)];

        [Test]
        public void TheConnectionController_LeavesWhoToUpdateAfterASaveToTheConnectionSave()
        {
            using var module = ModuleDefinition.ReadModule(typeof(WorkTrackingSystemConnectionController).Assembly.Location);
            var controller = module.GetType(typeof(WorkTrackingSystemConnectionController).FullName);
            var forbidden = LeftToTheConnectionSave.Select(type => type.FullName!).ToHashSet(StringComparer.Ordinal);

            var used = TheTypeAndEveryTypeNestedInIt(controller)
                .SelectMany(EveryTypeItNames)
                .Where(forbidden.Contains)
                .Distinct()
                .ToList();

            Assert.That(
                used,
                Is.Empty,
                "which Teams and Portfolios a connection save sets updating is decided in one place, the connection " +
                "save; a controller that compared fetch shapes or published events itself would be a second place " +
                "for that decision to drift");
        }

        private static IEnumerable<TypeDefinition> TheTypeAndEveryTypeNestedInIt(TypeDefinition type)
            => type.NestedTypes.SelectMany(TheTypeAndEveryTypeNestedInIt).Prepend(type);

        private static IEnumerable<string> EveryTypeItNames(TypeDefinition type)
        {
            foreach (var field in type.Fields)
            {
                yield return field.FieldType.FullName;
            }

            foreach (var method in type.Methods)
            {
                foreach (var parameter in method.Parameters)
                {
                    yield return parameter.ParameterType.FullName;
                }

                if (!method.HasBody)
                {
                    continue;
                }

                foreach (var instruction in method.Body.Instructions)
                {
                    var named = instruction.Operand switch
                    {
                        MemberReference { DeclaringType: not null } member when instruction.OpCode.OperandType != OperandType.InlineType => member.DeclaringType.FullName,
                        TypeReference typeReference => typeReference.FullName,
                        _ => null,
                    };

                    if (named is not null)
                    {
                        yield return named;
                    }
                }
            }
        }
    }
}
