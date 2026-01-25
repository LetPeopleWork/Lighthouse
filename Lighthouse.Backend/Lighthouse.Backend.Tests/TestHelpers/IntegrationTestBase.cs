using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    [TestFixture]
    public abstract class IntegrationTestBase(TestWebApplicationFactory<Program> webApplicationFactory)
    {
        private IServiceScope serviceScope;

        protected IServiceProvider ServiceProvider { get; private set; }

        protected HttpClient Client { get; } = webApplicationFactory.CreateClient();

        protected LighthouseAppContext DatabaseContext { get; private set; }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            webApplicationFactory.Dispose();

            Client.Dispose();
        }

        [SetUp]
        protected void Init()
        {
            serviceScope = webApplicationFactory.Services.CreateScope();
            ServiceProvider = serviceScope.ServiceProvider;

            DatabaseContext = ServiceProvider.GetService<LighthouseAppContext>()
                              ?? throw new InvalidOperationException("Could not Find DB Context");

            DatabaseContext.Database.EnsureDeleted();
            DatabaseContext.Database.EnsureCreated();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            DatabaseContext.Database.EnsureDeleted();
            serviceScope.Dispose();
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
