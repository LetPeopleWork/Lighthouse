using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Tests.Acceptance.SystemWideAccess;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Lighthouse.Backend.Tests.Acceptance.SystemWideAccess.EveryoneSignedInScenario;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// L11: two System Admins who read the switch at the same moment both save, on each real database the product
    /// runs on, migrated as an install is. One save is kept and the other is a conflict that leaves the kept value; on
    /// a database where no switch was stored yet, a save on the token read inserts it.
    /// </summary>
    [TestFixture]
    public class EveryoneSignedInStaleWriteTest : IntegrationFixture
    {
        public enum Provider
        {
            Sqlite,
            Postgres,
        }

        private static readonly bool[] OnlyOn = [true];

        // Started by the first Postgres case, so a run of the SQLite cases alone needs no Docker.
        private SharedPostgresContainer? postgres;

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [Test]
        public async Task L11_of_two_saves_on_the_same_token_one_is_kept_and_the_other_is_a_conflict([Values] Provider provider)
        {
            await OnAMigratedDatabase(provider, async services =>
            {
                await TheSwitch.StartUpSeederRuns(services.CreateScope().ServiceProvider);
                var readByAlex = await Read(services);
                var readByASecondAdmin = await Read(services);

                var alexSaves = await Save(services, true, readByAlex.Token);
                var theSecondAdminSaves = await Save(services, false, readByASecondAdmin.Token);

                var stored = Stored(services);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(alexSaves.Accepted, Is.True, $"WHAT: on {provider} the first save on the token read was not kept ({alexSaves}). WHY: a System Admin saving on a current reading must succeed. HOW: save under the token read.");
                    Assert.That(theSecondAdminSaves.Conflict, Is.True, $"WHAT: on {provider} a second save on the same, now stale, token answered {theSecondAdminSaves}. WHY: it would silently undo the first System Admin's change; the browser shows a conflict from a 409. HOW: configure the token as a concurrency token so the database refuses the stale update.");
                    Assert.That(stored.Select(row => row.On), Is.EqualTo(OnlyOn), $"WHAT: on {provider} the stored switch is {string.Join(", ", stored)} after the conflict. WHY: the refused save must leave the kept value. HOW: write nothing on a stale token.");
                }
            });
        }

        [Test]
        public async Task L11_with_no_switch_stored_a_save_on_the_token_read_inserts_it([Values] Provider provider)
        {
            await OnAMigratedDatabase(provider, async services =>
            {
                var read = await Read(services);

                var saved = await Save(services, true, read.Token);

                var stored = Stored(services);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(read.On, Is.False, $"WHAT: on {provider} a database with no switch stored reads it as on. WHY: a restore from an older backup must not open the instance. HOW: read a missing row as off.");
                    Assert.That(saved.Accepted, Is.True, $"WHAT: on {provider} turning the switch on with no row stored was not kept ({saved}). WHY: a System Admin must be able to turn it on even before the start-up seeder ran again. HOW: insert the row when it is missing, under the token the read gave.");
                    Assert.That(stored.Select(row => row.On), Is.EqualTo(OnlyOn), $"WHAT: on {provider} the stored switches are {string.Join(", ", stored)}. WHY: the switch is one row. HOW: insert exactly one row on the first save.");
                }
            });
        }

        private static async Task<SwitchState> Read(ServiceProvider services)
        {
            using var scope = services.CreateScope();
            return await TheSwitch.Read(scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>());
        }

        private static async Task<SaveAnswer> Save(ServiceProvider services, bool on, Guid token)
        {
            using var scope = services.CreateScope();
            return await TheSwitch.Save(scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>(), Alex_SignedIn(), on, token);
        }

        private static IReadOnlyList<SwitchState> Stored(ServiceProvider services)
        {
            using var scope = services.CreateScope();
            return TheSwitch.StoredRows(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>());
        }

        private async Task OnAMigratedDatabase(Provider provider, Func<ServiceProvider, Task> body)
        {
            if (provider == Provider.Sqlite)
            {
                var databaseFile = Path.Combine(Path.GetTempPath(), $"everyone-signed-in-{Guid.NewGuid():N}.db");
                try
                {
                    await using var services = ThePortOn(options => options.UseSqlite($"Data Source={databaseFile};Pooling=False", DatabaseConfigurator.ConfigureSqlite));
                    await MigratedWithThePeople(services);
                    await body(services);
                }
                finally
                {
                    File.Delete(databaseFile);
                }

                return;
            }

            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
            await using var postgresServices = ThePortOn(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));
            await MigratedWithThePeople(postgresServices);
            await body(postgresServices);
        }

        private static ServiceProvider ThePortOn(Action<DbContextOptionsBuilder> useDatabase)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(TestToday.FrozenNow));
            services.AddDbContext<LighthouseAppContext>(useDatabase);
            RegisterThePort(services);
            return services.BuildServiceProvider();
        }

        private static async Task MigratedWithThePeople(ServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();
            ThePeopleAreKnown(context);
        }
    }
}
