using Mono.Cecil;

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

        public static string KeyOf(MethodDefinition method)
            => method.DeclaringType.FullName + "::" + method.Name + "/" + method.Parameters.Count;

        public static string KeyOf(MethodReference method)
        {
            var declared = method is GenericInstanceMethod generic ? generic.ElementMethod : method;
            return declared.DeclaringType.GetElementType().FullName + "::" + declared.Name + "/" + declared.Parameters.Count;
        }
    }
}
