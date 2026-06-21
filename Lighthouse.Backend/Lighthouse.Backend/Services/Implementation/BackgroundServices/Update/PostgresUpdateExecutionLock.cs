using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public sealed class PostgresUpdateExecutionLock : IUpdateExecutionLock
    {
        private readonly string connectionString;

        public PostgresUpdateExecutionLock(IOptions<DatabaseConfiguration> databaseConfiguration)
        {
            connectionString = databaseConfiguration.Value.ConnectionString;
        }

        public async Task<IAsyncDisposable> AcquireAsync(UpdateKey key, CancellationToken cancellationToken = default)
        {
            var lockKey = AdvisoryKey(key);
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            try
            {
                await ExecuteLockCommandAsync(connection, "SELECT pg_advisory_lock(@key)", lockKey, cancellationToken);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }

            return new AdvisoryLockScope(connection, lockKey);
        }

        private static long AdvisoryKey(UpdateKey key) => (long)(int)key.UpdateType << 32 | (uint)key.Id;

        private static async Task ExecuteLockCommandAsync(NpgsqlConnection connection, string sql, long lockKey, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("key", lockKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private sealed class AdvisoryLockScope : IAsyncDisposable
        {
            private readonly NpgsqlConnection connection;
            private readonly long lockKey;

            public AdvisoryLockScope(NpgsqlConnection connection, long lockKey)
            {
                this.connection = connection;
                this.lockKey = lockKey;
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    await ExecuteLockCommandAsync(connection, "SELECT pg_advisory_unlock(@key)", lockKey, CancellationToken.None);
                }
                finally
                {
                    await connection.DisposeAsync();
                }
            }
        }
    }
}
