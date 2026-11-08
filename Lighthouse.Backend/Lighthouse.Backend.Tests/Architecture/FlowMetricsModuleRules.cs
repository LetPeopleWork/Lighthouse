using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The rules that keep a module's layers where they belong, each defined once so the real code and a made-up
    /// set of types are checked by the same rule. The namespaces a rule is about are parameters, so a test can hand
    /// it made-up types in their place, and the next module can reuse the rule with its own names.
    /// </summary>
    /// <remarks>
    /// The rules read the compiled IL rather than ArchUnitNET's dependency graph, because in a Release build,
    /// which is what CI tests, that graph drops what an async method uses.
    /// </remarks>
    internal static class FlowMetricsModuleRules
    {
        public const string FlowMetricsDomainNamespace = "Lighthouse.Backend.Modules.FlowMetrics.Domain";

        public const string FlowMetricsDomain = @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Domain(\.|$)";

        public const string TheBaseLibrary = @"^System(\.|$)";

        /// <summary>
        /// Each named type lives in the layer's namespace, and no type of that name is left behind in the namespace
        /// it moved out of. Types are named as strings, so the rule compiles before and after the move.
        /// </summary>
        public static KernelRuleVerdict TypesLiveInTheirLayer(ModuleDefinition module, string layerNamespace, IReadOnlyCollection<string> typeNames, string formerHome = KernelRules.TheServices)
        {
            var expected = typeNames.Select(name => $"{layerNamespace}.{name}").ToList();

            var leftBehind = IlDependencies.TypesIn(module, formerHome)
                .Where(fullName => typeNames.Contains(SimpleNameOf(fullName), StringComparer.Ordinal))
                .Select(fullName => new IlDependent(fullName, [$"{layerNamespace}.{SimpleNameOf(fullName)}"]))
                .ToList();

            return new KernelRuleVerdict(
                expected,
                leftBehind,
                $"Each of these types should live in namespace \"{layerNamespace}\", and none should be left in namespaces matching \"{formerHome}\", because " +
                "a module shows its layers only when every type it owns sits in the layer the design gives it. A type left " +
                "behind, or a second copy of it, keeps callers on the old home and hides which layer the type belongs to. " +
                "Move the file into the module's folder for that layer and change its namespace line, nothing else.",
                "is left behind instead of moving to")
            {
                MissingSubjects = [.. expected.Where(fullName => module.GetType(fullName) is null)],
            };
        }

        /// <summary>
        /// No type in the Domain reaches anything but its own Domain, the shared kernel and the base library. A
        /// Domain that holds no type at all fails too, so a rule pointed at the wrong namespace cannot pass unseen.
        /// </summary>
        public static KernelRuleVerdict DomainReachesOnlyTheKernelAndTheBaseLibrary(ModuleDefinition module, string domain = FlowMetricsDomain, string kernel = KernelRules.TheModels)
        {
            var examined = IlDependencies.TypesIn(module, domain);

            return new KernelRuleVerdict(
                examined,
                IlDependencies.DependentsIn(module, domain, type => !MayBeReachedFromTheDomain(type, domain, kernel)),
                $"Types in namespaces matching \"{domain}\" should depend only on types in namespaces matching \"{domain}\", " +
                $"\"{kernel}\" or \"{TheBaseLibrary}\", because the Domain is the module's pure calculations: handed values, " +
                "it gives an answer, and nothing else can change that answer. A Domain type that reaches the module's " +
                "Application or Adapters, Services, the data layer, EF Core or Microsoft.Extensions could read a database, a " +
                "setting or a logger while it calculates, and could no longer be tested by handing it values. What it needs " +
                "from there belongs in the Models, or the caller should hand it in as a value.")
            {
                MissingSubjects = examined.Count == 0 ? [$"The Domain, any type in a namespace matching \"{domain}\","] : [],
            };
        }

        private static bool MayBeReachedFromTheDomain(TypeReference type, string domain, string kernel)
            => IsCompilerMade(type)
                || IlDependencies.ResidesInNamespaceMatching(type, domain)
                || IlDependencies.ResidesInNamespaceMatching(type, kernel)
                || IlDependencies.ResidesInNamespaceMatching(type, TheBaseLibrary);

        // A generic parameter has no namespace, and the compiler's own helper types (the one holding the data of
        // an array written out in code, for instance) sit in no namespace under a name starting with '<', which no
        // hand-written type can have.
        private static bool IsCompilerMade(TypeReference type)
            => type is GenericParameter || (IlDependencies.NamespaceOf(type).Length == 0 && OutermostOf(type).Name.StartsWith('<'));

        private static TypeReference OutermostOf(TypeReference type)
            => type.DeclaringType is null ? type : OutermostOf(type.DeclaringType);

        private static string SimpleNameOf(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];
    }
}
