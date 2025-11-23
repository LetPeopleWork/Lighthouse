using Lighthouse.Backend.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    [TestFixture]
    public abstract class IntegrationTestBase
    {
        private readonly TestWebApplicationFactory<Program> webApplicationFactory;

        private readonly HttpClient client;

        private IServiceScope serviceScope;

        protected IntegrationTestBase(TestWebApplicationFactory<Program> webApplicationFactory)
        {
            this.webApplicationFactory = webApplicationFactory;

            client = webApplicationFactory.CreateClient();
        }

        protected IServiceProvider ServiceProvider { get; private set; }

        protected HttpClient Client => client;

        protected LighthouseAppContext DatabaseContext { get; private set; }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            webApplicationFactory.Dispose();

            client?.Dispose();
        }

        [SetUp]
        protected void Init()
        {
            serviceScope = webApplicationFactory.Services.CreateScope();
            ServiceProvider = serviceScope.ServiceProvider;

            DatabaseContext = ServiceProvider.GetService<LighthouseAppContext>()
                ?? throw new InvalidOperationException("Could not Find DB Context");

            // Ensure the database is created and migrations are applied
            DatabaseContext.Database.EnsureDeleted();
            DatabaseContext.Database.EnsureCreated();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            // Ensure we clean up before disposing
            if (DatabaseContext != null)
            {
                Thread.Sleep(100);
                DatabaseContext.Database.EnsureDeleted();
            }

            serviceScope?.Dispose();
        }
    }
}
