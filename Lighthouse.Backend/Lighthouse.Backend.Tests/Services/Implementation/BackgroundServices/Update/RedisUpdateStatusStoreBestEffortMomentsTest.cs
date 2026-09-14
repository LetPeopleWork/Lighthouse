using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Epic #5511 slice 03. ADR-182 calls the moments best-effort, and this is what that has to mean for the
    /// commands as well as for the values.
    ///
    /// Every moments command runs *after* the ordinal it accompanies has already been committed: the key is
    /// already admitted instance-wide, or the advance has already moved it. So a Redis failure escaping from
    /// a moments command does not lose a duration - it abandons a key that nothing will now run and nothing
    /// will now remove, and the team or portfolio it names stops refreshing until somebody clears the hash by
    /// hand. That is the failure these tests exist to keep closed.
    ///
    /// A mocked <c>IDatabase</c> rather than a container, because the point is what happens when Redis does
    /// not answer, and a working Redis cannot be asked to demonstrate that.
    /// </summary>
    [TestFixture]
    public class RedisUpdateStatusStoreBestEffortMomentsTest
    {
        private const string MomentsHashKey = "lighthouse:update-moments";

        private static readonly UpdateKey Key = new(UpdateType.Team, 7);

        [Test]
        public void TryAdmit_WhenTheMomentCannotBeRecorded_StillAdmitsTheWork()
        {
            var store = StoreWhoseMomentsFail(new TimeoutException("Redis did not answer in time"));

            var admitted = store.TryAdmit(Key, new UpdateStatus { UpdateType = Key.UpdateType, Id = Key.Id });

            Assert.That(admitted, Is.True,
                "The ordinal is already written at this point, so anything but 'admitted' leaves a key in the "
                + "store that the caller believes it never queued - and nothing will ever run or remove it.");
        }

        [Test]
        public void Requeue_WhenTheMomentCannotBeRecorded_DoesNotThrowAtTheCaller()
        {
            var store = StoreWhoseMomentsFail(new RedisException("Redis went away"));

            Assert.DoesNotThrow(() => store.Requeue(Key),
                "The script has already reset the ordinal to Queued. An exception here unwinds the coalesced "
                + "follow-up that was about to be scheduled, and the key waits for a run that never comes.");
        }

        [Test]
        public void Advance_WhenTheMomentCannotBeRecorded_StillReportsTheProgressItMade()
        {
            var store = StoreWhoseMomentsFail(new TimeoutException("Redis did not answer in time"));

            var status = store.Advance(Key, UpdateProgress.InProgress);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status, Is.Not.Null,
                    "The advance itself succeeded; returning null would tell the queue the key had gone.");
                Assert.That(status!.Status, Is.EqualTo(UpdateProgress.InProgress));
                Assert.That(status.StartedAt, Is.Null,
                    "The moment is the only thing lost, and a row with no duration is a state every reader "
                    + "already has to handle.");
            }
        }

        [Test]
        public void GetAdmittedWork_WhenTheMomentsCannotBeRead_StillAnswersWithTheWork()
        {
            var store = StoreWhoseMomentsFail(new TimeoutException("Redis did not answer in time"));

            var admitted = store.GetAdmittedWork();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(admitted, Has.Count.EqualTo(1),
                    "An operator asking what is running should get a worse answer, not an error page.");
                Assert.That(admitted[0].QueuedAt, Is.Null);
            }
        }

        /// <summary>
        /// A database where the ordinal hash behaves and every moments command fails. Keyed on the hash name
        /// so the two are told apart the way the store itself tells them apart.
        /// </summary>
        private static RedisUpdateStatusStore StoreWhoseMomentsFail(Exception failure)
        {
            var database = new Mock<IDatabase>();

            database
                .Setup(db => db.HashSet(It.Is<RedisKey>(k => k != MomentsHashKey), It.IsAny<RedisValue>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Returns(true);
            database
                .Setup(db => db.ScriptEvaluate(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()))
                .Returns(RedisResult.Create((RedisValue)(int)UpdateProgress.InProgress));
            database
                .Setup(db => db.HashGetAll(It.Is<RedisKey>(k => k != MomentsHashKey), It.IsAny<CommandFlags>()))
                .Returns([new HashEntry(Key.ToString(), (int)UpdateProgress.Queued)]);

            database
                .Setup(db => db.HashSet(It.Is<RedisKey>(k => k == MomentsHashKey), It.IsAny<RedisValue>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Throws(failure);
            database
                .Setup(db => db.HashGet(It.Is<RedisKey>(k => k == MomentsHashKey), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .Throws(failure);
            database
                .Setup(db => db.HashGetAll(It.Is<RedisKey>(k => k == MomentsHashKey), It.IsAny<CommandFlags>()))
                .Throws(failure);

            var multiplexer = new Mock<IConnectionMultiplexer>();
            multiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);

            return new RedisUpdateStatusStore(multiplexer.Object, Clocks.SystemUtc, NullLogger<RedisUpdateStatusStore>.Instance);
        }
    }
}
