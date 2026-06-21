namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class UpdateStatusStoreContainerTests
    {
        [Test]
        [Ignore("pending — DELIVER (epic-5305-k8s-readiness slice-07)")]
        public async Task SharedAdvance_ConcurrentWriters_OrdinalNeverRegresses()
        {
            await Task.CompletedTask;
            Assert.Fail(
                "Scenario #40 (US-07 INV-1, @requires-docker). " +
                "Given a shared RedisUpdateStatusStore backed by a real Redis container, " +
                "When many writers concurrently Advance the same UpdateKey through Queued→InProgress→Completed " +
                "interleaved with stale lower-ordinal writes, " +
                "Then no reader ever observes a regressed UpdateProgress ordinal " +
                "(monotonic CAS-on-ordinal via Lua/WATCH, not blind LWW). " +
                "Seed: RedisContainerFixture.StartFreshAsync(), one RedisUpdateStatusStore per simulated pod sharing the connection string.");
        }
    }
}
