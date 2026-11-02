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
            SharedHostCleanup.ReopenTheMaintenanceGate(services);
            await SharedHostCleanup.DrainTheHistoryFillerOrFail(services.GetRequiredService<OverTimeHistoryFiller>(), TimeSpan.FromSeconds(60));
            await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(services, TimeSpan.FromSeconds(30));
        }
    }
}
