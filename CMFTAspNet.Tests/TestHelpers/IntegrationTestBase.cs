using CMFTAspNet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CMFTAspNet.Tests.TestHelpers
{
    [TestFixture]
    public abstract class IntegrationTestBase
    {
        private readonly TestWebApplicationFactory<Program> webApplicationFactory;

        private IServiceScope serviceScope;

        public IntegrationTestBase(TestWebApplicationFactory<Program> webApplicationFactory)
        {
            this.webApplicationFactory = webApplicationFactory;
        }

        protected IServiceProvider ServiceProvider { get; private set; }

        protected Data.AppContext DatabaseContext { get; private set; }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            webApplicationFactory.Dispose();
        }

        [SetUp]
        protected void Init()
        {
            serviceScope = webApplicationFactory.Services.CreateScope();
            ServiceProvider = serviceScope.ServiceProvider;

            DatabaseContext = ServiceProvider.GetService<Data.AppContext>() ?? throw new InvalidOperationException("Could not Find DB Context");
            
            DatabaseContext.Database.Migrate();
        }

        [TearDown]
        protected void TearDown()
        {
            DatabaseContext.Database.EnsureDeleted();
            
            serviceScope?.Dispose();
        }
    }
}
