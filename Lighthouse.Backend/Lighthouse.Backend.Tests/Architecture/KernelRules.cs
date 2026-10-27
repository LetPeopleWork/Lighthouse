using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The rules that keep the shared kernel (Models) free of the application core, each defined once so the
    /// real code and a made-up set of types are checked by the same rule.
    /// </summary>
    internal static class KernelRules
    {
        public const string TheModels = @"^Lighthouse\.Backend\.Models(\.|$)";

        public const string TheServices = @"^Lighthouse\.Backend\.Services(\.|$)";

        public const string TheUpdateQueue = @"^Lighthouse\.Backend\.Services\.Implementation\.BackgroundServices\.Update(\.|$)";

        /// <summary>No type in the Models reaches a type in Services.</summary>
        public static IArchRule ModelsDoNotDependOnServices(string models = TheModels, string services = TheServices)
            => Types().That().ResideInNamespaceMatching(models)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(services))
                .Because(
                    "the Models are the shared kernel every other part builds on. A Model that reaches into " +
                    "Services makes the kernel depend on the very code that depends on it, so it cannot be " +
                    "understood or tested without the application core. What a Model needs from Services " +
                    "belongs in the Models, or the Services side should depend on the Model instead.");

        /// <summary>No type in the Models reaches a type in the update queue's namespace, not even its enum.</summary>
        public static IArchRule ModelsDoNotDependOnTheUpdateQueue(string models = TheModels, string updateQueue = TheUpdateQueue)
            => Types().That().ResideInNamespaceMatching(models)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(updateQueue))
                .Because(
                    "stopping a forecast is the update's business: it hands the forecast a token to stop on. A " +
                    "Model that reads the update queue's own state, or even names one of its types, decides " +
                    "things by which update it runs under, which nothing testing the Model on its own would " +
                    "ever see.");
    }
}
