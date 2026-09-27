using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    [TestFixture]
    public abstract class IntegrationTestBase
    {
        private readonly TestWebApplicationFactory<Program> webApplicationFactory;
        private IServiceScope serviceScope;

        protected IntegrationTestBase()
            : this(new TestWebApplicationFactory<Program>())
        {
        }

        protected IntegrationTestBase(TestWebApplicationFactory<Program> webApplicationFactory)
        {
            this.webApplicationFactory = webApplicationFactory;
            Client = webApplicationFactory.CreateClient();
        }

        protected IServiceProvider ServiceProvider { get; private set; }

        protected TestWebApplicationFactory<Program> WebApplicationFactory => webApplicationFactory;

        protected HttpClient Client { get; }

        protected LighthouseAppContext DatabaseContext { get; private set; }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);
            webApplicationFactory.Dispose();
            Client.Dispose();
        }

        [SetUp]
        protected void Init()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);
            webApplicationFactory.UseFreshDatabase();
            serviceScope = webApplicationFactory.Services.CreateScope();
            ServiceProvider = serviceScope.ServiceProvider;

            DatabaseContext = ServiceProvider.GetService<LighthouseAppContext>()
                              ?? throw new InvalidOperationException("Could not Find DB Context");

            DatabaseContext.Database.EnsureCreated();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            // A test that opened the connection itself leaves it open, because Entity Framework only
            // closes the ones it opened. Deleting a file that is still open is allowed on Linux, so CI
            // never sees it, and refused on Windows - where it fails this teardown and then every
            // following test in the fixture, none of which can remove a database still held open.
            DatabaseContext.Database.GetDbConnection().Close();

            DeleteDatabaseOnceTheRequestsThatOutlivedTheirResponseAreDone();
            serviceScope.Dispose();
        }

        // An endpoint that starts work outliving its own response - a refresh, say - can still be holding
        // a connection by the time the test that called it ends, and a database that is still open cannot
        // be deleted on Windows. Linux deletes it regardless and never reports the race, which is why this
        // only ever shows up off CI. Waiting briefly is enough: the work is finishing, not stuck.
        private void DeleteDatabaseOnceTheRequestsThatOutlivedTheirResponseAreDone()
        {
            const int attempts = 40;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    DatabaseContext.Database.EnsureDeleted();
                    return;
                }
                catch (IOException) when (attempt < attempts)
                {
                    Thread.Sleep(50);
                }
            }
        }

        protected async Task SeedDatabase()
        {
            var seeders = ServiceProvider.GetServices<ISeeder>();
            foreach (var seeder in seeders)
            {
                await seeder.Seed();
            }
        }
    }
}
