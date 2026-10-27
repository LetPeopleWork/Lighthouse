using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The rules that keep the shared kernel (Models) free of the application core, each defined once so the
    /// real code and a made-up set of types are checked by the same rule.
    /// </summary>
    /// <remarks>
    /// The rules read the compiled IL rather than ArchUnitNET's dependency graph, because in a Release build,
    /// which is what CI tests, that graph drops what an async method uses: a Model reaching into Services only
    /// inside an async method would pass unseen.
    /// </remarks>
    internal static class KernelRules
    {
        public const string TheModels = @"^Lighthouse\.Backend\.Models(\.|$)";

        public const string TheServices = @"^Lighthouse\.Backend\.Services(\.|$)";

        public const string TheUpdateQueue = @"^Lighthouse\.Backend\.Services\.Implementation\.BackgroundServices\.Update(\.|$)";

        /// <summary>No type in the Models reaches a type in Services.</summary>
        public static KernelRuleVerdict ModelsDoNotDependOnServices(ModuleDefinition module, string models = TheModels, string services = TheServices)
            => Verdict(
                module,
                models,
                services,
                "the Models are the shared kernel every other part builds on. A Model that reaches into " +
                "Services makes the kernel depend on the very code that depends on it, so it cannot be " +
                "understood or tested without the application core. What a Model needs from Services " +
                "belongs in the Models, or the Services side should depend on the Model instead.");

        /// <summary>No type in the Models reaches a type in the update queue's namespace, not even its enum.</summary>
        public static KernelRuleVerdict ModelsDoNotDependOnTheUpdateQueue(ModuleDefinition module, string models = TheModels, string updateQueue = TheUpdateQueue)
            => Verdict(
                module,
                models,
                updateQueue,
                "stopping a forecast is the update's business: it hands the forecast a token to stop on. A " +
                "Model that reads the update queue's own state, or even names one of its types, decides " +
                "things by which update it runs under, which nothing testing the Model on its own would " +
                "ever see.");

        private static KernelRuleVerdict Verdict(ModuleDefinition module, string models, string forbidden, string because)
            => new(
                IlDependencies.TypesIn(module, models),
                IlDependencies.DependentsIn(module, models, type => IlDependencies.ResidesInNamespaceMatching(type, forbidden)),
                $"Types in namespaces matching \"{models}\" should not depend on any type in namespaces matching \"{forbidden}\", because {because}");
    }

    /// <summary>What a kernel rule looked at, and which of those types broke it.</summary>
    internal sealed record KernelRuleVerdict(IReadOnlyList<string> Examined, IReadOnlyList<IlDependent> Offenders, string Rule)
    {
        public void Check()
        {
            if (Offenders.Count == 0)
            {
                return;
            }

            var offenders = Offenders.Select(offender => $"{offender.TypeName} depends on {string.Join(", ", offender.Targets)}");

            Assert.Fail($"{Rule}{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
        }
    }
}
