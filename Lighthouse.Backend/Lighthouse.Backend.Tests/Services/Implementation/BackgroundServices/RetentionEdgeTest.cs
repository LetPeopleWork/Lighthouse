using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// A team keeping thirty days of finished work, last refreshed on the 30th of June, still holds
    /// everything that finished on or after the 31st of May and nothing from before it.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class RetentionEdgeTest
    {
        private const int KeepsThirtyDays = 30;

        private static readonly DateOnly LastObservedOn = new(2026, 6, 30);

        private static readonly DateOnly OldestDayStillKept = new(2026, 5, 31);

        private static readonly PercentileValue[] AReading = [new PercentileValue(50, 4)];

        private static readonly ProcessBehaviourChart ReadyLimits = new()
        {
            Status = BaselineStatus.Ready,
            Average = 3,
            UpperNaturalProcessLimit = 7,
        };

        [Test]
        public void AnOwnerThatKeepsFinishedWorkForEver_HasNoEdge_SoTheWalkMayReachBackToItsFirstFinishedItem()
        {
            var longAgo = new DateOnly(2019, 1, 1);

            var edge = RetentionEdge.For(LastObservedOn, new Team { DoneItemsCutoffDays = 0 });

            Assert.That(edge.NoEarlierThanTheEdge(longAgo), Is.EqualTo(longAgo));
        }

        [Test]
        public void FinishedWorkOlderThanWhatIsKept_MovesTheFirstDayTheWalkMayWorkOutUpToTheEdge()
        {
            var edge = RetentionEdge.For(LastObservedOn, new Team { DoneItemsCutoffDays = KeepsThirtyDays });

            Assert.That(edge.NoEarlierThanTheEdge(new DateOnly(2026, 1, 1)), Is.EqualTo(OldestDayStillKept));
        }

        [Test]
        public void FinishedWorkThatBeganInsideWhatIsKept_LeavesTheFirstDayWhereTheWorkBegan()
        {
            var firstFinished = OldestDayStillKept.AddDays(5);

            var edge = RetentionEdge.For(LastObservedOn, new Team { DoneItemsCutoffDays = KeepsThirtyDays });

            Assert.That(edge.NoEarlierThanTheEdge(firstFinished), Is.EqualTo(firstFinished));
        }

        [Test]
        public void AnOwnerWithNothingFinished_StillHasNoFirstDay_WhateverItKeeps()
        {
            var edge = RetentionEdge.For(LastObservedOn, new Team { DoneItemsCutoffDays = KeepsThirtyDays });

            Assert.That(edge.NoEarlierThanTheEdge(null), Is.Null);
        }

        [Test]
        public void APercentileWindowReachingPastTheEdge_ComesBackEmpty_WithoutBeingRead()
        {
            var reads = 0;
            var guarded = EdgeKeepingThirtyDays().Guard(PercentilesCounting(() => reads++));

            var readings = guarded.ReadPercentiles(AtMidnight(OldestDayStillKept.AddDays(-1)), AtMidnight(LastObservedOn));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readings, Is.Empty);
                Assert.That(reads, Is.Zero);
            }
        }

        [Test]
        public void APercentileWindowStartingOnTheEdge_IsRead()
        {
            var guarded = EdgeKeepingThirtyDays().Guard(PercentilesCounting(() => { }));

            var readings = guarded.ReadPercentiles(AtMidnight(OldestDayStillKept), AtMidnight(LastObservedOn));

            Assert.That(readings, Is.EqualTo(AReading));
        }

        [Test]
        public void APercentileWindowOfAnOwnerThatKeepsEverything_IsReadHoweverFarBackItReaches()
        {
            var guarded = RetentionEdge.For(LastObservedOn, new Team { DoneItemsCutoffDays = 0 })
                .Guard(PercentilesCounting(() => { }));

            var readings = guarded.ReadPercentiles(AtMidnight(new DateOnly(2019, 1, 1)), AtMidnight(LastObservedOn));

            Assert.That(readings, Is.EqualTo(AReading));
        }

        [Test]
        public void LimitsWhoseOwnWindowReachesPastTheEdge_ComeBackNotReady()
        {
            var guarded = EdgeKeepingThirtyDays().Guard(LimitsReadAsOf(_ => { }));

            var chart = guarded.ReadChart(AtMidnight(OldestDayStillKept.AddDays(-1)), AtMidnight(LastObservedOn), LastObservedOn);

            Assert.That(chart.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
        }

        [Test]
        public void LimitsWhoseWindowLiesInsideWhatIsKept_AreReadAsOfTheDayAsked()
        {
            DateOnly? askedAsOf = null;
            var guarded = EdgeKeepingThirtyDays().Guard(LimitsReadAsOf(asOf => askedAsOf = asOf));

            var chart = guarded.ReadChart(AtMidnight(OldestDayStillKept), AtMidnight(LastObservedOn), LastObservedOn);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chart, Is.SameAs(ReadyLimits));
                Assert.That(askedAsOf, Is.EqualTo(LastObservedOn));
            }
        }

        /// <summary>
        /// A pinned stretch is where the limits come from, so a window inside what is kept says nothing
        /// about them once the stretch itself begins before the edge.
        /// </summary>
        [Test]
        public void LimitsDrawnFromAPinnedStretchThatBeginsBeforeTheEdge_ComeBackNotReady_EvenForAWindowInsideWhatIsKept()
        {
            var team = new Team
            {
                DoneItemsCutoffDays = KeepsThirtyDays,
                ProcessBehaviourChartBaselineStartDate = AtMidnight(OldestDayStillKept.AddDays(-10)),
            };
            var guarded = RetentionEdge.For(LastObservedOn, team).Guard(LimitsReadAsOf(_ => { }));

            var chart = guarded.ReadChart(AtMidnight(OldestDayStillKept.AddDays(5)), AtMidnight(LastObservedOn), LastObservedOn);

            Assert.That(chart.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
        }

        private static RetentionEdge EdgeKeepingThirtyDays()
            => RetentionEdge.For(LastObservedOn, new Team { DoneItemsCutoffDays = KeepsThirtyDays });

        private static PercentileFamilyReader PercentilesCounting(Action onRead)
            => new(MetricType.CycleTime, (_, _) =>
            {
                onRead();
                return AReading;
            });

        private static ProcessBehaviorFamilyReader LimitsReadAsOf(Action<DateOnly?> onRead)
            => new(ProcessBehaviorMetricType.Throughput, KeepsThirtyDays, (_, _, asOf) =>
            {
                onRead(asOf);
                return ReadyLimits;
            });

        private static DateTime AtMidnight(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }
}
