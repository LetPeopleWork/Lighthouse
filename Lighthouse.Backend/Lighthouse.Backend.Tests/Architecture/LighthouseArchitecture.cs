using ArchUnitNET.Loader;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Lighthouse.Backend.Tests.Architecture
{
    internal static class LighthouseArchitecture
    {
        public static ArchitectureModel Production { get; } = new ArchLoader()
            .LoadAssemblies(typeof(BaseMetricsService).Assembly)
            .Build();
    }
}
