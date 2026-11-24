using Testcontainers.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    public static class RedisContainerFixture
    {
        // The Redis tests call the store's synchronous API from many thread-pool tasks at once, while the rest of
        // the suite runs in parallel. StackExchange.Redis reads every reply on the thread pool, so when blocked
        // callers fill it, the replies that would release them wait in the queue and the calls time out after 5 s.
        // The pool grows on its own only slowly; a higher floor lets it hand out threads at once.
        private const int ThreadPoolWorkerFloor = 128;

        public static async Task<RedisContainer> StartFreshAsync()
        {
            KeepEnoughThreadsForRedisReplies();

            var container = new RedisBuilder("redis:7-alpine")
                .Build();

            await container.StartAsync();
            return container;
        }

        private static void KeepEnoughThreadsForRedisReplies()
        {
            ThreadPool.GetMinThreads(out var workers, out var completionPorts);
            if (workers < ThreadPoolWorkerFloor)
            {
                ThreadPool.SetMinThreads(ThreadPoolWorkerFloor, completionPorts);
            }
        }
    }
}
