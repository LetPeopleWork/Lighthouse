using ArchUnitNET.Fluent;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The rules that keep the shared kernel (Models) free of the application core, each defined once so the
    /// real code and a made-up set of types are checked by the same rule.
    /// </summary>
    // SCAFFOLD: true
    internal static class KernelRules
    {
        public const string TheModels = @"^Lighthouse\.Backend\.Models(\.|$)";

        public const string TheServices = @"^Lighthouse\.Backend\.Services(\.|$)";

        public const string TheUpdateQueue = @"^Lighthouse\.Backend\.Services\.Implementation\.BackgroundServices\.Update(\.|$)";

        /// <summary>No type in the Models reaches a type in Services.</summary>
        public static IArchRule ModelsDoNotDependOnServices(string models = TheModels, string services = TheServices)
            => throw new AssertionException("SCAFFOLD: KernelRules.ModelsDoNotDependOnServices is not built yet");

        /// <summary>No type in the Models reaches a type in the update queue's namespace, not even its enum.</summary>
        public static IArchRule ModelsDoNotDependOnTheUpdateQueue(string models = TheModels, string updateQueue = TheUpdateQueue)
            => throw new AssertionException("SCAFFOLD: KernelRules.ModelsDoNotDependOnTheUpdateQueue is not built yet");
    }
}
