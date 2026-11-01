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
    /// One owner's refused write must not hold back the others. The seeders of one start share a database context,
    /// so a failed owner that stays tracked would make the next owner's save fail with it. SQLite is enough: what
    /// is tested is how the context is used, not a provider.
    /// </summary>
    [TestFixture]
    public class CategoryListNormalisationSeederIsolationTests
    {
        private static string[] Listed(params string[] states) => states;

        [Test]
        [Ignore("pending: category lists hold raw states")]
        public async Task AnOwnerWhoseWriteIsRefused_IsSkipped_AndTheOwnersAfterItAreStillWritten()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-lists-isolation-{Guid.NewGuid():N}.db");
            try
            {
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton<ICryptoService, FakeCryptoService>();
                services.AddDbContext<LighthouseAppContext>(options =>
                    options.UseSqlite($"Data Source={databaseFile};Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));
                await using var provider = services.BuildServiceProvider();

                int refusedTeamId;
                int laterTeamId;
                int portfolioId;
                using (var scope = provider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                    await context.Database.MigrateAsync();

                    var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = WorkTrackingSystems.Jira };
                    var refused = ACollapsedTeam(connection, "First");
                    var later = ACollapsedTeam(connection, "Second");
                    var portfolio = new Portfolio
                    {
                        Name = "Portfolio",
                        WorkTrackingSystemConnection = connection,
                        DoingStates = ["Analysis", "Review", "Waiting"],
                        StateMappings = [new StateMapping { Name = "Review", States = ["Code Review", "Testing"] }],
                    };
                    context.AddRange(refused, later, portfolio);
                    await context.SaveChangesAsync();
                    (refusedTeamId, laterTeamId, portfolioId) = (refused.Id, later.Id, portfolio.Id);

                    var database = context.Database.GetDbConnection();
                    await database.OpenAsync();
                    await using (var command = database.CreateCommand())
                    {
                        command.CommandText = $"CREATE TRIGGER refuse_one_owner BEFORE UPDATE ON \"Teams\" WHEN NEW.\"Id\" = {refusedTeamId} BEGIN SELECT RAISE(ABORT, 'refused'); END";
                        await command.ExecuteNonQueryAsync();
                    }

                    await database.CloseAsync();
                }

                var log = new RecordingLogger<CategoryListNormalisationSeeder>();
                using (var scope = provider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                    await new CategoryListNormalisationSeeder(
                        new TeamRepository(context, NullLogger<TeamRepository>.Instance),
                        new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance),
                        log).Seed();
                }

                using var readScope = provider.CreateScope();
                var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var refusedTeam = await readContext.Teams.AsNoTracking().SingleAsync(t => t.Id == refusedTeamId);
                var refusedAfter = refusedTeam.DoingStates;
                var laterAfter = (await readContext.Teams.AsNoTracking().SingleAsync(t => t.Id == laterTeamId)).DoingStates;
                var portfolioAfter = (await readContext.Portfolios.AsNoTracking().SingleAsync(p => p.Id == portfolioId)).DoingStates;
                var summary = log.Everything.Last(entry => entry.Level == LogLevel.Information && entry.Properties.ContainsKey("Scanned"));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refusedAfter, Is.EqualTo(Listed("Analysis", "Review", "Waiting")), "The refused owner keeps what it had.");
                    Assert.That(refusedTeam.AllStates, Is.SupersetOf(Listed("Code Review", "Testing")), "and every read of it still finds the group's states.");
                    Assert.That(laterAfter, Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
                    Assert.That(portfolioAfter, Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
                    Assert.That(Convert.ToInt32(summary.Properties["Skipped"], System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(1));
                    Assert.That(Convert.ToInt32(summary.Properties["Rewritten"], System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(2));
                    Assert.That(log.Warnings, Has.Count.EqualTo(1));
                    Assert.That(log.Warnings[0], Does.Contain(refusedTeamId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        private static Team ACollapsedTeam(WorkTrackingSystemConnection connection, string name)
            => new()
            {
                Name = name,
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 180,
                DoingStates = ["Analysis", "Review", "Waiting"],
                StateMappings = [new StateMapping { Name = "Review", States = ["Code Review", "Testing"] }],
            };
    }
}
