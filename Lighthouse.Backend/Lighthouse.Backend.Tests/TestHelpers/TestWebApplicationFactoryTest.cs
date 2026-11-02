using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public class TestWebApplicationFactoryTest
    {
        [Test]
        public void AFreshDatabaseNeverReusesThePathOfAnEarlierOne()
        {
            using var factory = new TestWebApplicationFactory<Program>();

            var first = PathOfCurrentDatabase(factory);
            factory.UseFreshDatabase();
            var second = PathOfCurrentDatabase(factory);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        // The product switches its database file to write-ahead logging when it opens it, and that setting
        // is stored in the file itself, so a product database still in its original mode after a test host
        // has handed out a database context is one the product's own setup never opened. Startup may still
        // read the file to look for stored secrets; that read leaves the mode alone.
        [Test]
        public void UsingTheTestDatabaseNeverOpensTheProductDatabase()
        {
            var productDirectory = Path.Combine(Path.GetTempPath(), $"lighthouse-product-db-{Guid.NewGuid():N}");
            var productConnectionString = $"Data Source={Path.Combine(productDirectory, "LighthouseAppContext.db")};Pooling=False";

            try
            {
                Directory.CreateDirectory(productDirectory);
                Execute(productConnectionString, "CREATE TABLE Untouched (Id INTEGER)");

                using (var root = new TestWebApplicationFactory<Program>())
                using (var factory = root.WithWebHostBuilder(builder =>
                    builder.UseSetting("Database:ConnectionString", productConnectionString)))
                using (var scope = factory.Services.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureCreated();
                }

                Assert.That(Execute(productConnectionString, "PRAGMA journal_mode"), Is.EqualTo("delete"));
            }
            finally
            {
                Directory.Delete(productDirectory, recursive: true);
            }
        }

        [Test]
        public void TheTestDatabaseSplitsQueriesLikeTheProductDoes()
        {
            using var factory = new TestWebApplicationFactory<Program>();
            using var scope = factory.Services.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LighthouseAppContext>>();

            var relationalOptions = options.Extensions.OfType<RelationalOptionsExtension>().Single();
            Assert.That(relationalOptions.QuerySplittingBehavior, Is.EqualTo(QuerySplittingBehavior.SplitQuery));
        }

        [Test]
        public void TheHostReadsTheFactoryClockAndResettingItRestoresTheFrozenInstant()
        {
            using var factory = new TestWebApplicationFactory<Program>();
            var hostClock = factory.Services.GetRequiredService<TimeProvider>();

            factory.Clock.Advance(TimeSpan.FromDays(3));
            Assert.That(hostClock.GetUtcNow(), Is.EqualTo(TestToday.FrozenNow.AddDays(3)));

            factory.Clock.Reset();
            Assert.That(hostClock.GetUtcNow(), Is.EqualTo(TestToday.FrozenNow));
        }

        [Test]
        public void WithTheSwitchOffADatabaseStartsWithoutTheSchema()
        {
            using var factory = new TestWebApplicationFactory<Program>();

            var path = PathOfCurrentDatabase(factory);

            Assert.That(SchemaOf(path), Is.Empty);
        }

        [Test]
        public void WithTheSwitchOnEachNewDatabaseStartsAsACompleteCopyAndAnEarlierScopeKeepsItsOwn()
        {
            using var factory = new TestWebApplicationFactory<Program> { UseSchemaTemplate = true };
            var template = SchemaOf(TestDatabaseTemplate.Path);

            using var earlierScope = factory.Services.CreateScope();
            var earlier = earlierScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.GetDbConnection().DataSource;
            factory.UseFreshDatabase();
            var later = PathOfCurrentDatabase(factory);
            factory.UseFreshDatabase();
            var last = PathOfCurrentDatabase(factory);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(new[] { earlier, later, last }.Distinct().Count(), Is.EqualTo(3));
                Assert.That(SchemaOf(earlier), Is.EqualTo(template));
                Assert.That(SchemaOf(later), Is.EqualTo(template));
                Assert.That(SchemaOf(last), Is.EqualTo(template));
            }
        }

        [Test]
        public void AFailedCopyIsRetriedAFewTimesAndThenFailsNamingTheDestination()
        {
            var attempts = 0;
            var destination = Path.Combine(Path.GetTempPath(), $"never-written-{Guid.NewGuid():N}.db");

            var failure = Assert.Throws<IOException>(() => TestDatabaseTemplate.CopyTo(destination, (_, _) =>
            {
                attempts++;
                throw new IOException("locked");
            }));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempts, Is.InRange(2, 10));
                Assert.That(failure.Message, Does.Contain(destination));
            }
        }

        [Test]
        public void ACopyThatSucceedsAfterATransientFailureLeavesTheDestinationComplete()
        {
            var attempts = 0;
            var destination = Path.Combine(Path.GetTempPath(), $"retried-{Guid.NewGuid():N}.db");

            try
            {
                TestDatabaseTemplate.CopyTo(destination, (template, target) =>
                {
                    if (++attempts < 3)
                    {
                        throw new IOException("locked");
                    }

                    File.Copy(template, target);
                });

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(attempts, Is.EqualTo(3));
                    Assert.That(SchemaOf(destination), Is.EqualTo(SchemaOf(TestDatabaseTemplate.Path)));
                }
            }
            finally
            {
                File.Delete(destination);
            }
        }

        [Test]
        public async Task ManyCallersBuildingTheTemplateAtOnceAllGetOneCompleteDatabase()
        {
            using var directory = new TemporaryDirectory();
            using var everyoneReady = new Barrier(8);

            var paths = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                everyoneReady.SignalAndWait();
                return TestDatabaseTemplate.EnsureBuilt(directory.Path);
            })));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(paths.Distinct().Count(), Is.EqualTo(1));
                Assert.That(SchemaOf(paths[0]), Is.EqualTo(SchemaBuiltDirectly(directory)));
                Assert.That(Directory.GetFiles(directory.Path).Where(file => !file.Contains("direct_")).Select(Path.GetFileName), Is.EqualTo(new[] { Path.GetFileName(paths[0]) }));
            }
        }

        [Test]
        public void TheTemplateIsLeftWithoutLogFilesNextToIt()
        {
            using var directory = new TemporaryDirectory();

            var path = TestDatabaseTemplate.EnsureBuilt(directory.Path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(path), Is.True);
                Assert.That(File.Exists($"{path}-wal"), Is.False);
                Assert.That(File.Exists($"{path}-shm"), Is.False);
            }
        }

        [Test]
        public void ATemplateAnotherProcessAlreadyMovedIntoPlaceIsKeptAsItIs()
        {
            using var directory = new TemporaryDirectory();
            var finalPath = TestDatabaseTemplate.EnsureBuilt(directory.Path);
            var bytesBefore = File.ReadAllBytes(finalPath);

            var again = TestDatabaseTemplate.EnsureBuilt(directory.Path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(again, Is.EqualTo(finalPath));
                Assert.That(File.ReadAllBytes(finalPath), Is.EqualTo(bytesBefore));
            }
        }

        [Test]
        public void ATemplateAnotherProcessIsStillBuildingIsNeitherUsedNorDisturbed()
        {
            using var directory = new TemporaryDirectory();
            var unfinished = Path.Combine(directory.Path, "template_0123456789abcdef.tmp");
            File.WriteAllText(unfinished, "half written");

            var path = TestDatabaseTemplate.EnsureBuilt(directory.Path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(path, Is.Not.EqualTo(unfinished));
                Assert.That(SchemaOf(path), Is.EqualTo(SchemaBuiltDirectly(directory)));
                Assert.That(File.ReadAllText(unfinished), Is.EqualTo("half written"));
            }
        }

        [Test]
        public void ATemplateLeftByAnEarlierSchemaIsNeverReused()
        {
            using var directory = new TemporaryDirectory();
            var stale = Path.Combine(directory.Path, "template_0000000000000000.db");
            File.WriteAllText(stale, "from an earlier schema");

            var path = TestDatabaseTemplate.EnsureBuilt(directory.Path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(path, Is.Not.EqualTo(stale));
                Assert.That(SchemaOf(path), Is.EqualTo(SchemaBuiltDirectly(directory)));
            }
        }

        [Test]
        public void TheTemplateOfThisProcessIsBuiltOnceAndComplete()
        {
            var first = TestDatabaseTemplate.Path;
            var second = TestDatabaseTemplate.Path;
            using var directory = new TemporaryDirectory();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(second, Is.EqualTo(first));
                Assert.That(SchemaOf(first), Is.EqualTo(SchemaBuiltDirectly(directory)));
            }
        }

        private static List<string> SchemaBuiltDirectly(TemporaryDirectory directory)
        {
            var path = Path.Combine(directory.Path, $"direct_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite($"DataSource={path};Pooling=False", DatabaseConfigurator.ConfigureSqlite)
                .Options;
            using (var context = new LighthouseAppContext(options, new FakeCryptoService(), NullLogger<LighthouseAppContext>.Instance))
            {
                context.Database.EnsureCreated();
            }

            return SchemaOf(path);
        }

        private static List<string> SchemaOf(string path)
        {
            using var connection = new SqliteConnection($"DataSource=file:{path}?mode=ro&immutable=1;Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT type || ' ' || name || ' ' || COALESCE(sql, '') FROM sqlite_master ORDER BY type, name";
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
            {
                rows.Add(reader.GetString(0));
            }

            return rows;
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lighthouse-template-{Guid.NewGuid():N}");

            public TemporaryDirectory()
            {
                Directory.CreateDirectory(Path);
            }

            public void Dispose()
            {
                Directory.Delete(Path, recursive: true);
            }
        }

        private static string PathOfCurrentDatabase(TestWebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.GetDbConnection().DataSource;
        }

        private static string? Execute(string connectionString, string sql)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return command.ExecuteScalar()?.ToString();
        }
    }
}
