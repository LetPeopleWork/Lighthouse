using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// The whole tear-down a fixture that shares one host runs before it forgets owners and deletes its
    /// database: leave the host the way the next scenario expects to find it. It lives next to
    /// <see cref="SharedHostCleanup"/> rather than in it because the guard that every shared host drains
    /// the update queue follows calls into the test assembly, but does not enter that class.
    /// </summary>
    public static class SharedHostTearDown
    {
        public static async Task LeaveTheHostIdleOrFail(IServiceProvider services)
        {
            // The gate reopens last: letting an operator in is refused for as long as a pass or an update is
            // still running. Draining first is safe while an operator is counted as waiting, because a pass
            // that finds one stands down at once rather than writing, so the drain cannot hang on the gate.
            await SharedHostCleanup.DrainTheHistoryFillerOrFail(services.GetRequiredService<OverTimeHistoryFiller>(), TimeSpan.FromSeconds(60));
            await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(services, TimeSpan.FromSeconds(30));
            SharedHostCleanup.ReopenTheMaintenanceGate(services);
        }
    }
}
