using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Mutation-driven characterization tests for <see cref="BlockedCountSeriesBuilder"/> (Bug 5522).
    /// The controller-level tests mock IBlockedCountSnapshotRepository, so the LINQ predicate and
    /// seed-lookup mutations Stryker survived there are exercised here against a REAL
    /// BlockedCountSnapshotRepository over EF InMemory — the predicates actually execute.
    /// </summary>
    [TestFixture]
    public class BlockedCountSeriesBuilderTests
    {
        private static readonly DateOnly Start = new(2026, 7, 10);
        private static readonly DateOnly End = new(2026, 7, 12);

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
        public async Task BuildDailySeries_MultiplePreStartSnapshots_SeedsFromTheLatestSnapshotBeforeStart()
        {
            // Kills Stryker mutant 8437 (OrderByDescending() -> OrderBy()): an earliest-first seed
            // would surface count 1 (2026-06-30) instead of 3 (2026-07-09) on the first emitted row.
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 6, 30), 1);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 7), 2);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 9), 3);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 12), 9);

            var series = BuildSeries(context, 1, OwnerType.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(3));
                Assert.That(series[0].RecordedAt, Is.EqualTo(Start));
                Assert.That(series[0].BlockedCount, Is.EqualTo(3),
                    "the seed must be the LATEST snapshot before start (2026-07-09), not the earliest");
                Assert.That(series[1].RecordedAt, Is.EqualTo(Start.AddDays(1)));
                Assert.That(series[1].BlockedCount, Is.EqualTo(3));
                Assert.That(series[2].RecordedAt, Is.EqualTo(End));
                Assert.That(series[2].BlockedCount, Is.EqualTo(9));
            }
        }

        [Test]
        public async Task BuildDailySeries_OnlySnapshotsForOtherOwnersOrTypes_EmitsNothing()
        {
            // Kills Stryker mutants 8439 (repository && -> ||) and 6176/6177 (builder && -> ||):
            // any ||-mutated predicate would match these rows, seeding or emitting a phantom series.
            using var context = CreateContext();
            await GivenSnapshot(context, 2, OwnerType.Team, new DateOnly(2026, 7, 9), 7);
            await GivenSnapshot(context, 1, OwnerType.Portfolio, new DateOnly(2026, 7, 9), 8);
            await GivenSnapshot(context, 2, OwnerType.Team, new DateOnly(2026, 7, 11), 70);
            await GivenSnapshot(context, 1, OwnerType.Portfolio, new DateOnly(2026, 7, 11), 80);

            var series = BuildSeries(context, 1, OwnerType.Team);

            Assert.That(series, Is.Empty,
                "snapshots of a different OwnerId or OwnerType must neither seed nor appear in the series");
        }

        [Test]
        public async Task BuildDailySeries_SnapshotsExactlyOnStartAndEnd_AreBothIncluded()
        {
            // Kills Stryker mutants 6182 (>= start -> > start) and 6184 (<= end -> < end):
            // excluding the start-day row would emit the seed (2) on day one; excluding the end-day
            // row would carry 4 through the last day instead of 6.
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 9), 2);
            await GivenSnapshot(context, 1, OwnerType.Team, Start, 4);
            await GivenSnapshot(context, 1, OwnerType.Team, End, 6);

            var series = BuildSeries(context, 1, OwnerType.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(3));
                Assert.That(series[0].RecordedAt, Is.EqualTo(Start));
                Assert.That(series[0].BlockedCount, Is.EqualTo(4),
                    "a snapshot exactly on start is in range (>= start) and overrides the seed");
                Assert.That(series[1].RecordedAt, Is.EqualTo(Start.AddDays(1)));
                Assert.That(series[1].BlockedCount, Is.EqualTo(4));
                Assert.That(series[2].RecordedAt, Is.EqualTo(End));
                Assert.That(series[2].BlockedCount, Is.EqualTo(6),
                    "a snapshot exactly on end is in range (<= end)");
            }
        }

        [Test]
        public async Task BuildDailySeries_InRangeSnapshotsOfOtherOwnersOrTypes_AreNeverEmitted()
        {
            // Value-based kill of Stryker mutants 6176/6177/6178 (builder && -> ||): with any of the
            // ||-mutated predicates the wrong-owner rows below would land in the day dictionary and
            // surface as 88 on 2026-07-10 or 99 on 2026-07-11 instead of the carried seed (3).
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 9), 3);
            await GivenSnapshot(context, 1, OwnerType.Portfolio, Start, 88);
            await GivenSnapshot(context, 2, OwnerType.Team, new DateOnly(2026, 7, 11), 99);

            var series = BuildSeries(context, 1, OwnerType.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(3));
                Assert.That(series[0].RecordedAt, Is.EqualTo(Start));
                Assert.That(series[0].BlockedCount, Is.EqualTo(3));
                Assert.That(series[1].RecordedAt, Is.EqualTo(Start.AddDays(1)));
                Assert.That(series[1].BlockedCount, Is.EqualTo(3));
                Assert.That(series[2].RecordedAt, Is.EqualTo(End));
                Assert.That(series[2].BlockedCount, Is.EqualTo(3));
                Assert.That(series, Has.All.Property(nameof(BlockedCountSnapshot.OwnerId)).EqualTo(1));
                Assert.That(series, Has.All.Property(nameof(BlockedCountSnapshot.OwnerType)).EqualTo(OwnerType.Team));
            }
        }

        private static List<BlockedCountSnapshot> BuildSeries(
            LighthouseAppContext context, int ownerId, OwnerType ownerType)
        {
            var repository = new BlockedCountSnapshotRepository(
                context, Mock.Of<ILogger<BlockedCountSnapshotRepository>>());
            return BlockedCountSeriesBuilder.BuildDailySeries(repository, ownerId, ownerType, Start, End);
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
