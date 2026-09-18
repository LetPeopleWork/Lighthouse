using Microsoft.Data.Sqlite;
using NUnit.Framework;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// ADO #5877 slice 01's pre-slice probe, in the shape Epic #5511's cancellation-reach probe
    /// established: a measurement the design rests on, kept in the ordinary suite so it keeps being
    /// true.
    ///
    /// The question DESIGN could not answer on paper: three lanes mean up to three saves at once where
    /// there has only ever been one, and the user who reported this runs standalone on SQLite. Nothing
    /// else in that configuration serialises them — <c>InProcessUpdateExecutionLock.AcquireAsync</c>
    /// hands back a no-op scope, so the database file's own locking is all there is between three lanes
    /// and a lost write.
    ///
    /// What is claimed, and all that is claimed: with the PRAGMAs the product actually sets — WAL on,
    /// <c>busy_timeout</c> at 10 000 ms, <c>synchronous</c> at NORMAL — concurrent writers to one file
    /// wait for each other rather than failing, and every save lands. No duration measured on a
    /// developer's machine is asserted anywhere here; a wall-clock budget taken locally is a red build
    /// waiting for a loaded runner.
    ///
    /// The second test is a positive control and is the reason the first is worth anything. A probe
    /// that cannot go red is green forever and guards nothing, so the fixture also shows that a writer
    /// held out longer than the instance is willing to wait IS refused, and that this fixture sees the
    /// refusal. It also records the shape of the real ceiling, which is wider than it first looks:
    /// <c>busy_timeout</c> is only the first half, because <c>Microsoft.Data.Sqlite</c> retries a
    /// refused statement on its own until the command timeout runs out — thirty seconds by default, on
    /// top of the ten this instance configures.
    ///
    /// One thing this probe found that is worth knowing before reading its green as reassurance: under
    /// sustained pressure the lanes do not take turns. Reading the rows back in commit order shows
    /// three unbroken blocks — the lane holding the file keeps re-taking it while the others sit in
    /// SQLite's backoff, so they wait rather than fail but they wait for the whole of the incumbent's
    /// run. That is safe, and it is not fair. It matters little in practice because a refresh spends
    /// nearly all of its time inside connector calls and nothing holds a write transaction across one,
    /// so real saves are short and far apart; it would matter a great deal if a lane ever started
    /// saving in a tight loop.
    ///
    /// If the first test ever goes red, the honest options are the ones DESIGN named: a write-
    /// serialising gate in front of the lanes, or per-lane save batching. Both are cheaper to find here
    /// than after the lanes ship to somebody running on SQLite.
    /// </summary>
    [TestFixture]
    [Category("story-5877-update-queue-lanes")]
    [Category("slice-01-probe")]
    [Category("real-io")]
    public class ThreeLaneSqliteWriteProbe
    {
        /// <summary>
        /// Copied from <c>DatabaseConfigurator</c> rather than referenced, so that a change to what the
        /// product sets shows up here as a probe that has stopped describing the product.
        /// </summary>
        private const string TheProductionPragmas = @"
                    PRAGMA journal_mode=WAL;
                    PRAGMA busy_timeout=10000;
                    PRAGMA synchronous=NORMAL;
                ";

        /// <summary>
        /// Team, Portfolio and Forecast. The probe is about the number of lanes rather than about what
        /// runs in them.
        /// </summary>
        private const int Lanes = 3;

        private const int SavesPerLane = 40;

        /// <summary>
        /// Rows per save. A save of a single row holds the write lock for less time than it takes
        /// another lane to ask for it, so three of them take turns by luck rather than contending. A
        /// refresh saves an entity's whole work-item set, so a save of some size is also the more
        /// honest shape.
        /// </summary>
        private const int RowsPerSave = 400;

        private const string TakeTheWriteLock = "BEGIN IMMEDIATE;";

        private string databaseDirectory = null!;

        private string connectionString = null!;

        /// <summary>
        /// Holds all three lanes at the gate until every one of them is ready, so they start together.
        /// Lanes started one after another can finish one after another, which is the arrangement this
        /// probe exists to avoid arranging.
        /// </summary>
        private Barrier everyLaneStartsTogether = null!;

        private int lanesWantingTheFile;

        private int mostLanesSavingAtOnce;

        /// <summary>
        /// Counted from the moment a lane asks for the write lock rather than from the moment it gets
        /// it, because a lane waiting at the door is exactly what contention is. Without this the probe
        /// cannot tell three lanes that queued for one file from three that happened to take turns, and
        /// the difference is the whole question.
        /// </summary>
        private void RecordThatOneMoreLaneWantsTheFile()
        {
            var wanting = Interlocked.Increment(ref lanesWantingTheFile);
            var highest = Volatile.Read(ref mostLanesSavingAtOnce);

            while (wanting > highest
                && Interlocked.CompareExchange(ref mostLanesSavingAtOnce, wanting, highest) != highest)
            {
                highest = Volatile.Read(ref mostLanesSavingAtOnce);
            }
        }

        [SetUp]
        public void CreateAFreshDatabaseFile()
        {
            databaseDirectory = Path.Combine(Path.GetTempPath(), $"ThreeLaneSqliteProbe_{Guid.NewGuid():N}");
            Directory.CreateDirectory(databaseDirectory);
            everyLaneStartsTogether = new Barrier(Lanes);
            lanesWantingTheFile = 0;
            mostLanesSavingAtOnce = 0;

            // Pooling is off so the file is genuinely closed when the writers are done. The SQLite
            // connection pool is process-wide, and a pooled connection left holding this file cannot be
            // deleted on Windows - which fails the teardown rather than the claim.
            connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(databaseDirectory, "probe.db"),
                Pooling = false,
            }.ToString();

            using var connection = OpenConfiguredConnection();
            using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE IF NOT EXISTS Saves (Lane INTEGER NOT NULL, Sequence INTEGER NOT NULL);";
            create.ExecuteNonQuery();
        }

        [TearDown]
        public void RemoveTheDatabaseFile()
        {
            everyLaneStartsTogether.Dispose();

            try
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A file still held open is a tidiness problem, not a claim about the product.
            }
        }

        [Test]
        public async Task Three_lanes_saving_at_once_wait_for_each_other_at_the_file_rather_than_failing()
        {
            var refusals = new ConcurrentBag<string>();

            var lanes = Enumerable
                .Range(0, Lanes)
                .Select(lane => SaveRepeatedlyFrom(lane, refusals))
                .ToList();

            await Task.WhenAll(lanes);

            var saved = CountOfSavesOnFile();
            var lanesSavingAtOnce = Volatile.Read(ref mostLanesSavingAtOnce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lanesSavingAtOnce, Is.GreaterThan(1),
                    "Read first, because everything below is worthless without it. Lanes that never wanted the "
                    + "file at the same time never contended for it, and a probe about contention that never "
                    + "contends passes whatever the settings are.");
                Assert.That(refusals, Is.Empty,
                    "Three lanes mean up to three saves at once where there has only ever been one, and on the "
                    + "standalone path nothing else serialises them. A refused write is a refresh whose results "
                    + "are lost with nobody told - and the instance that reported this bug is the one running "
                    + $"this way. Refused: {string.Join(" | ", refusals)}");
                Assert.That(saved, Is.EqualTo(Lanes * SavesPerLane * RowsPerSave),
                    "Waiting for each other is the whole of the claim: every save has to land, not most of them.");
            }
        }

        /// <summary>
        /// The positive control. Without it the test above passes whatever the settings are, because
        /// three writers that never actually contend never fail — which is how a probe becomes green
        /// forever and stops meaning anything.
        ///
        /// Nothing here is timed against a machine. The blocking transaction is held open until the
        /// assertion is done, so the writer that is kept out is kept out for certain, and the only
        /// number is the one this test configures as its own patience.
        /// </summary>
        [Test]
        public void A_writer_kept_out_past_what_the_instance_will_wait_is_refused_and_this_probe_sees_it()
        {
            using var holdingTheWriteLock = OpenConfiguredConnection();
            Execute(holdingTheWriteLock, TakeTheWriteLock, TheProvidersOrdinaryPatienceSeconds);
            Execute(holdingTheWriteLock, "INSERT INTO Saves (Lane, Sequence) VALUES (0, 0);", TheProvidersOrdinaryPatienceSeconds);

            using var keptOut = OpenConnectionThatWillNotWait();

            SqliteException? refused = null;

            try
            {
                Execute(keptOut, TakeTheWriteLock, WhatThisTestIsWillingToWaitSeconds);
            }
            catch (SqliteException busy)
            {
                refused = busy;
            }

            Execute(holdingTheWriteLock, "ROLLBACK;", TheProvidersOrdinaryPatienceSeconds);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused, Is.Not.Null,
                    "One file takes one writer at a time, so a writer held out has to be refused eventually. If it "
                    + "never is, this fixture cannot tell a database that serialises from one that silently drops "
                    + "writes, and the test above is worth nothing.");
                Assert.That(refused?.SqliteErrorCode, Is.EqualTo(SqliteBusy),
                    "The refusal an instance under write pressure gets is SQLITE_BUSY, and that is the one the test "
                    + $"above is asserting the absence of. Got: {refused?.Message}");
            }
        }

        /// <summary>
        /// How long the provider goes on re-asking after SQLite itself has refused. Its own default, in
        /// seconds, written here because the ordinary path has to be spelled out where the positive
        /// control turns it off.
        /// </summary>
        private const int TheProvidersOrdinaryPatienceSeconds = 30;

        /// <summary>
        /// SQLITE_BUSY. Named rather than written as a bare 5 at the assertion, because the number on
        /// its own says nothing to the next reader.
        /// </summary>
        private const int SqliteBusy = 5;

        /// <summary>
        /// How long this test is prepared to be kept waiting before calling the refusal proven. It is
        /// the test's own patience rather than a claim about anything the product does, and the
        /// transaction blocking it is held open regardless, so a loaded runner makes this slower and
        /// never makes it wrong.
        /// </summary>
        private const int WhatThisTestIsWillingToWaitSeconds = 1;

        private Task SaveRepeatedlyFrom(int lane, ConcurrentBag<string> refusals)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var connection = OpenConfiguredConnection();

                    everyLaneStartsTogether.SignalAndWait();

                    for (var save = 0; save < SavesPerLane; save++)
                    {
                        // A transaction per save, because that is the shape of the thing being asked
                        // about: one refresh finishing its work and committing, three of them at once.
                        // It takes the write lock at BEGIN rather than at the first row, which is what a
                        // save that has already decided what it is writing does.
                        RecordThatOneMoreLaneWantsTheFile();
                        Execute(connection, TakeTheWriteLock, TheProvidersOrdinaryPatienceSeconds);

                        using (var insert = connection.CreateCommand())
                        {
                            insert.CommandText = "INSERT INTO Saves (Lane, Sequence) VALUES ($lane, $sequence);";
                            insert.Parameters.AddWithValue("$lane", lane);
                            var sequenceParameter = insert.Parameters.AddWithValue("$sequence", 0);

                            for (var row = 0; row < RowsPerSave; row++)
                            {
                                sequenceParameter.Value = (save * RowsPerSave) + row;
                                insert.ExecuteNonQuery();
                            }
                        }

                        Execute(connection, "COMMIT;", TheProvidersOrdinaryPatienceSeconds);
                        Interlocked.Decrement(ref lanesWantingTheFile);
                    }
                }
                catch (SqliteException refused)
                {
                    refusals.Add($"lane {lane}: {refused.SqliteErrorCode}/{refused.SqliteExtendedErrorCode} {refused.Message}");
                }
            });
        }

        private static void Execute(SqliteConnection connection, string sql, int commandTimeoutSeconds)
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = commandTimeoutSeconds;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private SqliteConnection OpenConfiguredConnection()
        {
            // Each writer gets its own connection, because that is what the product does: AddDbContext
            // uses the (provider, options) overload, so every DbContext rebuilds its options and opens
            // its own.
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            Execute(connection, TheProductionPragmas, TheProvidersOrdinaryPatienceSeconds);

            return connection;
        }

        /// <summary>
        /// The same file with the waiting turned off, which is what the positive control needs and what
        /// no instance runs. Both halves have to go: the PRAGMA is how long SQLite itself waits, and
        /// the command timeout is how long the .NET provider goes on re-asking after SQLite has
        /// refused. DESIGN read the 10 000 ms PRAGMA as the ceiling; the provider's own retry sits on
        /// top of it, so the real ceiling for a save under contention is the wider of the two.
        /// </summary>
        private SqliteConnection OpenConnectionThatWillNotWait()
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            Execute(connection, "PRAGMA busy_timeout=0;", TheProvidersOrdinaryPatienceSeconds);

            return connection;
        }

        private int CountOfSavesOnFile()
        {
            using var connection = OpenConfiguredConnection();
            using var count = connection.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM Saves;";

            return Convert.ToInt32(count.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
