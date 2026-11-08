using System.Collections.Concurrent;
using System.Data.Common;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What a scenario needs to drive an application service with no web host: a database of its own, built and
    /// connected the way the host's is so that foreign keys and file behaviour match, and a container holding only
    /// the context, logging, the clock and what the fixture registers. It seeds nothing and holds no static state,
    /// so fixtures stay parallel. A fixture creates one in its set-up and disposes it in its tear-down.
    /// </summary>
    public sealed class ServicePortHarness : IDisposable
    {
        private readonly ServiceProvider services;

        private readonly List<IServiceScope> scopesHandedOut = [];

        private bool disposed;

        public ServicePortHarness(Action<IServiceCollection>? registerWhatThePortNeeds = null)
        {
            DatabasePath = Path.Combine(Path.GetTempPath(), $"service-port-{Guid.NewGuid():N}.db");
            Writes = new DatabaseWriteWatch(DatabasePath);
            services = ComposeTheContainer(registerWhatThePortNeeds);
            TestDatabaseTemplate.CopyTo(DatabasePath);
            Writes.StartWatchingTheFile();
        }

        public string DatabasePath { get; }

        /// <summary>Sees what is written to this database: by its own contexts, and by any other connection.</summary>
        public DatabaseWriteWatch Writes { get; }

        /// <summary>One step of a scenario. Each scope has its own context, so a step reads what is stored.</summary>
        public IServiceScope NewScope()
        {
            var scope = services.CreateScope();
            scopesHandedOut.Add(scope);
            return scope;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            // A scenario that failed never reached the end of its own scopes, and a database that is still open
            // cannot be deleted on Windows.
            scopesHandedOut.ForEach(scope => scope.Dispose());
            services.Dispose();
            Writes.Dispose();

            foreach (var file in new[] { DatabasePath, $"{DatabasePath}-wal", $"{DatabasePath}-shm", $"{DatabasePath}-journal" })
            {
                DeleteOnceNothingHoldsIt(file);
            }
        }

        private ServiceProvider ComposeTheContainer(Action<IServiceCollection>? registerWhatThePortNeeds)
        {
            var collection = new ServiceCollection();
            collection.AddLogging();
            collection.AddSingleton<ICryptoService, FakeCryptoService>();
            collection.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(TestToday.FrozenNow));
            collection.AddDbContext<LighthouseAppContext>(options =>
            {
                options.UseSqlite($"DataSource={DatabasePath};Pooling=False", DatabaseConfigurator.ConfigureSqlite);
                options.AddInterceptors(NoWaitingForTheDisk.Instance, Writes);
            });
            registerWhatThePortNeeds?.Invoke(collection);

            return collection.BuildServiceProvider();
        }

        // On Windows a file stays held for a moment after its last connection closes, and deleting it then is
        // refused. Linux never reports this. Waiting briefly is enough: the file is being released, not kept.
        private static void DeleteOnceNothingHoldsIt(string file)
        {
            const int attempts = 40;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    File.Delete(file);
                    return;
                }
                catch (IOException) when (attempt < attempts)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }

    /// <summary>What was written while a step ran.</summary>
    public sealed record WritesSeen(IReadOnlyList<string> StatementsThatWrote, bool TheFileChanged);

    /// <summary>
    /// Two views of what a step writes. The command counter names each statement that writes, but it sees only the
    /// contexts built on the harness's options: a second context on its own connection, or a bare connection, passes
    /// it by. So a watcher connection also reads SQLite's data version, which moves whenever any other connection
    /// commits to the file. The watcher shares no cache with anyone, as under a shared cache the version would not move.
    /// An UPDATE that stores the value already there is caught by the counter only. A write rolled back changes nothing
    /// in the file, so the file never sees it, and the counter names it only if a harness context sent it.
    /// </summary>
    public sealed partial class DatabaseWriteWatch(string databasePath) : DbCommandInterceptor, IDisposable
    {
        private readonly ConcurrentQueue<string> statementsThatWrote = new();

        private readonly SqliteConnection watcher = new($"Data Source={databasePath};Pooling=False;Cache=Private");

        public WritesSeen During(Action step)
        {
            var versionBefore = StartAfresh();
            step();
            return WhatWasWrittenSince(versionBefore);
        }

        public async Task<WritesSeen> DuringAsync(Func<Task> step)
        {
            var versionBefore = StartAfresh();
            await step();
            return WhatWasWrittenSince(versionBefore);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            NoteItIfItWrites(command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            NoteItIfItWrites(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            NoteItIfItWrites(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            NoteItIfItWrites(command);
            return ValueTask.FromResult(result);
        }

        public void Dispose() => watcher.Dispose();

        internal void StartWatchingTheFile() => watcher.Open();

        private long StartAfresh()
        {
            statementsThatWrote.Clear();
            return DataVersion();
        }

        private WritesSeen WhatWasWrittenSince(long versionBefore)
            => new([.. statementsThatWrote], DataVersion() != versionBefore);

        private long DataVersion()
        {
            using var pragma = watcher.CreateCommand();
            pragma.CommandText = "PRAGMA data_version;";
            return (long)pragma.ExecuteScalar()!;
        }

        private void NoteItIfItWrites(DbCommand command)
        {
            if (AStatementThatWrites().IsMatch(command.CommandText))
            {
                statementsThatWrote.Enqueue(command.CommandText);
            }
        }

        // EF sends a batch as one command, and any statement in it may be the write. A statement that opens with
        // WITH writes when its named queries are followed by a write rather than by a SELECT.
        [GeneratedRegex(@"(?:^|;)\s*(?:WITH\b[^;]*?\)\s*)?(?:INSERT|UPDATE|DELETE|REPLACE|CREATE|DROP|ALTER)\b", RegexOptions.IgnoreCase)]
        private static partial Regex AStatementThatWrites();
    }
}
