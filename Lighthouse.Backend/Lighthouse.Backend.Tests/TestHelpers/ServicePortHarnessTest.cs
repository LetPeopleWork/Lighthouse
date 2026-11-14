using System.Data;
using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    [TestFixture]
    public class ServicePortHarnessTest
    {
        private const string StoredName = "Ocean Explorer";

        private const string NameAnotherStepStored = "Coastal Survey";

        private const int NoTeamHasThisId = 987654;

        [Test]
        public void Each_scenario_gets_a_database_of_its_own_holding_the_schema_and_nothing_else()
        {
            using var first = new ServicePortHarness();
            using var second = new ServicePortHarness();

            ATeamIsSavedThrough(first);

            using var firstScope = first.NewScope();
            using var secondScope = second.NewScope();
            var secondContext = ContextOf(secondScope);
            var tablesTheModelNeeds = secondContext.Model.GetEntityTypes()
                .Select(entityType => entityType.GetTableName())
                .OfType<string>()
                .Distinct();
            var tablesInTheFile = secondContext.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ContextOf(firstScope).Teams.Count(), Is.EqualTo(1), "the first harness keeps what was saved through it");
                Assert.That(secondContext.Teams.Count(), Is.Zero, "a Team saved through one harness reached another");
                Assert.That(tablesInTheFile, Is.SupersetOf(tablesTheModelNeeds));
            }
        }

        [Test]
        public void What_a_fixture_registers_is_what_its_scenario_is_given()
        {
            var script = Mock.Of<IForecastService>();
            using var harness = new ServicePortHarness(services => services.AddSingleton(script));

            using var scope = harness.NewScope();

            Assert.That(scope.ServiceProvider.GetService<IForecastService>(), Is.SameAs(script));
        }

        [Test]
        public void A_finished_scenario_leaves_no_file_behind()
        {
            string databasePath;
            using (var harness = new ServicePortHarness())
            {
                databasePath = harness.DatabasePath;
                ATeamIsSavedThrough(harness);
                ThereAreSideFilesAKilledConnectionLeftBeside(databasePath);
                Assert.That(FilesOf(databasePath), Has.Length.EqualTo(4), "the database and its three side files are there to be removed");
            }

            Assert.That(FilesOf(databasePath), Is.Empty);
        }

        [Test]
        public void Each_scenario_reads_what_is_stored_not_what_an_earlier_step_still_holds()
        {
            using var harness = new ServicePortHarness();
            var teamId = ATeamIsSavedThrough(harness);

            using var earlierStep = harness.NewScope();
            var heldByTheEarlierStep = ContextOf(earlierStep).Teams.Single(team => team.Id == teamId);

            using (var anotherStep = harness.NewScope())
            {
                ContextOf(anotherStep).Teams
                    .Where(team => team.Id == teamId)
                    .ExecuteUpdate(setters => setters.SetProperty(team => team.Name, NameAnotherStepStored));
            }

            using var nextStep = harness.NewScope();
            var read = ContextOf(nextStep).Teams.Single(team => team.Id == teamId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.Name, Is.EqualTo(NameAnotherStepStored), "the read came from an earlier step, not from the file");
                Assert.That(read, Is.Not.SameAs(heldByTheEarlierStep));
            }
        }

        [Test]
        public void A_row_that_breaks_a_foreign_key_is_refused_as_the_host_refuses_it()
        {
            using var harness = new ServicePortHarness();

            using (var scope = harness.NewScope())
            {
                var context = ContextOf(scope);
                context.WorkItems.Add(AWorkItemOf(NoTeamHasThisId));

                var refusal = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

                Assert.That(refusal.InnerException?.Message, Does.Contain("FOREIGN KEY constraint failed"));
            }

            using var nextStep = harness.NewScope();
            Assert.That(ContextOf(nextStep).WorkItems.Count(), Is.Zero);
        }

        [Test]
        public void A_scenario_that_failed_with_its_scope_open_leaves_no_file_behind()
        {
            string databasePath;
            DbConnection connectionLeftOpen;
            using (var harness = new ServicePortHarness())
            {
                databasePath = harness.DatabasePath;
                connectionLeftOpen = AScenarioFailsHalfwayThroughAWrite(harness);
                Assert.That(FilesOf(databasePath), Has.Some.EndsWith("-wal"), "the write is under way, so its side file is there");
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connectionLeftOpen.State, Is.EqualTo(ConnectionState.Closed), "Windows cannot delete a database a connection still holds");
                Assert.That(FilesOf(databasePath), Is.Empty);
            }
        }

        [TestCase(true, 2)]
        [TestCase(false, 1)]
        public void Disposing_a_harness_twice_or_unused_does_no_harm(bool aScenarioRan, int timesDisposed)
        {
            var harness = new ServicePortHarness();
            var databasePath = harness.DatabasePath;
            if (aScenarioRan)
            {
                ATeamIsSavedThrough(harness);
            }

            for (var time = 0; time < timesDisposed; time++)
            {
                Assert.DoesNotThrow(harness.Dispose);
            }

            Assert.That(FilesOf(databasePath), Is.Empty);
        }

        [Test]
        public void A_service_no_fixture_registered_is_named_when_a_scenario_asks_for_it()
        {
            using var harness = new ServicePortHarness(services => services.AddScoped<PortThatReadsTheLicence>());
            using var scope = harness.NewScope();

            var failure = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<PortThatReadsTheLicence>());

            Assert.That(failure.Message, Does.Contain(nameof(ILicenseService)));
        }

        [Test]
        public void A_commit_from_a_connection_the_harness_never_built_is_seen_through_the_file_alone()
        {
            using var harness = new ServicePortHarness();
            AScratchTableIn(harness);

            var writes = harness.Writes.During(() =>
            {
                using var stranger = new SqliteConnection($"Data Source={harness.DatabasePath};Pooling=False");
                stranger.Open();
                using var command = stranger.CreateCommand();
                command.CommandText = "UPDATE Scratch SET Value = 2";
                command.ExecuteNonQuery();
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writes.TheFileChanged, Is.True, "the data version did not move on a commit from another connection");
                Assert.That(writes.StatementsThatWrote, Is.Empty, "the counter sees only the contexts the harness builds");
            }
        }

        [Test]
        public void A_step_that_only_reads_is_seen_writing_nothing()
        {
            using var harness = new ServicePortHarness();
            ATeamIsSavedThrough(harness);

            var writes = harness.Writes.During(() =>
            {
                using var scope = harness.NewScope();
                var context = ContextOf(scope);
                _ = context.Teams.ToList();
                context.Database.ExecuteSqlRaw("SELECT 1");
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writes.TheFileChanged, Is.False);
                Assert.That(writes.StatementsThatWrote, Is.Empty);
            }
        }

        [Test]
        public void A_Team_saved_through_a_scope_is_named_by_its_insert()
        {
            using var harness = new ServicePortHarness();

            var writes = harness.Writes.During(() => ATeamIsSavedThrough(harness));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writes.StatementsThatWrote, Has.Some.Contains("INSERT INTO \"Teams\""));
                Assert.That(writes.TheFileChanged, Is.True);
            }
        }

        [TestCase("INSERT INTO Scratch (Value) VALUES (3)", false, false)]
        [TestCase("UPDATE Scratch SET Value = 4", false, true)]
        [TestCase("DELETE FROM Scratch", false, false)]
        [TestCase("INSERT INTO Scratch (Value) VALUES (3) RETURNING Value", true, true)]
        [TestCase("UPDATE Scratch SET Value = 4 RETURNING Value", true, false)]
        [TestCase("DELETE FROM Scratch RETURNING Value", true, true)]
        [TestCase("CREATE TABLE Another (Value INTEGER NOT NULL)", false, false)]
        [TestCase("WITH Next AS (SELECT 5 AS Value) UPDATE Scratch SET Value = (SELECT Value FROM Next)", false, true)]
        public async Task A_statement_that_writes_is_named_whether_it_returns_rows_or_not(string statement, bool returnsRows, bool sentAsync)
        {
            using var harness = new ServicePortHarness();
            AScratchTableIn(harness);
            using var scope = harness.NewScope();
            var database = ContextOf(scope).Database;

            var writes = await harness.Writes.DuringAsync(async () =>
            {
                switch (returnsRows, sentAsync)
                {
                    case (false, false):
                        database.ExecuteSqlRaw(statement);
                        break;
                    case (false, true):
                        await database.ExecuteSqlRawAsync(statement);
                        break;
                    case (true, false):
                        _ = database.SqlQueryRaw<int>(statement).ToList();
                        break;
                    case (true, true):
                        _ = await database.SqlQueryRaw<int>(statement).ToListAsync();
                        break;
                }
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writes.StatementsThatWrote, Has.Count.EqualTo(1));
                Assert.That(writes.StatementsThatWrote, Has.All.Contains(statement));
                Assert.That(writes.TheFileChanged, Is.True);
            }
        }

        private static void AScratchTableIn(ServicePortHarness harness)
        {
            using var scope = harness.NewScope();
            ContextOf(scope).Database.ExecuteSqlRaw("CREATE TABLE Scratch (Value INTEGER NOT NULL); INSERT INTO Scratch (Value) VALUES (1);");
        }

        private static LighthouseAppContext ContextOf(IServiceScope scope)
            => scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

        private static int ATeamIsSavedThrough(ServicePortHarness harness)
        {
            using var scope = harness.NewScope();
            var context = ContextOf(scope);

            var team = ATeamOn(AStoredConnection(context));
            context.Teams.Add(team);
            context.SaveChanges();

            return team.Id;
        }

        private static DbConnection AScenarioFailsHalfwayThroughAWrite(ServicePortHarness harness)
        {
            var context = ContextOf(harness.NewScope());
            context.Database.BeginTransaction();
            AStoredConnection(context);

            return context.Database.GetDbConnection();
        }

        private static WorkTrackingSystemConnection AStoredConnection(LighthouseAppContext context)
        {
            var connection = new WorkTrackingSystemConnection { Name = "Harness", WorkTrackingSystem = WorkTrackingSystems.Csv };
            context.WorkTrackingSystemConnections.Add(connection);
            context.SaveChanges();

            return connection;
        }

        private static Team ATeamOn(WorkTrackingSystemConnection connection)
            => new() { Name = StoredName, WorkTrackingSystemConnectionId = connection.Id };

        private static WorkItem AWorkItemOf(int teamId)
            => new()
            {
                ReferenceId = "H-1",
                Name = "Work Item of no Team",
                Type = "User Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                TeamId = teamId,
                Url = string.Empty,
                ParentReferenceId = string.Empty,
                Order = "1",
            };

        private static void ThereAreSideFilesAKilledConnectionLeftBeside(string databasePath)
        {
            foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            {
                File.WriteAllBytes(databasePath + suffix, []);
            }
        }

        private static string[] FilesOf(string databasePath)
            => Directory.GetFiles(Path.GetDirectoryName(databasePath)!, $"{Path.GetFileName(databasePath)}*");

        private sealed class PortThatReadsTheLicence(ILicenseService licence)
        {
            public bool MayUsePremiumFeatures() => licence.CanUsePremiumFeatures();
        }
    }
}
