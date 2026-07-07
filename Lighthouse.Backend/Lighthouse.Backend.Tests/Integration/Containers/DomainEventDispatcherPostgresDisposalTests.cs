using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Npgsql;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Regression guard for the CI E2E flakiness on the Postgres verify job:
    /// PortfolioUpdater/TeamUpdater flooded with
    /// "Received backend message BindComplete while expecting ReadyForQueryMessage",
    /// which left portfolios unpopulated and failed the DeliveryMetrics burnup E2E.
    ///
    /// Root cause: <see cref="DomainEventDispatcher.PublishAsync"/> disposed its DI scope
    /// synchronously ("using var scope"). Disposing an Npgsql-backed DbContext synchronously
    /// runs NpgsqlConnection.CloseAsync sync-over-async, which under the app's concurrent load
    /// races the connector and desyncs the wire protocol. SQLite tolerates it (main E2E stayed
    /// green); Postgres does not (verify job flaked).
    ///
    /// The wire-protocol desync is a timing heisenbug that does not reproduce deterministically
    /// in an isolated harness, so this guard pins the defect itself: the dispatcher must dispose
    /// its scope ASYNCHRONOUSLY. .NET's ServiceProvider throws when a scope holding an
    /// IAsyncDisposable-only service is disposed synchronously, so a sentinel resolved inside the
    /// dispatcher's scope fails against synchronous disposal and passes once disposal is async.
    /// The scope also resolves a real Npgsql-backed handler so the disposal path exercised is the
    /// production one.
    /// </summary>
    [TestFixture]
    [Category("e2e-flakiness-npgsql-disposal")]
    public class DomainEventDispatcherPostgresDisposalTests
    {
        [Test]
        public async Task PublishAsync_OnPostgres_DisposesScopeAsynchronously_SoNpgsqlConnectionIsNeverClosedSyncOverAsync()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);

            var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

            // Against synchronous scope disposal this throws InvalidOperationException from the
            // ServiceProvider (the scope holds an IAsyncDisposable-only sentinel). Against async
            // disposal it completes cleanly.
            Assert.DoesNotThrowAsync(
                async () => await dispatcher.PublishAsync(new DisposalProbeEvent()),
                "DomainEventDispatcher.PublishAsync must dispose its scope asynchronously so the Npgsql "
                + "connection is closed via DisposeAsync, never sync-over-async.");
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();
        }

        private static ServiceProvider BuildPostgresProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<AsyncOnlyDisposableSentinel>();
            services.AddScoped<IDomainEventHandler<DisposalProbeEvent>, NpgsqlWritingProbeHandler>();

            return services.BuildServiceProvider();
        }

        private sealed record DisposalProbeEvent : IDomainEvent;

        private sealed class NpgsqlWritingProbeHandler(
            LighthouseAppContext context,
            AsyncOnlyDisposableSentinel sentinel)
            : IDomainEventHandler<DisposalProbeEvent>
        {
            public async Task HandleAsync(DisposalProbeEvent domainEvent, CancellationToken cancellationToken)
            {
                // Resolving the sentinel enlists it in the scope's disposal set. Mirror production
                // snapshot handlers with a real sync read + async write against the Npgsql context.
                _ = sentinel;
                _ = context.Teams.SingleOrDefault(team => team.Id == -1);

                context.BlockedCountSnapshots.Add(new BlockedCountSnapshot
                {
                    OwnerId = 1,
                    OwnerType = OwnerType.Team,
                    RecordedAt = new DateOnly(2026, 7, 7),
                    BlockedCount = 1,
                });

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private sealed class AsyncOnlyDisposableSentinel : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
