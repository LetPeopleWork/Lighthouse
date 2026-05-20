using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    [TestFixture]
    [NonParallelizable]
    public abstract class IntegrationTestBase
    {
        private static readonly Lazy<TestWebApplicationFactory<Program>> SharedFactoryLazy =
            new(() => new TestWebApplicationFactory<Program>(), isThreadSafe: true);

        internal static TestWebApplicationFactory<Program> SharedFactory => SharedFactoryLazy.Value;

        internal static bool TryGetSharedFactoryIfCreated(out TestWebApplicationFactory<Program>? factory)
        {
            if (SharedFactoryLazy.IsValueCreated)
            {
                factory = SharedFactoryLazy.Value;
                return true;
            }

            factory = null;
            return false;
        }

        private readonly TestWebApplicationFactory<Program> webApplicationFactory;
        private readonly bool ownsFactory;
        private IServiceScope serviceScope;

        protected IntegrationTestBase()
            : this(SharedFactory, ownsFactory: false)
        {
        }

        protected IntegrationTestBase(TestWebApplicationFactory<Program> webApplicationFactory)
            : this(webApplicationFactory, ownsFactory: true)
        {
        }

        private IntegrationTestBase(TestWebApplicationFactory<Program> webApplicationFactory, bool ownsFactory)
        {
            this.webApplicationFactory = webApplicationFactory;
            this.ownsFactory = ownsFactory;
            Client = webApplicationFactory.CreateClient();
        }

        protected IServiceProvider ServiceProvider { get; private set; }

        protected HttpClient Client { get; }

        protected LighthouseAppContext DatabaseContext { get; private set; }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);
            if (ownsFactory)
            {
                webApplicationFactory.Dispose();
            }

            Client.Dispose();
        }

        [SetUp]
        protected void Init()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);
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
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);
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
