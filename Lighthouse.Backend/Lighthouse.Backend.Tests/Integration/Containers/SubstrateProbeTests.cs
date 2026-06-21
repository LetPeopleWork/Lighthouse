using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Npgsql;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class SubstrateProbeTests
    {
        [Test]
        public async Task AdvisoryLock_TwoConnectionsSameKey_ExactlyOneAcquires()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            var key = AdvisoryKey(UpdateType.Team, 42);

            await using var firstHolder = new NpgsqlConnection(postgres.GetConnectionString());
            await using var contender = new NpgsqlConnection(postgres.GetConnectionString());
            await firstHolder.OpenAsync();
            await contender.OpenAsync();

            var firstWon = await TryAdvisoryLock(firstHolder, key);
            var contenderWon = await TryAdvisoryLock(contender, key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstWon, Is.True, "the first connection acquires the per-entity advisory lock");
                Assert.That(contenderWon, Is.False,
                    "a second connection contending for the same UpdateKey-derived lock must be refused — the " +
                    "advisory lock is the cluster-wide single-active-lifecycle-per-UpdateKey boundary (Option B / INV-4). " +
                    "A false-positive here would mean a pooler in transaction mode silently broke session affinity.");
            }
        }

        [Test]
        public async Task SharedStatusStore_SameKeyAdmittedTwice_SecondRefused()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
            var database = multiplexer.GetDatabase();
            var field = AdvisoryKey(UpdateType.Features, 7).ToString();

            var firstAdmission = await database.HashSetAsync(StatusHash, field, nameof(UpdateProgress.Queued), When.NotExists);
            var secondAdmission = await database.HashSetAsync(StatusHash, field, nameof(UpdateProgress.Queued), When.NotExists);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstAdmission, Is.True, "the first admission writes the status field (HSETNX succeeds)");
                Assert.That(secondAdmission, Is.False,
                    "a second admission of the same UpdateKey while one is in flight is refused (soft cluster-wide " +
                    "dedup) so two pods don't both enqueue and run the same update in the common case");
            }
        }

        [Test]
        public async Task AdvisoryLock_HolderConnectionDropped_LockReclaimed()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            var key = AdvisoryKey(UpdateType.Team, 99);

            var holder = new NpgsqlConnection(postgres.GetConnectionString());
            await holder.OpenAsync();
            var holderAcquired = await TryAdvisoryLock(holder, key);

            await ForciblyTerminateBackendAsync(postgres.GetConnectionString(), holder);
            await holder.DisposeAsync();

            await using var reclaimer = new NpgsqlConnection(postgres.GetConnectionString());
            await reclaimer.OpenAsync();
            var reclaimed = await TryAdvisoryLock(reclaimer, key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holderAcquired, Is.True, "the holder acquires the per-entity lock");
                Assert.That(reclaimed, Is.True,
                    "a session-scoped advisory lock auto-releases when the holding backend dies, so a pod killed " +
                    "mid-update frees the lock with no TTL/fencing machinery (Earned-Trust reclaim-on-holder-death)");
            }
        }

        private const string StatusHash = "lighthouse:update-status";

        private static long AdvisoryKey(UpdateType updateType, int id) => (long)(int)updateType << 32 | (uint)id;

        private static async Task<bool> TryAdvisoryLock(NpgsqlConnection connection, long key)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_try_advisory_lock(@key)";
            command.Parameters.AddWithValue("key", key);
            return (bool)(await command.ExecuteScalarAsync())!;
        }

        private static async Task ForciblyTerminateBackendAsync(string connectionString, NpgsqlConnection holder)
        {
            var holderPid = holder.ProcessID;

            await using var terminator = new NpgsqlConnection(connectionString);
            await terminator.OpenAsync();
            await using var command = terminator.CreateCommand();
            command.CommandText = "SELECT pg_terminate_backend(@pid)";
            command.Parameters.AddWithValue("pid", holderPid);
            await command.ExecuteScalarAsync();
        }
    }
}
