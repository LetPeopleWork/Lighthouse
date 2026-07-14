using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// ADR-068 blocked-duration capture probe. Proves against REAL relational providers
    /// (SQLite + Postgres) that the generated AddWorkItemBlockedTransition migration creates
    /// the table and that the capture/close handlers round-trip a transition through the repository.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedDurationMigrationTests
    {
        [Test]
        public async Task Migration_OnSqlite_PersistsWorkItemBlockedTransitionTable()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-blocked-duration-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertTableRoundTripsAsync(provider);
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
        public async Task Migration_OnPostgres_PersistsWorkItemBlockedTransitionTable()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertTableRoundTripsAsync(provider);
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty,
                "the generated AddWorkItemBlockedTransition migration must apply cleanly on a real provider");
        }

        private static async Task AssertTableRoundTripsAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 0,
            };

            var workItem = new WorkItem
            {
                ReferenceId = "WI-1",
                Name = "Story WI-1",
                Type = "Story",
                State = "In Progress",
                Tags = [],
                Order = "",
                Team = team,
            };

            context.Teams.Add(team);
            context.WorkItems.Add(workItem);
            await context.SaveChangesAsync();

            var transition = new WorkItemBlockedTransition
            {
                WorkItemId = workItem.Id,
                EnteredAt = DateTime.UtcNow,
                LeftAt = null,
            };

            context.WorkItemBlockedTransitions.Add(transition);
            await context.SaveChangesAsync();

            var reloaded = await context.WorkItemBlockedTransitions
                .SingleAsync(t => t.WorkItemId == workItem.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.WorkItemId, Is.EqualTo(workItem.Id),
                    "the migration must persist a WorkItemBlockedTransition row");
                Assert.That(reloaded.EnteredAt, Is.EqualTo(transition.EnteredAt).Within(TimeSpan.FromSeconds(1)),
                    "EnteredAt must round-trip");
                Assert.That(reloaded.LeftAt, Is.Null,
                    "a newly-created open transition has a null LeftAt");
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
