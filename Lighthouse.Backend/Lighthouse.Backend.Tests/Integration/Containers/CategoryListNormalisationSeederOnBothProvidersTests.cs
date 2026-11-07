using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// The start-up pass that turns the To Do, Doing and Done lists the previous release stored (a group's name
    /// standing in for its states) into the tracker's own states, on a database written by that release, on both
    /// real providers. A second start must find nothing left to do, and an owner it cannot read must not stop the start.
    /// </summary>
    [TestFixture]
    public class CategoryListNormalisationSeederOnBothProvidersTests : IntegrationFixture
    {
        public enum Provider
        {
            Sqlite,
            Postgres,
        }

        public enum StoredShape
        {
            AlreadyRawWithNoGroup,
            AlreadyRawGroupStatesNextToEachOther,
            AlreadyRawGroupStatesApart,
            GroupNamedInPlaceOfItsStates,
            GroupNamedAfterOneOfItsOwnStates,
            NameShadowingAStateThatIsStillListed,
            GroupThatListsAnotherGroupsName,
        }

        private enum Outcome
        {
            Untouched,
            Rewritten,
            RewrittenTheOldWayWithAWarning,
        }

        private sealed record Shape(StateMapping[] Mappings, string[] Doing, string[] ExpectedDoing, Outcome Outcome);

        private static Shape ShapeOf(StoredShape shape) => shape switch
        {
            StoredShape.AlreadyRawWithNoGroup => new Shape([], ["Analysis", "Testing"], ["Analysis", "Testing"], Outcome.Untouched),
            StoredShape.AlreadyRawGroupStatesNextToEachOther => new Shape(
                [Review()], ["Analysis", "Code Review", "Testing", "Waiting"], ["Analysis", "Code Review", "Testing", "Waiting"], Outcome.Untouched),
            StoredShape.AlreadyRawGroupStatesApart => new Shape(
                [Review()], ["Code Review", "Analysis", "Testing"], ["Code Review", "Analysis", "Testing"], Outcome.Untouched),
            StoredShape.GroupNamedInPlaceOfItsStates => new Shape(
                [Review()], ["Analysis", "Review", "Waiting"], ["Analysis", "Code Review", "Testing", "Waiting"], Outcome.Rewritten),
            StoredShape.GroupNamedAfterOneOfItsOwnStates => new Shape(
                [new StateMapping { Name = "Active", States = ["Active", "Resolved"] }], ["Active", "Waiting"], ["Active", "Resolved", "Waiting"], Outcome.Rewritten),
            StoredShape.NameShadowingAStateThatIsStillListed => new Shape(
                [new StateMapping { Name = "In Arbeit", States = ["In Progress"] }], ["In Arbeit", "In Progress"], ["In Progress"], Outcome.RewrittenTheOldWayWithAWarning),
            StoredShape.GroupThatListsAnotherGroupsName => new Shape(
                [new StateMapping { Name = "A", States = ["Fix"] }, new StateMapping { Name = "B", States = ["A"] }], ["B", "Analysis"], ["A", "Analysis"], Outcome.RewrittenTheOldWayWithAWarning),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

        private static readonly string[] TheUnreadableTeamsDoing = ["Analysis", "Review", "Waiting"];

        private static StateMapping Review() => new() { Name = "Review", States = ["Code Review", "Testing"] };

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
        public async Task EveryStoredShape_IsRewrittenOnceAndLeftAloneTheSecondTime(
            [Values] Provider provider,
            [Values] StoredShape storedShape)
        {
            var shape = ShapeOf(storedShape);

            await WithADatabase(provider, async services =>
            {
                var owners = await SeedATeamAndAPortfolio(services, shape);

                var firstRun = new RecordingLogger<CategoryListNormalisationSeeder>();
                await RunTheSeeder(services, firstRun);
                var afterFirst = await ReadBack(services, owners);

                var secondRun = new RecordingLogger<CategoryListNormalisationSeeder>();
                await RunTheSeeder(services, secondRun);
                var afterSecond = await ReadBack(services, owners);

                var rewritten = shape.Outcome != Outcome.Untouched;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(afterFirst.TeamDoing, Is.EqualTo(shape.ExpectedDoing), "team after the first start");
                    Assert.That(afterFirst.PortfolioDoing, Is.EqualTo(shape.ExpectedDoing), "portfolio after the first start");
                    Assert.That(Counted(firstRun, "Scanned"), Is.EqualTo(2));
                    Assert.That(Counted(firstRun, "Rewritten"), Is.EqualTo(rewritten ? 2 : 0));
                    Assert.That(Counted(firstRun, "LegacyExpanded"), Is.EqualTo(shape.Outcome == Outcome.RewrittenTheOldWayWithAWarning ? 2 : 0));
                    Assert.That(Counted(firstRun, "Skipped"), Is.Zero);
                    Assert.That(firstRun.Warnings, Has.Count.EqualTo(shape.Outcome == Outcome.RewrittenTheOldWayWithAWarning ? 2 : 0));

                    Assert.That(afterSecond.TeamDoing, Is.EqualTo(shape.ExpectedDoing), "team after the second start");
                    Assert.That(afterSecond.PortfolioDoing, Is.EqualTo(shape.ExpectedDoing), "portfolio after the second start");
                    Assert.That(afterSecond.ConcurrencyTokens, Is.EqualTo(afterFirst.ConcurrencyTokens), "the second start saved nothing");
                    Assert.That(Counted(secondRun, "Rewritten"), Is.Zero);
                    Assert.That(secondRun.Warnings, Is.Empty);

                    AssertNothingPersonalWasLogged(firstRun, owners);
                }
            });
        }

        [Test]
        public async Task AnOwnerWhoseGroupHasNoStates_IsLeftAloneWithAWarning_AndTheOthersAreStillRewritten(
            [Values] Provider provider,
            [Values] bool statesAreNull)
        {
            var shape = ShapeOf(StoredShape.GroupNamedInPlaceOfItsStates);

            await WithADatabase(provider, async services =>
            {
                var owners = await SeedATeamAndAPortfolio(services, shape);
                var unreadableTeamId = await SeedATeamWhoseGroupHasNoStatesAtAll(services, statesAreNull);

                var run = new RecordingLogger<CategoryListNormalisationSeeder>();
                await RunTheSeeder(services, run);
                var rewritten = await ReadBack(services, owners);
                var unreadable = await ReadTeamDoing(services, unreadableTeamId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(rewritten.TeamDoing, Is.EqualTo(shape.ExpectedDoing));
                    Assert.That(rewritten.PortfolioDoing, Is.EqualTo(shape.ExpectedDoing));
                    Assert.That(unreadable, Is.EqualTo(TheUnreadableTeamsDoing), "the unreadable owner keeps what it had");
                    Assert.That(Counted(run, "Scanned"), Is.EqualTo(3));
                    Assert.That(Counted(run, "Rewritten"), Is.EqualTo(2));
                    Assert.That(Counted(run, "Skipped"), Is.EqualTo(1));
                    Assert.That(run.Warnings, Has.Count.EqualTo(1));
                    var warning = run.Everything.Single(entry => entry.Level == LogLevel.Warning);
                    Assert.That(warning.Properties["OwnerId"], Is.EqualTo(unreadableTeamId));
                    Assert.That(warning.Properties["Rule"], Is.EqualTo("EmptyGroup"));
                }
            });
        }

        private sealed record Owners(int TeamId, int PortfolioId, string TeamName, string PortfolioName);

        private sealed record Stored(string[] TeamDoing, string[] PortfolioDoing, Guid[] ConcurrencyTokens);

        private async Task WithADatabase(Provider provider, Func<ServiceProvider, Task> body)
        {
            if (provider == Provider.Sqlite)
            {
                var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-lists-{Guid.NewGuid():N}.db");
                try
                {
                    await using var services = BuildProvider(options => options.UseSqlite($"Data Source={databaseFile};Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));
                    await MigrateToTheLatestSchema(services);
                    await body(services);
                }
                finally
                {
                    if (File.Exists(databaseFile))
                    {
                        File.Delete(databaseFile);
                    }
                }

                return;
            }

            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
            await using var postgresServices = BuildProvider(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));
            await MigrateToTheLatestSchema(postgresServices);
            await body(postgresServices);
        }

        private static ServiceProvider BuildProvider(Action<DbContextOptionsBuilder> useDatabase)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(useDatabase);
            return services.BuildServiceProvider();
        }

        private static async Task MigrateToTheLatestSchema(ServiceProvider services)
        {
            using var scope = services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.MigrateAsync();
        }

        private static async Task<Owners> SeedATeamAndAPortfolio(ServiceProvider services, Shape shape)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 180,
                ToDoStates = ["Backlog"],
                DoingStates = [.. shape.Doing],
                DoneStates = ["Done"],
                StateMappings = [.. shape.Mappings.Select(Copy)],
            };
            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                ToDoStates = ["Backlog"],
                DoingStates = [.. shape.Doing],
                DoneStates = ["Done"],
                StateMappings = [.. shape.Mappings.Select(Copy)],
            };

            context.AddRange(team, portfolio);
            await context.SaveChangesAsync();

            return new Owners(team.Id, portfolio.Id, team.Name, portfolio.Name);
        }

        private static StateMapping Copy(StateMapping mapping) => new() { Name = mapping.Name, States = [.. mapping.States] };

        // Written with SQL because the model refuses to hold such a mapping: a row left by a version that let one through.
        private static async Task<int> SeedATeamWhoseGroupHasNoStatesAtAll(ServiceProvider services, bool statesAreNull)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = WorkTrackingSystems.Jira },
                DoneItemsCutoffDays = 180,
                ToDoStates = ["Backlog"],
                DoingStates = ["Analysis", "Review", "Waiting"],
                DoneStates = ["Done"],
            };
            context.Add(team);
            await context.SaveChangesAsync();

            var aGroupWithNoStates = statesAreNull ? "[{\"Name\":\"Review\",\"States\":null}]" : "[{\"Name\":\"Review\",\"States\":[]}]";
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Teams\" SET \"StateMappings\" = {aGroupWithNoStates} WHERE \"Id\" = {team.Id}");
            return team.Id;
        }

        private static async Task RunTheSeeder(ServiceProvider services, RecordingLogger<CategoryListNormalisationSeeder> log)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var seeder = new CategoryListNormalisationSeeder(
                new TeamRepository(context, NullLogger<TeamRepository>.Instance),
                new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance),
                log);

            await seeder.Seed();
        }

        private static async Task<Stored> ReadBack(ServiceProvider services, Owners owners)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var team = await context.Teams.AsNoTracking().SingleAsync(t => t.Id == owners.TeamId);
            var portfolio = await context.Portfolios.AsNoTracking().SingleAsync(p => p.Id == owners.PortfolioId);

            return new Stored([.. team.DoingStates], [.. portfolio.DoingStates], [team.ConcurrencyToken, portfolio.ConcurrencyToken]);
        }

        private static async Task<string[]> ReadTeamDoing(ServiceProvider services, int teamId)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return [.. (await context.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId)).DoingStates];
        }

        // The summary line carries its counts as named values, which is how an operator's log search reads them.
        private static int Counted(RecordingLogger<CategoryListNormalisationSeeder> log, string name)
        {
            var summary = log.Everything.Last(entry => entry.Level == LogLevel.Information && entry.Properties.ContainsKey("Scanned"));
            return Convert.ToInt32(summary.Properties[name], System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void AssertNothingPersonalWasLogged(RecordingLogger<CategoryListNormalisationSeeder> log, Owners owners)
        {
            var everythingWritten = string.Join('\n', log.Everything.SelectMany(entry => entry.Properties.Values.Select(value => $"{value}").Prepend(entry.Message)));
            string[] mustNotAppear = [owners.TeamName, owners.PortfolioName, "Analysis", "Review", "Code Review", "In Arbeit", "In Progress", "Active", "Resolved"];

            Assert.That(mustNotAppear.Where(secret => everythingWritten.Contains(secret, StringComparison.Ordinal)), Is.Empty,
                "The seeder's lines carry ids and counts only, never an owner's or a state's name.");
        }
    }
}
