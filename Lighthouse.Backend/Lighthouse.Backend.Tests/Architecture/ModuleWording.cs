namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// How a module's rule texts speak of it: the module's name in words, and who may call its Application, in words.
    /// A failing rule then sends the reader to the module it checked and to the callers that module allows.
    /// </summary>
    internal sealed class ModuleWording(string moduleName, string legalCallers)
    {
        public static readonly ModuleWording FlowMetrics = new("Flow metrics", "the API and the Reports and Refinement modules");

        public string LegalCallers { get; } = legalCallers;

        public string Of(string layer) => TakesABareApostrophe ? $"{moduleName}' {layer}" : $"{moduleName}'s {layer}";

        // A plural such as "Flow metrics" takes a bare apostrophe; a singular noun ending in ss, such as "Access", takes 's.
        private bool TakesABareApostrophe => moduleName.EndsWith('s') && !moduleName.EndsWith("ss", StringComparison.Ordinal);
    }
}
