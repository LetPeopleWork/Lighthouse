using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Lighthouse.Backend.Services.Interfaces.Update;
using StackExchange.Redis;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class RedisUpdateStatusStore : IUpdateStatusStore
    {
        private const string StatusHashKey = "lighthouse:update-status";

        /// <summary>
        /// The moments live in their own hash, field-for-field alongside the ordinal, and are written and
        /// deleted by ordinary commands rather than from inside either script. That is what keeps the two
        /// guarantees the scripts carry - a key cannot go backwards, and a key another replica removed
        /// cannot be resurrected - provably unaffected by anything recorded here. ADR-182.
        ///
        /// The consequence is that a moment can be missing while its ordinal exists, and that is a normal
        /// state rather than a fault: a replica still on an older build records nothing at all.
        /// </summary>
        private const string MomentsHashKey = "lighthouse:update-moments";

        // internal rather than private so that RedisUpdateStatusScriptFreezeTest can compare them
        // character for character against the text that shipped before the moments existed.
        internal static readonly LuaScript MonotonicAdvanceScript = LuaScript.Prepare(
            "local current = redis.call('HGET', @hashKey, @field)\n" +
            "if current == false then return -1 end\n" +
            "if tonumber(@to) >= tonumber(current) then\n" +
            "    redis.call('HSET', @hashKey, @field, @to)\n" +
            "    return tonumber(@to)\n" +
            "end\n" +
            "return tonumber(current)");

        // Redis has no HSETXX and StackExchange.Redis rejects When.Exists on HashSet (only Always /
        // NotExists are legal), so the "reset only an already-admitted key" guard needs a script.
        internal static readonly LuaScript RequeueIfAdmittedScript = LuaScript.Prepare(
            "if redis.call('HEXISTS', @hashKey, @field) == 1 then\n" +
            "    redis.call('HSET', @hashKey, @field, @to)\n" +
            "    return 1\n" +
            "end\n" +
            "return 0");

        private readonly IDatabase database;

        private readonly ILighthouseClock clock;

        private readonly ILogger<RedisUpdateStatusStore> logger;

        public RedisUpdateStatusStore(IConnectionMultiplexer multiplexer, ILighthouseClock clock, ILogger<RedisUpdateStatusStore> logger)
        {
            database = multiplexer.GetDatabase();
            this.clock = clock;
            this.logger = logger;
        }

        public bool TryAdmit(UpdateKey key, UpdateStatus status)
        {
            var admittedAt = clock.Now;

            if (!database.HashSet(StatusHashKey, key.ToString(), (int)status.Status, When.NotExists))
            {
                return false;
            }

            status.QueuedAt = admittedAt;
            status.StartedAt = null;
            WriteMoments(key, new UpdateMoments(admittedAt, null));

            return true;
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

            // Only when the key actually landed on InProgress. The script is monotonic, so an advance that
            // was refused returns the ordinal the key already had, and stamping on that would restart the
            // clock of a run that has been going for some time.
            var moments = to == UpdateProgress.InProgress && resultingOrdinal == (int)UpdateProgress.InProgress
                ? StampStartedUnlessAlreadyRunning(key)
                : MomentsFor(key);

            return StatusFor(key, resultingOrdinal, moments);
        }

        public void Requeue(UpdateKey key)
        {
            // HEXISTS-guarded: only an admitted key may be re-queued, so a key another pod already
            // removed cannot be resurrected into a phantom active entry that never completes.
            var requeued = (long)database.ScriptEvaluate(
                RequeueIfAdmittedScript,
                new { hashKey = (RedisKey)StatusHashKey, field = key.ToString(), to = (int)UpdateProgress.Queued });

            if (requeued != 1)
            {
                // The guard refused, so there is no ordinal. Writing a moment now would leave the two hashes
                // carrying different key sets, which is the drift keeping them side by side has to avoid.
                return;
            }

            // A coalesced follow-up is new work waiting, not the old work still waiting, and the run that
            // just ended is over.
            WriteMoments(key, new UpdateMoments(clock.Now, null));
        }

        public bool TryGet(UpdateKey key, out UpdateStatus? status)
        {
            var value = database.HashGet(StatusHashKey, key.ToString());
            if (value.IsNull)
            {
                status = null;
                return false;
            }

            status = StatusFor(key, (long)value, MomentsFor(key));
            return true;
        }

        public void Remove(UpdateKey key)
        {
            database.HashDelete(StatusHashKey, key.ToString());
            database.HashDelete(MomentsHashKey, key.ToString());
        }

        /// <summary>
        /// Reads the whole hash, which is the same shape <see cref="HasActiveWork"/> already scans. Each field
        /// is the key's own <c>Type_Id</c> rendering and each value the ordinal, so the entity a row belongs to
        /// has to be reconstructed from the field name - the value alone says only how far it got. A field that
        /// does not parse is skipped rather than thrown on: one unreadable entry written by a future version
        /// must not cost an operator the whole list.
        /// </summary>
        public IReadOnlyList<UpdateStatus> GetAdmittedWork()
        {
            var admitted = new List<UpdateStatus>();

            // Both hashes in one pass each, rather than a moments lookup per row: the list is read whenever
            // an operator opens the popover, and the ordinal hash is the one that decides who is on it. If the
            // moments cannot be read the list is still answered, without durations - an operator asking what
            // is running gets a worse answer rather than none.
            var moments = BestEffortMoments();

            foreach (var entry in database.HashGetAll(StatusHashKey))
            {
                if (TryReadKey(entry.Name, out var key))
                {
                    moments.TryGetValue(entry.Name, out var recorded);
                    admitted.Add(StatusFor(key!, (long)entry.Value, recorded));
                }
            }

            return admitted;
        }

        private static bool TryReadKey(string? field, out UpdateKey? key)
        {
            key = null;

            if (string.IsNullOrEmpty(field))
            {
                return false;
            }

            // Split at the last separator: no UpdateType name contains one, but nothing stops an id from
            // being appended to something that does in a later version.
            var separator = field.LastIndexOf('_');
            if (separator <= 0 || separator == field.Length - 1)
            {
                return false;
            }

            if (!Enum.TryParse<UpdateType>(field[..separator], out var updateType)
                || !int.TryParse(field[(separator + 1)..], out var id))
            {
                return false;
            }

            key = new UpdateKey(updateType, id);
            return true;
        }

        public bool HasActiveWork()
        {
            return database.HashValues(StatusHashKey)
                .Select(value => (UpdateProgress)(int)(long)value)
                .Any(progress => progress is UpdateProgress.Queued or UpdateProgress.InProgress);
        }

        public bool HasQueuedWork(IReadOnlyCollection<UpdateKey> keys)
        {
            if (keys.Count == 0)
            {
                // Redis rejects a field-less HMGET outright, and a caller waiting on nothing is never held back anyway.
                return false;
            }

            var fields = keys.Select(key => (RedisValue)key.ToString()).ToArray();

            return database.HashGet(StatusHashKey, fields)
                .Any(value => !value.IsNull && (UpdateProgress)(int)(long)value == UpdateProgress.Queued);
        }

        private static UpdateStatus StatusFor(UpdateKey key, long ordinal, UpdateMoments moments)
        {
            return new UpdateStatus
            {
                UpdateType = key.UpdateType,
                Id = key.Id,
                Status = (UpdateProgress)(int)ordinal,
                QueuedAt = moments.QueuedAt,
                StartedAt = moments.StartedAt,
            };
        }

        private UpdateMoments StampStartedUnlessAlreadyRunning(UpdateKey key)
        {
            var recorded = MomentsFor(key);
            if (recorded.StartedAt is not null)
            {
                return recorded;
            }

            // The admission moment is carried through as it stands, including when it is missing: an entry
            // admitted by an older replica can still say honestly when it started, even though nothing can
            // say any more when it began waiting.
            var started = recorded with { StartedAt = clock.Now };

            // Report what was written down rather than what was meant to be. A moment the store failed to
            // keep would make this answer disagree with every read that comes after it.
            return WriteMoments(key, started) ? started : recorded;
        }

        private Dictionary<RedisValue, UpdateMoments> BestEffortMoments()
        {
            try
            {
                return database.HashGetAll(MomentsHashKey)
                    .ToDictionary(entry => entry.Name, entry => UpdateMoments.Parse(entry.Value));
            }
            catch (RedisException ex)
            {
                WarnListHasNoDurations(ex);
            }
            catch (TimeoutException ex)
            {
                WarnListHasNoDurations(ex);
            }

            return [];
        }

        private void WarnListHasNoDurations(Exception ex)
        {
            logger.LogWarning(ex, "Could not read when the admitted updates were admitted or started; the task list will show no durations.");
        }

        private UpdateMoments MomentsFor(UpdateKey key)
            => BestEffort(
                () => UpdateMoments.Parse(database.HashGet(MomentsHashKey, key.ToString())),
                UpdateMoments.NothingRecorded,
                key);

        private bool WriteMoments(UpdateKey key, UpdateMoments moments)
            => BestEffort(
                () =>
                {
                    database.HashSet(MomentsHashKey, key.ToString(), moments.ToStorageValue());
                    return true;
                },
                false,
                key);

        /// <summary>
        /// ADR-182 calls the moments best-effort, and that has to hold for the command as well as the value.
        /// Every one of them runs after the ordinal it accompanies has already been committed - the admission
        /// is already visible instance-wide, the advance has already moved the key - so letting a Redis
        /// timeout escape from here would abandon a key that is admitted but that nothing will now run or
        /// remove, and the team or portfolio it names would never refresh again.
        ///
        /// Losing a moment costs a row its duration, which is a state every reader already handles. Losing
        /// the refresh costs the instance the work. The trade is not close.
        /// </summary>
        private T BestEffort<T>(Func<T> moments, T whenUnavailable, UpdateKey key)
        {
            try
            {
                return moments();
            }
            catch (RedisException ex)
            {
                WarnMomentUnavailable(ex, key);
            }
            catch (TimeoutException ex)
            {
                WarnMomentUnavailable(ex, key);
            }

            return whenUnavailable;
        }

        private void WarnMomentUnavailable(Exception ex, UpdateKey key)
        {
            logger.LogWarning(
                ex,
                "Could not record or read when the update for {UpdateType} with ID {Id} was admitted or started. The refresh itself is unaffected; its row will show no duration.",
                key.UpdateType,
                key.Id);
        }
    }
}
