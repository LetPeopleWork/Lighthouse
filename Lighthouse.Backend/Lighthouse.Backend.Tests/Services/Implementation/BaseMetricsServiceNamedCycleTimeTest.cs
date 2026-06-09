using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class BaseMetricsServiceNamedCycleTimeTest
    {
        private static readonly DateTime FixtureStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] AllStatesInOrder = ["Planned", "In Progress", "Review", "Done"];
        private static readonly string[] InProgressToReviewSpan = ["In Progress", "Review"];

        [Test]
        public void NamedCycleTimeDays_ItemReachesPlannedThenDone46DaysLater_IsInclusive47Days()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(3)),
                Transition("In Progress", "Done", FixtureStart.AddDays(46)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Planned", "Done");

            Assert.That(result, Is.EqualTo(47));
        }

        [Test]
        public void NamedCycleTimeDays_ItemReopensAndReentersPlanned_UsesFirstCrossingNotTheReopen()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(5)),
                Transition("In Progress", "Planned", FixtureStart.AddDays(10)),
                Transition("Planned", "In Progress", FixtureStart.AddDays(20)),
                Transition("In Progress", "Done", FixtureStart.AddDays(29)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Planned", "Done");

            Assert.That(result, Is.EqualTo(30));
        }

        [Test]
        public void NamedCycleTimeDays_ItemNeverEntersEndStateOrLater_ReturnsNull()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(4)),
                Transition("In Progress", "Review", FixtureStart.AddDays(9)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Planned", "Done");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void NamedCycleTimeDays_ItemCrossesTheStartBoundaryButNeverTheEnd_ReturnsNull()
        {
            var startState = "Review";
            var endState = "Done";
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(3)),
                Transition("In Progress", "Review", FixtureStart.AddDays(8)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, startState, endState);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void NamedCycleTimeDays_BoundariesPresentButEndSortsBeforeStart_ReturnsNull()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(2)),
                Transition("In Progress", "Done", FixtureStart.AddDays(14)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Done", "Review");

            Assert.That(result, Is.Null,
                "A definition whose end boundary sorts before its start boundary has no window — it must not yield a spurious near-zero duration, matching ScopedCumulativeStateOrder's guard.");
        }

        [Test]
        public void NamedCycleTimeDays_StartAndEndResolveToTheSamePosition_ReturnsNull()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(2)),
                Transition("In Progress", "Done", FixtureStart.AddDays(14)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Done", "Done");

            Assert.That(result, Is.Null,
                "A zero-width window (end boundary at the same position as start) has no duration and must return null, not a one-day artifact.");
        }

        [Test]
        public void NamedCycleTimeDays_ItemNeverReachesTheStartBoundary_ReturnsNull()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(3)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Review", "Done");

            Assert.That(result, Is.Null,
                "An item whose journey never reaches the start boundary (or later) has no window start, so the named duration is null.");
        }

        [Test]
        public void ResolveBoundaryState_UnresolvableBoundary_FallsBackToTheBoundaryItself()
        {
            var owner = new Team
            {
                ToDoStates = ["Backlog"],
                DoingStates = ["Implementation"],
                DoneStates = ["Done"],
            };
            var allStates = owner.AllStates.ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TestableBaseMetricsService.ResolveBoundary(owner, allStates, "Implementation"), Is.EqualTo("Implementation"),
                    "A boundary that maps to a present state resolves to that ordered state.");
                Assert.That(TestableBaseMetricsService.ResolveBoundary(owner, allStates, "Ghost"), Is.EqualTo("Ghost"),
                    "A boundary that resolves to no present state falls back to the boundary literal, never null.");
            }
        }

        [Test]
        public void NamedCycleTimeDays_WindowSpanningAToDoFirstState_MeasuresFromCreationNotStarted()
        {
            var item = ItemWithDates(
                createdDate: FixtureStart,
                startedDate: FixtureStart.AddDays(3),
                Transition("Planned", "In Progress", FixtureStart.AddDays(3)),
                Transition("In Progress", "Done", FixtureStart.AddDays(13)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Planned", "Done");

            Assert.That(result, Is.EqualTo(14),
                "A window anchored at a To Do state measures from when the item entered that state at creation (day 0), " +
                "not from StartedDate (day 3) — so a wider window is never shorter than the default cycle time.");
        }

        [Test]
        public void NamedCycleTimeDays_ItemReopenedAndReclosed_EndsOnTheFinalReClose()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart),
                Transition("In Progress", "Done", FixtureStart.AddDays(10)),
                Transition("Done", "In Progress", FixtureStart.AddDays(15)),
                Transition("In Progress", "Done", FixtureStart.AddDays(20)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "In Progress", "Done");

            Assert.That(result, Is.EqualTo(21),
                "A reopened item ends on the LAST forward crossing into Done (day 20), not the first (day 10) — " +
                "aligning with the default Cycle Time's ClosedDate so a wider window never measures shorter.");
        }

        [Test]
        public void ScopedCumulativeStateOrder_SpansFromStartInclusiveToEndExclusive()
        {
            var span = TestableBaseMetricsService.ScopedOrder(AllStatesInOrder, "In Progress", "Done");

            Assert.That(span, Is.EqualTo(InProgressToReviewSpan),
                "The scoped span is the ordered AllStates slice [start .. end): it includes the start state and excludes the end state.");
        }

        [Test]
        public void ScopedCumulativeStateOrder_DegenerateOrAbsentBoundaries_YieldAnEmptySpan()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TestableBaseMetricsService.ScopedOrder(AllStatesInOrder, "Done", "Done"), Is.Empty,
                    "An equal start/end position is a zero-width span.");
                Assert.That(TestableBaseMetricsService.ScopedOrder(AllStatesInOrder, "Done", "In Progress"), Is.Empty,
                    "An end boundary sorting before the start is an empty span.");
                Assert.That(TestableBaseMetricsService.ScopedOrder(AllStatesInOrder, "Ghost", "Done"), Is.Empty,
                    "A start boundary absent from the workflow is an empty span.");
            }
        }

        [Test]
        public void NamedCycleTimeDays_EndStateEntryStopsTheClock_DwellInEndStateIsExcluded()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(2)),
                Transition("In Progress", "Done", FixtureStart.AddDays(14)),
                Transition("Done", "Done", FixtureStart.AddDays(40)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Planned", "Done");

            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void Cumulative_InFlightItemEnteredCurrentStateAfterWindowEnd_ContributesZeroOngoingNotNegative()
        {
            var windowEnd = FixtureStart.AddDays(10);
            var inFlight = new WorkItem
            {
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                StartedDate = FixtureStart,
                CurrentStateEnteredAt = windowEnd.AddDays(5),
                SyncedTransitions = [],
            };

            var rows = TestableBaseMetricsService.Cumulative([inFlight], ["In Progress"], windowEnd);
            var inProgress = rows.Single(row => row.State == "In Progress");

            Assert.That(inProgress.OngoingContributionDays, Is.Zero,
                "An item that entered its current state after the window end has no ongoing time in the window — the bar segment must never go negative.");
        }

        private static WorkItem ItemWithTransitions(DateTime startedDate, params WorkItemStateTransition[] transitions)
        {
            return new WorkItem
            {
                State = "Done",
                StateCategory = StateCategories.Done,
                StartedDate = startedDate,
                ClosedDate = transitions.Length > 0 ? transitions[^1].TransitionedAt : startedDate,
                SyncedTransitions = transitions,
            };
        }

        private static WorkItem ItemWithDates(DateTime createdDate, DateTime startedDate, params WorkItemStateTransition[] transitions)
        {
            return new WorkItem
            {
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = createdDate,
                StartedDate = startedDate,
                ClosedDate = transitions.Length > 0 ? transitions[^1].TransitionedAt : startedDate,
                SyncedTransitions = transitions,
            };
        }

        private static WorkItemStateTransition Transition(string fromState, string toState, DateTime transitionedAt)
        {
            return new WorkItemStateTransition
            {
                FromState = fromState,
                ToState = toState,
                TransitionedAt = transitionedAt,
            };
        }

        private sealed class TestableBaseMetricsService(IServiceProvider serviceProvider)
            : BaseMetricsService(1, serviceProvider)
        {
            public static int? NamedDays(WorkItem item, IReadOnlyList<string> allStatesInOrder, string startState, string endState)
            {
                return NamedCycleTimeDays(item, allStatesInOrder, startState, endState);
            }

            public static IReadOnlyList<string> ScopedOrder(IReadOnlyList<string> allStatesInOrder, string startState, string endState)
            {
                return ScopedCumulativeStateOrder(allStatesInOrder, startState, endState);
            }

            public static string ResolveBoundary(WorkTrackingSystemOptionsOwner owner, IReadOnlyList<string> allStatesInOrder, string boundaryState)
            {
                return ResolveBoundaryState(owner, allStatesInOrder, boundaryState);
            }

            public static IReadOnlyList<CumulativeStateTimeStateRowDto> Cumulative(IEnumerable<WorkItem> items, IReadOnlyList<string> stateOrder, DateTime nowSnapshot)
            {
                return ComputeCumulativeStateTime(items, stateOrder, nowSnapshot);
            }
        }
    }
}
