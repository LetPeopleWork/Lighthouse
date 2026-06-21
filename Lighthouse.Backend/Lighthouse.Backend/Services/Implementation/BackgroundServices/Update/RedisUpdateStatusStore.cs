using Lighthouse.Backend.Services.Interfaces.Update;
using StackExchange.Redis;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class RedisUpdateStatusStore : IUpdateStatusStore
    {
        private const string StatusHashKey = "lighthouse:update-status";

        private static readonly LuaScript MonotonicAdvanceScript = LuaScript.Prepare(
            "local current = redis.call('HGET', @hashKey, @field)\n" +
            "if current == false then return -1 end\n" +
            "if tonumber(@to) >= tonumber(current) then\n" +
            "    redis.call('HSET', @hashKey, @field, @to)\n" +
            "    return tonumber(@to)\n" +
            "end\n" +
            "return tonumber(current)");

        private readonly IDatabase database;

        public RedisUpdateStatusStore(IConnectionMultiplexer multiplexer)
        {
            database = multiplexer.GetDatabase();
        }

        public bool TryAdmit(UpdateKey key, UpdateStatus status)
        {
            return database.HashSet(StatusHashKey, key.ToString(), (int)status.Status, When.NotExists);
        }

        public UpdateStatus? Advance(UpdateKey key, UpdateProgress to)
        {
            var resultingOrdinal = (long)database.ScriptEvaluate(
                MonotonicAdvanceScript,
                new { hashKey = (RedisKey)StatusHashKey, field = key.ToString(), to = (int)to });

            if (resultingOrdinal < 0)
            {
                return null;
            }

            return StatusFor(key, resultingOrdinal);
        }

        public bool TryGet(UpdateKey key, out UpdateStatus? status)
        {
            var value = database.HashGet(StatusHashKey, key.ToString());
            if (value.IsNull)
            {
                status = null;
                return false;
            }

            status = StatusFor(key, (long)value);
            return true;
        }

        public void Remove(UpdateKey key)
        {
            database.HashDelete(StatusHashKey, key.ToString());
        }

        public bool HasActiveWork()
        {
            return database.HashValues(StatusHashKey)
                .Select(value => (UpdateProgress)(int)(long)value)
                .Any(progress => progress is UpdateProgress.Queued or UpdateProgress.InProgress);
        }

        private static UpdateStatus StatusFor(UpdateKey key, long ordinal)
        {
            return new UpdateStatus
            {
                UpdateType = key.UpdateType,
                Id = key.Id,
                Status = (UpdateProgress)(int)ordinal,
            };
        }
    }
}
