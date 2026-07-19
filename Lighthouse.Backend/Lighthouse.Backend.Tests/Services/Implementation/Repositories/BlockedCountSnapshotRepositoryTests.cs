using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    /// <summary>
    /// Mutation-driven characterization tests for BlockedCountSnapshotRepository.GetLatestAtOrBefore
    /// (Bug 5522 seed lookup). The builder and controller tests mock the repository, so the
    /// OrderByDescending / predicate mutations Stryker survived are pinned here against EF InMemory.
    /// </summary>
    [TestFixture]
    public class BlockedCountSnapshotRepositoryTests
    {
        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoServiceMock = new Mock<ICryptoService>();
            appContextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();
        }

        [Test]
        public async Task GetLatestAtOrBefore_ReturnsTheLatestSnapshotAtOrBeforeTheCutoff()
        {
            // Kills Stryker mutant 8437 (OrderByDescending() -> OrderBy()): earliest-first ordering
            // would return the 2026-06-30 row (count 1) instead of the 2026-07-09 row (count 3).
            // The 2026-07-10 row is after the cutoff and must never be returned (<= date pin).
            var cutoff = new DateOnly(2026, 7, 9);
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 6, 30), 1);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 5), 2);
            await GivenSnapshot(context, 1, OwnerType.Team, cutoff, 3);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 10), 4);

            var subject = CreateSubject(context);
            var latest = subject.GetLatestAtOrBefore(1, OwnerType.Team, cutoff);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(latest, Is.Not.Null);
                Assert.That(latest!.RecordedAt, Is.EqualTo(cutoff),
                    "a snapshot exactly on the cutoff is included (<= date) and is the latest at-or-before");
                Assert.That(latest.BlockedCount, Is.EqualTo(3));
            }
        }

        [Test]
        public async Task GetLatestAtOrBefore_NeverReturnsSnapshotsOfOtherOwnersOrTypes()
        {
            // Kills Stryker mutant 8439 (&& -> ||): a ||-mutated predicate would match the
            // wrong-owner rows on 2026-07-09 and return one of them instead of the 2026-07-01 row.
            var cutoff = new DateOnly(2026, 7, 9);
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 1), 3);
            await GivenSnapshot(context, 2, OwnerType.Team, cutoff, 70);
            await GivenSnapshot(context, 1, OwnerType.Portfolio, cutoff, 80);

            var subject = CreateSubject(context);
            var latest = subject.GetLatestAtOrBefore(1, OwnerType.Team, cutoff);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(latest, Is.Not.Null);
                Assert.That(latest!.OwnerId, Is.EqualTo(1));
                Assert.That(latest.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(latest.RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 1)));
                Assert.That(latest.BlockedCount, Is.EqualTo(3));
            }
        }

        [Test]
        public async Task GetLatestAtOrBefore_OnlyOtherOwnersOrTypesExist_ReturnsNull()
        {
            // Second pin for mutant 8439: with no matching owner row at all, a ||-mutated predicate
            // would still find the wrong-owner rows and fabricate a seed where none exists.
            var cutoff = new DateOnly(2026, 7, 9);
            using var context = CreateContext();
            await GivenSnapshot(context, 2, OwnerType.Team, new DateOnly(2026, 7, 1), 7);
            await GivenSnapshot(context, 1, OwnerType.Portfolio, new DateOnly(2026, 7, 8), 8);

            var subject = CreateSubject(context);
            var latest = subject.GetLatestAtOrBefore(1, OwnerType.Team, cutoff);

            Assert.That(latest, Is.Null,
                "snapshots of a different OwnerId or OwnerType must never seed the series");
        }

        private BlockedCountSnapshotRepository CreateSubject(LighthouseAppContext context)
        {
            return new BlockedCountSnapshotRepository(context, Mock.Of<ILogger<BlockedCountSnapshotRepository>>());
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private static async Task GivenSnapshot(
            LighthouseAppContext context, int ownerId, OwnerType ownerType, DateOnly recordedAt, int blockedCount)
        {
            context.BlockedCountSnapshots.Add(new BlockedCountSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                RecordedAt = recordedAt,
                BlockedCount = blockedCount,
            });
            await context.SaveChangesAsync();
        }
    }
}
