using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// The harness with a host of its own for every test. The scenario families outside the Task Manager
    /// folder still stand on it; the Task Manager scenarios themselves use
    /// <see cref="TaskManagerHostPerFixtureAcceptanceTest"/>.
    /// </summary>
    public abstract class TaskManagerAcceptanceTest : TaskManagerHarness
    {
        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();
            ResetTheTestState();
            BuildTheHost();
            GiveTheTestAnEmptySeededDatabase();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Factory.Dispose();
            RootFactory.Dispose();
        }
    }
}
