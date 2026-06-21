using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class PostgresUpdateExecutionLockTests
    {
        [Test]
        public async Task SameKey_SecondAcquisition_WaitsUntilFirstReleases()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            var lockProvider = CreateLock(postgres.GetConnectionString());
            var key = new UpdateKey(UpdateType.Team, 5);

            var firstHold = await lockProvider.AcquireAsync(key);

            var secondAcquisition = lockProvider.AcquireAsync(key);
            var acquiredWhileFirstHeld = await Task.WhenAny(secondAcquisition, Task.Delay(750)) == secondAcquisition;

            await firstHold.DisposeAsync();
            var secondHold = await secondAcquisition;
            await secondHold.DisposeAsync();

            Assert.That(acquiredWhileFirstHeld, Is.False,
                "a second acquisition for the same UpdateKey must block while the first holds the per-entity advisory " +
                "lock — the lock is the cluster-wide single-active-lifecycle-per-UpdateKey boundary (INV-4); it is " +
                "granted only once the first holder releases");
        }

        [Test]
        public async Task DifferentKeys_BothAcquire_WithoutBlocking()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            var lockProvider = CreateLock(postgres.GetConnectionString());

            var teamHold = await lockProvider.AcquireAsync(new UpdateKey(UpdateType.Team, 1));

            var featuresAcquisition = lockProvider.AcquireAsync(new UpdateKey(UpdateType.Features, 1));
            var bothAcquired = await Task.WhenAny(featuresAcquisition, Task.Delay(750)) == featuresAcquisition;

            await teamHold.DisposeAsync();
            if (bothAcquired)
            {
                await (await featuresAcquisition).DisposeAsync();
            }

            Assert.That(bothAcquired, Is.True,
                "distinct UpdateKeys derive distinct advisory-lock numbers, so unrelated entities never serialise — " +
                "the per-entity lock guards only same-entity work");
        }

        private static PostgresUpdateExecutionLock CreateLock(string connectionString)
        {
            return new PostgresUpdateExecutionLock(
                Options.Create(new DatabaseConfiguration { Provider = "Postgresql", ConnectionString = connectionString }));
        }
    }
}
