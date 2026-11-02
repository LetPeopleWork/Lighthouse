using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// What the guards that read the compiled test assembly share: which types are test-host factories,
    /// how to walk a type with the types nested in it, and how a call is matched to the method it runs.
    /// </summary>
    internal static class TestHostScanning
    {
        private static readonly string[] FactoryTypeNamePrefixes = ["TestWebApplicationFactory", "WebApplicationFactory"];

        public static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        public static bool NamesAFactory(TypeReference type)
            => FactoryTypeNamePrefixes.Any(prefix => type.GetElementType().Name.StartsWith(prefix, StringComparison.Ordinal));

        /// <summary>The full names of the types that are a factory or derive from one, however deep.</summary>
        public static HashSet<string> FactoryTypesIn(List<TypeDefinition> allTypes)
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

        /// <summary>Whether the instruction builds a host: a new factory, or a host derived from one.</summary>
        public static bool IsAHostStart(Instruction instruction, HashSet<string> factoryTypeNames)
            => instruction.Operand is MethodReference called
                && IsAFactoryType(called.DeclaringType, factoryTypeNames)
                && (instruction.OpCode.Code == Code.Newobj
                    || instruction.OpCode.Code is Code.Call or Code.Callvirt && called.Name is "WithWebHostBuilder" or "WithTestAuthentication");

        private static bool IsAFactoryType(TypeReference type, HashSet<string> factoryTypeNames)
            => NamesAFactory(type) || factoryTypeNames.Contains(type.GetElementType().FullName);

        /// <summary>What a list of type names says against the names a scan found: the ones missing from the list, and the ones on it that were not found.</summary>
        public static ListComparison Compare(IEnumerable<string> listed, IEnumerable<string> found)
        {
            var listedNames = listed.ToHashSet(StringComparer.Ordinal);
            var foundNames = found.ToHashSet(StringComparer.Ordinal);

            return new ListComparison(
                [.. foundNames.Except(listedNames).Order(StringComparer.Ordinal)],
                [.. listedNames.Except(foundNames).Order(StringComparer.Ordinal)]);
        }

        public sealed record ListComparison(IReadOnlyList<string> Newcomers, IReadOnlyList<string> Stale);

        public static string KeyOf(MethodDefinition method)
            => method.DeclaringType.FullName + "::" + method.Name + "/" + method.Parameters.Count;

        public static string KeyOf(MethodReference method)
        {
            var declared = method is GenericInstanceMethod generic ? generic.ElementMethod : method;
            return declared.DeclaringType.GetElementType().FullName + "::" + declared.Name + "/" + declared.Parameters.Count;
        }
    }
}
