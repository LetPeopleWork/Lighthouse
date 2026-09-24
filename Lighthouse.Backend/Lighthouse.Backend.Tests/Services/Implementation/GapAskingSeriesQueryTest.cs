using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Opening an over-time chart answers with what is stored and, on the way, names the days of the
    /// asked-for period that the stored series already holds, so that only the others are filled in.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class GapAskingSeriesQueryTest
    {
        private const int OwnerId = 6;

        private static readonly DateOnly From = new(2026, 8, 1);

        private static readonly DateOnly To = new(2026, 8, 31);

        private static readonly DateOnly[] DaysHeld = [new(2026, 8, 3), new(2026, 8, 9)];

        [Test]
        public void APercentileSeries_IsServedAsStored_AndTheDaysItHoldsArePassedOnForThePeriodAsked()
        {
            var stored = DaysHeld.Select(day => new PercentilesOverTimeSnapshot { RecordedAt = day }).ToList();
            var snapshots = new Mock<IPercentilesOverTimeSnapshotRepository>();
            snapshots
                .Setup(repository => repository.GetSeries(OwnerId, OwnerType.Team, MetricType.CycleTime, 30, From, To))
                .Returns(stored);
            var reconciler = new Mock<IOverTimeGapReconciler>();
            var query = new GapAskingPercentilesOverTimeSeriesQuery(new PercentilesOverTimeSeriesQuery(snapshots.Object), reconciler.Object);

            var series = query.GetSeries(OwnerId, OwnerType.Team, MetricType.CycleTime, 30, From, To);

            Assert.That(series, Is.SameAs(stored));
            reconciler.Verify(r => r.AskForTheDaysThatAreMissing(
                OwnerId, OwnerType.Team, From, To, It.Is<IReadOnlyList<DateOnly>>(days => days.SequenceEqual(DaysHeld))), Times.Once);
        }

        [Test]
        public void ALimitSeries_IsServedAsStored_AndTheDaysItHoldsArePassedOnForThePeriodAsked()
        {
            var stored = DaysHeld.Select(day => new ProcessBehaviorSnapshot { RecordedAt = day }).ToList();
            var snapshots = new Mock<IProcessBehaviorSnapshotRepository>();
            snapshots
                .Setup(repository => repository.GetSeries(OwnerId, OwnerType.Portfolio, ProcessBehaviorMetricType.Wip, From, To))
                .Returns(stored);
            var reconciler = new Mock<IOverTimeGapReconciler>();
            var query = new GapAskingProcessBehaviorSeriesQuery(new ProcessBehaviorSeriesQuery(snapshots.Object), reconciler.Object);

            var series = query.GetSeries(OwnerId, OwnerType.Portfolio, ProcessBehaviorMetricType.Wip, From, To);

            Assert.That(series, Is.SameAs(stored));
            reconciler.Verify(r => r.AskForTheDaysThatAreMissing(
                OwnerId, OwnerType.Portfolio, From, To, It.Is<IReadOnlyList<DateOnly>>(days => days.SequenceEqual(DaysHeld))), Times.Once);
        }
    }
}
