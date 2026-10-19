using Npgsql;
using Testcontainers.PostgreSql;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// One Postgres container for a whole fixture, handing each test a database of its own. Starting a
    /// container takes seconds; a new database inside a running one takes milliseconds, and keeps every
    /// test's rows out of every other test's way just the same.
    /// </summary>
    public sealed class SharedPostgresContainer : IAsyncDisposable
    {
        private readonly PostgreSqlContainer container;

        private SharedPostgresContainer(PostgreSqlContainer container)
        {
            this.container = container;
        }

        public static async Task<SharedPostgresContainer> StartAsync()
            => new(await PostgresContainerFixture.StartFreshAsync());

        /// <summary>
        /// Names a database nobody has used yet. It does not exist until something creates it: EF's
        /// migrator does so on its first run against it.
        /// </summary>
        public string ConnectionStringToANewDatabase()
        {
            var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
            {
                Database = $"t_{Guid.NewGuid():N}",
            };
            return builder.ConnectionString;
        }

        /// <summary>
        /// Like <see cref="ConnectionStringToANewDatabase"/>, but the database exists before it is
        /// returned, for code that opens a connection of its own before anything migrates.
        /// </summary>
        public async Task<string> ConnectionStringToANewCreatedDatabaseAsync()
        {
            var connectionString = ConnectionStringToANewDatabase();
            var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;

            await using var connection = new NpgsqlConnection(container.GetConnectionString());
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
            await command.ExecuteNonQueryAsync();

            return connectionString;
        }

        public ValueTask DisposeAsync() => container.DisposeAsync();
    }
}
