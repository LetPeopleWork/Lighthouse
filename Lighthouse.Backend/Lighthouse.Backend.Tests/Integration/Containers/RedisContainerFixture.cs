using Testcontainers.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    public static class RedisContainerFixture
    {
        public static async Task<RedisContainer> StartFreshAsync()
        {
            var container = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();

            await container.StartAsync();
            return container;
        }
    }
}
