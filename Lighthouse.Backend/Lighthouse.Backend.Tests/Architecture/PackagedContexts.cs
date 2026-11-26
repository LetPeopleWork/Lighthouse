using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The packaged contexts the compiled code holds: the name of every namespace right under the modules root, once each,
    /// in order, read from the assembly rather than written down, so a rule over every context also sees a context nobody
    /// named yet.
    /// </summary>
    internal static class PackagedContexts
    {
        public static IReadOnlyList<string> InTheAssembly(ModuleDefinition module)
        {
            ArgumentNullException.ThrowIfNull(module);

            var underTheModulesRoot = $"{ContextContractsMembership.TheModulesRoot}.";

            return [.. module.Types
                .Select(type => type.Namespace)
                .Where(name => name.StartsWith(underTheModulesRoot, StringComparison.Ordinal))
                .Select(name => name[underTheModulesRoot.Length..].Split('.')[0])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];
        }
    }
}
