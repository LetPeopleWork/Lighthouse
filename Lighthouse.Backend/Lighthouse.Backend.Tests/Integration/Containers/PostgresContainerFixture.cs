using Testcontainers.PostgreSql;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    public static class PostgresContainerFixture
    {
        public static async Task<PostgreSqlContainer> StartFreshAsync()
        {
            var container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();

            await container.StartAsync();
            return container;
        }
    }
}
