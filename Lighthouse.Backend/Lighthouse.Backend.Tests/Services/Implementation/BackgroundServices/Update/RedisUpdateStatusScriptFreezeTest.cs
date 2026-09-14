using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Epic #5511 slice 03, AC-03.2. Two guarantees the update queue rests on are enforced inside Lua over
    /// a bare number: a key cannot move backwards through <c>Advance</c>, and a key another replica already
    /// removed cannot be resurrected by <c>Requeue</c>. Slice 03 adds admission and start moments to the
    /// same store, and the way it was allowed to do that was by not touching either script — the moments go
    /// in a sibling hash written outside them (ADR-182).
    ///
    /// "Not touching them" is the whole safety argument, so it is asserted rather than asserted-about. The
    /// text below was copied from the scripts as they shipped before the moments existed; if a future
    /// change has to alter one, the guarantee it carries has to be re-argued and re-tested against a real
    /// Redis, and changing this literal is where that conversation starts.
    ///
    /// No Redis needed: the scripts are compared as text, so this runs everywhere the suite does.
    /// </summary>
    [TestFixture]
    public class RedisUpdateStatusScriptFreezeTest
    {
        private const string MonotonicAdvanceAsItShipped =
            "local current = redis.call('HGET', @hashKey, @field)\n" +
            "if current == false then return -1 end\n" +
            "if tonumber(@to) >= tonumber(current) then\n" +
            "    redis.call('HSET', @hashKey, @field, @to)\n" +
            "    return tonumber(@to)\n" +
            "end\n" +
            "return tonumber(current)";

        private const string RequeueIfAdmittedAsItShipped =
            "if redis.call('HEXISTS', @hashKey, @field) == 1 then\n" +
            "    redis.call('HSET', @hashKey, @field, @to)\n" +
            "    return 1\n" +
            "end\n" +
            "return 0";

        [Test]
        public void MonotonicAdvanceScript_IsCharacterForCharacterWhatShipped()
        {
            Assert.That(RedisUpdateStatusStore.MonotonicAdvanceScript.OriginalScript, Is.EqualTo(MonotonicAdvanceAsItShipped),
                "Monotonic advance across replicas is this comparison. Nothing slice 03 adds is worth re-opening it, "
                + "and a change here that looks harmless is how a key starts going backwards under load.");
        }

        [Test]
        public void RequeueIfAdmittedScript_IsCharacterForCharacterWhatShipped()
        {
            Assert.That(RedisUpdateStatusStore.RequeueIfAdmittedScript.OriginalScript, Is.EqualTo(RequeueIfAdmittedAsItShipped),
                "The HEXISTS guard is what stops a key another pod already removed being resurrected into a phantom "
                + "active entry that never completes, which an operator reads as an instance permanently busy.");
        }

        [Test]
        public void BothScripts_StillTranslateToTheSameThingRedisActuallyRuns()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(RedisUpdateStatusStore.MonotonicAdvanceScript.ExecutableScript, Is.EqualTo(Executable(MonotonicAdvanceAsItShipped)));
                Assert.That(RedisUpdateStatusStore.RequeueIfAdmittedScript.ExecutableScript, Is.EqualTo(Executable(RequeueIfAdmittedAsItShipped)));
            }
        }

        /// <summary>
        /// The text above is what an author reads; what Redis runs is the KEYS/ARGV form the client rewrites
        /// it into. Freezing only the readable half would miss a change in how the parameters are bound,
        /// which is a change to the script even though the source looks untouched.
        /// </summary>
        private static string Executable(string original)
            => StackExchange.Redis.LuaScript.Prepare(original).ExecutableScript;
    }
}
