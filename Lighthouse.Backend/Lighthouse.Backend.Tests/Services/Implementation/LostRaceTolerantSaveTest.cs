using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Another copy of the application wrote the same day first, and the database refused this copy's
    /// rows on the natural key. Whether that refusal is harmless depends only on whether every refused
    /// row is one that is now stored.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class LostRaceTolerantSaveTest
    {
        private LighthouseAppContext context = null!;

        private int saves;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new LighthouseAppContext(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
            saves = 0;
        }

        [TearDown]
        public void TearDown()
        {
            context.Dispose();
        }

        [Test]
        public async Task ASaveNobodyRefuses_IsMadeOnce()
        {
            await LostRaceTolerantSave.SaveAsync(() => SaveRefusingFirst(null), _ => false);

            Assert.That(saves, Is.EqualTo(1));
        }

        [Test]
        public async Task ARefusalOfRowsThatAreAllNowStored_IsAbsorbed_TheRowsLeaveTheStagingArea_AndTheRestIsSaved()
        {
            var first = StagedRow(30);
            var second = StagedRow(60);

            await LostRaceTolerantSave.SaveAsync(() => SaveRefusingFirst(first, second), _ => true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saves, Is.EqualTo(2), "The save was not made again once the refused rows were dropped.");
                Assert.That(first.State, Is.EqualTo(EntityState.Detached));
                Assert.That(second.State, Is.EqualTo(EntityState.Detached));
            }
        }

        [Test]
        public void ARefusalOfARowThatIsNotStored_IsThrownOn_AndTheRowStillLeavesTheStagingArea()
        {
            var refused = StagedRow(30);

            Assert.ThrowsAsync<DbUpdateException>(
                () => LostRaceTolerantSave.SaveAsync(() => SaveRefusingFirst(refused), _ => false));
            Assert.That(refused.State, Is.EqualTo(EntityState.Detached),
                "A refused row left staged is retried by the next day's save and fails that day too.");
        }

        [Test]
        public void ARefusalWhereOnlySomeRowsAreNowStored_IsThrownOn()
        {
            var stored = StagedRow(30);
            var notStored = StagedRow(60);

            Assert.ThrowsAsync<DbUpdateException>(() => LostRaceTolerantSave.SaveAsync(
                () => SaveRefusingFirst(stored, notStored),
                entity => ReferenceEquals(entity, stored.Entity)));
        }

        [Test]
        public void ARefusalOfARowBeingChangedRatherThanAdded_IsThrownOn_EvenWhenTheRowIsStored()
        {
            var row = new PercentilesOverTimeSnapshot { Id = 12, OwnerId = 1, Horizon = 30 };
            var changed = context.Attach(row);
            changed.State = EntityState.Modified;

            Assert.ThrowsAsync<DbUpdateException>(
                () => LostRaceTolerantSave.SaveAsync(() => SaveRefusingFirst(changed), _ => true));
        }

        [Test]
        public void ARefusalNamingNoRow_IsThrownOn()
        {
            Assert.ThrowsAsync<DbUpdateException>(
                () => LostRaceTolerantSave.SaveAsync(() => SaveRefusingFirst(), _ => true));
        }

        private EntityEntry<PercentilesOverTimeSnapshot> StagedRow(int horizon)
            => context.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot { OwnerId = 1, Horizon = horizon });

        /// <summary>
        /// The first save is refused naming the given rows; any later one goes through. Passing null
        /// makes the first save go through as well.
        /// </summary>
        private Task SaveRefusingFirst(params EntityEntry[]? refused)
        {
            saves++;
            if (saves == 1 && refused is not null)
            {
                throw new DbUpdateException("the natural key is already taken", refused);
            }

            return Task.CompletedTask;
        }
    }
}
