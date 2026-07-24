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
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class PercentilesOverTimeSnapshotRepositoryTests
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
        public async Task GetSeries_ReturnsMatchingSeriesOrderedByRecordedAt_WithAllFieldsRoundTripped()
        {
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 3), MetricType.CycleTime, 30, 4, 6, 9, 14);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 1), MetricType.CycleTime, 30, 3, 5, 8, 13);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 2), MetricType.CycleTime, 30, 2, 4, 7, 12);

            var subject = CreateSubject(context);
            var series = subject.GetSeries(1, OwnerType.Team, MetricType.CycleTime, 30);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(3));
                Assert.That(series[0].RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 1)));
                Assert.That(series[1].RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 2)));
                Assert.That(series[2].RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 3)));

                var first = series[0];
                Assert.That(first.OwnerId, Is.EqualTo(1));
                Assert.That(first.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(first.MetricType, Is.EqualTo(MetricType.CycleTime));
                Assert.That(first.Horizon, Is.EqualTo(30));
                Assert.That(first.P50, Is.EqualTo(3));
                Assert.That(first.P70, Is.EqualTo(5));
                Assert.That(first.P85, Is.EqualTo(8));
                Assert.That(first.P95, Is.EqualTo(13));
            }
        }

        [Test]
        public async Task GetSeries_NeverReturnsSnapshotsOfOtherOwnersTypesOrHorizons()
        {
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 1), MetricType.CycleTime, 30, 3, 5, 8, 13);
            await GivenSnapshot(context, 2, OwnerType.Team, new DateOnly(2026, 7, 1), MetricType.CycleTime, 30, 30, 40, 50, 60);
            await GivenSnapshot(context, 1, OwnerType.Portfolio, new DateOnly(2026, 7, 1), MetricType.CycleTime, 30, 70, 80, 90, 99);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 1), MetricType.CycleTime, 60, 11, 12, 13, 14);

            var subject = CreateSubject(context);
            var series = subject.GetSeries(1, OwnerType.Team, MetricType.CycleTime, 30);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(1));
                Assert.That(series[0].OwnerId, Is.EqualTo(1));
                Assert.That(series[0].OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(series[0].Horizon, Is.EqualTo(30));
                Assert.That(series[0].P50, Is.EqualTo(3));
            }
        }

        [Test]
        public async Task GetSeries_MatchesNullHorizon_WithoutMatchingHorizonedRows()
        {
            using var context = CreateContext();
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 1), MetricType.CycleTime, null, 3, 5, 8, 13);
            await GivenSnapshot(context, 1, OwnerType.Team, new DateOnly(2026, 7, 2), MetricType.CycleTime, 30, 4, 6, 9, 14);

            var subject = CreateSubject(context);
            var series = subject.GetSeries(1, OwnerType.Team, MetricType.CycleTime, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(1));
                Assert.That(series[0].Horizon, Is.Null);
                Assert.That(series[0].RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 1)));
            }
        }

        [Test]
        public async Task GetSeries_NoMatchingRows_ReturnsEmpty()
        {
            using var context = CreateContext();
            await GivenSnapshot(context, 2, OwnerType.Team, new DateOnly(2026, 7, 1), MetricType.CycleTime, 30, 3, 5, 8, 13);

            var subject = CreateSubject(context);
            var series = subject.GetSeries(1, OwnerType.Team, MetricType.CycleTime, 30);

            Assert.That(series, Is.Empty);
        }

        private PercentilesOverTimeSnapshotRepository CreateSubject(LighthouseAppContext context)
        {
            return new PercentilesOverTimeSnapshotRepository(context, Mock.Of<ILogger<PercentilesOverTimeSnapshotRepository>>());
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private static async Task GivenSnapshot(
            LighthouseAppContext context,
            int ownerId,
            OwnerType ownerType,
            DateOnly recordedAt,
            MetricType metricType,
            int? horizon,
            int p50,
            int p70,
            int p85,
            int p95)
        {
            context.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                RecordedAt = recordedAt,
                MetricType = metricType,
                Horizon = horizon,
                P50 = p50,
                P70 = p70,
                P85 = p85,
                P95 = p95,
            });
            await context.SaveChangesAsync();
        }
    }
}
