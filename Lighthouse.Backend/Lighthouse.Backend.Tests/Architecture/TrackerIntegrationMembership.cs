namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The types that make up Tracker integration, measured from the compiled code, by name without their namespace so
    /// the same lists hold before and after the move into Modules/TrackerIntegration. Moves holds every type that goes
    /// into the module, Stays every type of the context's namespaces kept where it is, with the context that keeps it,
    /// and ModelsTypesThatMove the shared Models types the context may take with it.
    /// </summary>
    internal static class TrackerIntegrationMembership
    {
        public static IReadOnlyList<string> Moves { get; } =
        [
            "AMadeUpConnectorNobodyWrote",
        ];

        public static IReadOnlyDictionary<string, string> Stays { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public static IReadOnlyList<string> ModelsTypesThatMove { get; } =
        [
            "AMadeUpModelsValueNobodyWrote",
        ];
    }
}
