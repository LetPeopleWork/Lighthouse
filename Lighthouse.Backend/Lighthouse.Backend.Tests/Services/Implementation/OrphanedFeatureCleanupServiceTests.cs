using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class OrphanedFeatureCleanupServiceTests
    {
        private Mock<ICryptoService> cryptoServiceMock;
        private Mock<ILogger<LighthouseAppContext>> contextLoggerMock;
        private Mock<ILogger<OrphanedFeatureCleanupService>> serviceLoggerMock;
        private SqliteConnection connection;
        private DbContextOptions<LighthouseAppContext> options;

        [SetUp]
        public void SetUp()
        {
            cryptoServiceMock = new Mock<ICryptoService>();
            contextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();
            serviceLoggerMock = new Mock<ILogger<OrphanedFeatureCleanupService>>();

            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            using (var pragmaCommand = connection.CreateCommand())
            {
                pragmaCommand.CommandText = "PRAGMA foreign_keys = ON";
                pragmaCommand.ExecuteNonQuery();
            }

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite(connection)
                .Options;

            using var seedContext = new LighthouseAppContext(options, cryptoServiceMock.Object, contextLoggerMock.Object);
            seedContext.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            connection?.Dispose();
        }

        [Test]
        public async Task CleanupAsync_FeatureWithNoPortfolios_DeletesIt()
        {
            using (var seedContext = createContext())
            {
                var team = await seedTeam(seedContext, "Orphan Team");
                var orphan = getFeature(team, "Orphan Feature");
                seedContext.Features.Add(orphan);
                await seedContext.SaveChangesAsync();
            }

            var subject = new OrphanedFeatureCleanupService(buildScopeFactory(), serviceLoggerMock.Object);
            var deleted = await subject.CleanupAsync();

            using var assertContext = createContext();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted, Is.EqualTo(1));
                Assert.That(await assertContext.Features.CountAsync(), Is.Zero);
            }
        }

        [Test]
        public async Task CleanupAsync_FeatureLinkedToPortfolio_PreservesIt()
        {
            using (var seedContext = createContext())
            {
                var team = await seedTeam(seedContext, "Linked Team");
                var portfolio = await seedPortfolio(seedContext, "Linked Portfolio");
                var feature = getFeature(team, "Linked Feature");
                feature.Portfolios.Add(portfolio);
                seedContext.Features.Add(feature);
                await seedContext.SaveChangesAsync();
            }

            var subject = new OrphanedFeatureCleanupService(buildScopeFactory(), serviceLoggerMock.Object);
            var deleted = await subject.CleanupAsync();

            using var assertContext = createContext();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted, Is.Zero);
                Assert.That(await assertContext.Features.CountAsync(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task CleanupAsync_ParentFeature_PreservesIt()
        {
            using (var seedContext = createContext())
            {
                var team = await seedTeam(seedContext, "Parent Team");
                var parent = getFeature(team, "Parent Feature");
                parent.IsParentFeature = true;
                seedContext.Features.Add(parent);
                await seedContext.SaveChangesAsync();
            }

            var subject = new OrphanedFeatureCleanupService(buildScopeFactory(), serviceLoggerMock.Object);
            var deleted = await subject.CleanupAsync();

            using var assertContext = createContext();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted, Is.Zero);
                Assert.That(await assertContext.Features.CountAsync(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task CleanupAsync_OrphanWithFeatureWorkAndForecasts_CascadesDependents()
        {
            using (var seedContext = createContext())
            {
                var team = await seedTeam(seedContext, "Cascade Team");
                var orphan = new Feature(team, 5)
                {
                    Name = "Orphan With Dependents",
                    Order = "1",
                };
                orphan.Forecasts.Add(new WhenForecast { NumberOfItems = 3 });
                seedContext.Features.Add(orphan);
                await seedContext.SaveChangesAsync();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(await seedContext.Set<FeatureWork>().CountAsync(), Is.EqualTo(1), "precondition: FeatureWork seeded");
                    Assert.That(await seedContext.Set<WhenForecast>().CountAsync(), Is.EqualTo(1), "precondition: Forecast seeded");
                }
            }

            var subject = new OrphanedFeatureCleanupService(buildScopeFactory(), serviceLoggerMock.Object);
            var deleted = await subject.CleanupAsync();

            using var assertContext = createContext();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted, Is.EqualTo(1));
                Assert.That(await assertContext.Features.CountAsync(), Is.Zero);
                Assert.That(await assertContext.Set<FeatureWork>().CountAsync(), Is.Zero);
                Assert.That(await assertContext.Set<WhenForecast>().CountAsync(), Is.Zero);
            }
        }

        private LighthouseAppContext createContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, contextLoggerMock.Object);
        }

        private IServiceScopeFactory buildScopeFactory()
        {
            var services = new ServiceCollection();
            services.AddSingleton(cryptoServiceMock.Object);
            services.AddSingleton(contextLoggerMock.Object);
            services.AddScoped(_ => new LighthouseAppContext(options, cryptoServiceMock.Object, contextLoggerMock.Object));
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IServiceScopeFactory>();
        }

        private static async Task<WorkTrackingSystemConnection> seedConnection(LighthouseAppContext context, string name)
        {
            var workTrackingConnection = new WorkTrackingSystemConnection
            {
                Name = name,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            context.WorkTrackingSystemConnections.Add(workTrackingConnection);
            await context.SaveChangesAsync();
            return workTrackingConnection;
        }

        private static async Task<Team> seedTeam(LighthouseAppContext context, string name)
        {
            var workTrackingConnection = await seedConnection(context, $"{name} Connection");
            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnectionId = workTrackingConnection.Id,
            };
            context.Teams.Add(team);
            await context.SaveChangesAsync();
            return team;
        }

        private static async Task<Portfolio> seedPortfolio(LighthouseAppContext context, string name)
        {
            var workTrackingConnection = await seedConnection(context, $"{name} Connection");
            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnectionId = workTrackingConnection.Id,
            };
            context.Portfolios.Add(portfolio);
            await context.SaveChangesAsync();
            return portfolio;
        }

        private static Feature getFeature(Team team, string name)
        {
            return new Feature(team, 0)
            {
                Name = name,
                Order = "1",
            };
        }
    }
}
