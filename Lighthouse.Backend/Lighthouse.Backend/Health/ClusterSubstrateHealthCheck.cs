using Lighthouse.Backend.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;

namespace Lighthouse.Backend.Health
{
    public sealed class ClusterSubstrateHealthCheck : IHealthCheck
    {
        private const long ProbeAdvisoryKey = 7733551L;
        private const string ProbeHashKey = "lighthouse:substrate-probe";

        private readonly string postgresConnectionString;
        private readonly IConnectionMultiplexer multiplexer;
        private readonly Lazy<Task<HealthCheckResult>> probe;

        public ClusterSubstrateHealthCheck(IOptions<DatabaseConfiguration> databaseConfiguration, IConnectionMultiplexer multiplexer)
        {
            postgresConnectionString = databaseConfiguration.Value.ConnectionString;
            this.multiplexer = multiplexer;
            probe = new Lazy<Task<HealthCheckResult>>(RunProbeAsync);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return probe.Value;
        }

        private async Task<HealthCheckResult> RunProbeAsync()
        {
            try
            {
                if (!await AdvisoryLockIsMutuallyExclusiveAsync())
                {
                    return Refused("advisory lock is not mutually exclusive — the connection pooler is likely in transaction mode; use session-mode pooling");
                }

                if (!await SharedStoreDedupsAdmissionAsync())
                {
                    return Refused("shared status-store admission is not atomic — HSETNX did not dedup a repeated key");
                }

                if (!await AdvisoryLockReclaimsOnHolderDeathAsync())
                {
                    return Refused("advisory lock was not reclaimed after the holding connection died");
                }

                return HealthCheckResult.Healthy(
                    "Cluster substrate probe passed: advisory-lock mutual exclusion, shared-store dedup, and reclaim on holder death.");
            }
            catch (Exception ex)
            {
                return Refused($"cluster substrate probe could not run against Postgres + Redis: {ex.Message}");
            }
        }

        private static HealthCheckResult Refused(string lie) =>
            HealthCheckResult.Unhealthy($"health.startup.refused: {lie}");

        private async Task<bool> AdvisoryLockIsMutuallyExclusiveAsync()
        {
            await using var holder = new NpgsqlConnection(postgresConnectionString);
            await using var contender = new NpgsqlConnection(postgresConnectionString);
            await holder.OpenAsync();
            await contender.OpenAsync();

            var holderWon = await TryAdvisoryLockAsync(holder, ProbeAdvisoryKey);
            var contenderWon = await TryAdvisoryLockAsync(contender, ProbeAdvisoryKey);

            if (holderWon)
            {
                await AdvisoryUnlockAsync(holder, ProbeAdvisoryKey);
            }

            return holderWon && !contenderWon;
        }

        private async Task<bool> SharedStoreDedupsAdmissionAsync()
        {
            var database = multiplexer.GetDatabase();
            var probeField = Guid.NewGuid().ToString("N");

            try
            {
                var firstAdmission = await database.HashSetAsync(ProbeHashKey, probeField, 0, When.NotExists);
                var secondAdmission = await database.HashSetAsync(ProbeHashKey, probeField, 0, When.NotExists);
                return firstAdmission && !secondAdmission;
            }
            finally
            {
                await database.HashDeleteAsync(ProbeHashKey, probeField);
            }
        }

        private async Task<bool> AdvisoryLockReclaimsOnHolderDeathAsync()
        {
            var holder = new NpgsqlConnection(postgresConnectionString);
            await holder.OpenAsync();
            await TryAdvisoryLockAsync(holder, ProbeAdvisoryKey);

            await TerminateBackendAsync(holder.ProcessID);
            await holder.DisposeAsync();

            await using var reclaimer = new NpgsqlConnection(postgresConnectionString);
            await reclaimer.OpenAsync();
            var reclaimed = await TryAdvisoryLockAsync(reclaimer, ProbeAdvisoryKey);

            if (reclaimed)
            {
                await AdvisoryUnlockAsync(reclaimer, ProbeAdvisoryKey);
            }

            return reclaimed;
        }

        private async Task TerminateBackendAsync(int backendProcessId)
        {
            await using var terminator = new NpgsqlConnection(postgresConnectionString);
            await terminator.OpenAsync();
            await using var command = terminator.CreateCommand();
            command.CommandText = "SELECT pg_terminate_backend(@pid)";
            command.Parameters.AddWithValue("pid", backendProcessId);
            await command.ExecuteScalarAsync();
        }

        private static async Task<bool> TryAdvisoryLockAsync(NpgsqlConnection connection, long key)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_try_advisory_lock(@key)";
            command.Parameters.AddWithValue("key", key);
            return (bool)(await command.ExecuteScalarAsync())!;
        }

        private static async Task AdvisoryUnlockAsync(NpgsqlConnection connection, long key)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_advisory_unlock(@key)";
            command.Parameters.AddWithValue("key", key);
            await command.ExecuteScalarAsync();
        }
    }
}
