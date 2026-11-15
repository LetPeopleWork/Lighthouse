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

        public string Of(string layer) => moduleName.EndsWith('s') ? $"{moduleName}' {layer}" : $"{moduleName}'s {layer}";
    }
}
