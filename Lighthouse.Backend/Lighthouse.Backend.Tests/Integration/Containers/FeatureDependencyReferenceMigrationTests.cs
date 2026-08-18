using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    // EF InMemory cannot host this check: it never runs a migration, so it reports a green
    // round trip against a schema the deployed databases do not have. Only a real provider
    // that applies the generated migration can tell whether the table is actually there.
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class FeatureDependencyReferenceMigrationTests
    {
        private const string TrackerReferenceId = "PROJ-4365";

        private const string PortfolioReferenceId = "PROJ-9182";

        [Test]
        public async Task Migration_OnSqlite_PersistsReferencesAFeatureWaitsOn()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-feature-dependency-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertReferencesRoundTripAsync(provider);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Test]
        public async Task Migration_OnPostgres_PersistsReferencesAFeatureWaitsOn()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertReferencesRoundTripAsync(provider);
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty,
                "the generated migration must leave the model and the database in step");
        }

        private static async Task AssertReferencesRoundTripAsync(ServiceProvider provider)
        {
            int featureId;

            using (var writeScope = provider.CreateScope())
            {
                var writeContext = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var feature = new Feature
                {
                    Name = "Checkout redesign",
                    ReferenceId = "PROJ-1",
                    Order = "1",
                };

                writeContext.Features.Add(feature);
                await writeContext.SaveChangesAsync();

                feature.ReplaceDependsOnReferences(
                [
                    new FeatureDependencyReference(feature.Id, TrackerReferenceId, DependencySource.TrackerLink),
                    new FeatureDependencyReference(feature.Id, PortfolioReferenceId, DependencySource.PortfolioField),
                ]);

                await writeContext.SaveChangesAsync();
                featureId = feature.Id;
            }

            using var readScope = provider.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var reloaded = await readContext.Features
                .Include(f => f.DependsOnReferences)
                .SingleAsync(f => f.Id == featureId);

            var tracker = reloaded.DependsOnReferences.Single(r => r.ReferenceId == TrackerReferenceId);
            var portfolio = reloaded.DependsOnReferences.Single(r => r.ReferenceId == PortfolioReferenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.DependsOnReferences, Has.Count.EqualTo(2),
                    "both references a Feature waits on must survive the round trip");
                Assert.That(tracker.FeatureId, Is.EqualTo(featureId),
                    "a reference must come back attached to the Feature that waits on it");
                Assert.That(tracker.Source, Is.EqualTo(DependencySource.TrackerLink),
                    "where the reference came from must round-trip");
                Assert.That(portfolio.Source, Is.EqualTo(DependencySource.PortfolioField),
                    "where the reference came from must round-trip");
            }
        }

        private static ServiceProvider BuildSqliteProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }

        private static ServiceProvider BuildPostgresProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            return services.BuildServiceProvider();
        }
    }
}
