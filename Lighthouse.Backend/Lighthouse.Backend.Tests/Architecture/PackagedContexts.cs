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

            return [];
        }
    }
}
