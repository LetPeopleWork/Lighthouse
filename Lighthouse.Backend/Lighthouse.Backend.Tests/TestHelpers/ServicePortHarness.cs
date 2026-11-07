using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What a scenario needs to drive an application service with no web host: a database of its own, built and
    /// connected the way the host's is so that foreign keys and file behaviour match, and a container holding only
    /// the context, logging, the clock and what the fixture registers. It seeds nothing and holds no static state,
    /// so fixtures stay parallel. A fixture creates one in its set-up and disposes it in its tear-down.
    /// </summary>
    public sealed class ServicePortHarness : IDisposable
    {
        private readonly ServiceProvider services;

        private readonly List<IServiceScope> scopesHandedOut = [];

        private bool disposed;

        public ServicePortHarness(Action<IServiceCollection>? registerWhatThePortNeeds = null)
        {
            DatabasePath = Path.Combine(Path.GetTempPath(), $"service-port-{Guid.NewGuid():N}.db");
            services = ComposeTheContainer(registerWhatThePortNeeds);
            TestDatabaseTemplate.CopyTo(DatabasePath);
        }

        public string DatabasePath { get; }

        /// <summary>One step of a scenario. Each scope has its own context, so a step reads what is stored.</summary>
        public IServiceScope NewScope()
        {
            var scope = services.CreateScope();
            scopesHandedOut.Add(scope);
            return scope;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            // A scenario that failed never reached the end of its own scopes, and a database that is still open
            // cannot be deleted on Windows.
            scopesHandedOut.ForEach(scope => scope.Dispose());
            services.Dispose();

            foreach (var file in new[] { DatabasePath, $"{DatabasePath}-wal", $"{DatabasePath}-shm", $"{DatabasePath}-journal" })
            {
                DeleteOnceNothingHoldsIt(file);
            }
        }

        private ServiceProvider ComposeTheContainer(Action<IServiceCollection>? registerWhatThePortNeeds)
        {
            var collection = new ServiceCollection();
            collection.AddLogging();
            collection.AddSingleton<ICryptoService, FakeCryptoService>();
            collection.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(TestToday.FrozenNow));
            collection.AddDbContext<LighthouseAppContext>(options =>
            {
                options.UseSqlite($"DataSource={DatabasePath};Pooling=False", DatabaseConfigurator.ConfigureSqlite);
                options.AddInterceptors(NoWaitingForTheDisk.Instance);
            });
            registerWhatThePortNeeds?.Invoke(collection);

            return collection.BuildServiceProvider();
        }

        // On Windows a file stays held for a moment after its last connection closes, and deleting it then is
        // refused. Linux never reports this. Waiting briefly is enough: the file is being released, not kept.
        private static void DeleteOnceNothingHoldsIt(string file)
        {
            const int attempts = 40;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    File.Delete(file);
                    return;
                }
                catch (IOException) when (attempt < attempts)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
