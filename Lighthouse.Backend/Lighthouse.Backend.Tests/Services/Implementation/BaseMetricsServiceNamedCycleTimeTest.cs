using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class BaseMetricsServiceNamedCycleTimeTest
    {
        private static readonly DateTime FixtureStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] AllStatesInOrder = ["Planned", "In Progress", "Review", "Done"];

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
        public void NamedCycleTimeDays_EndStateEntryStopsTheClock_DwellInEndStateIsExcluded()
        {
            var item = ItemWithTransitions(FixtureStart,
                Transition("Planned", "In Progress", FixtureStart.AddDays(2)),
                Transition("In Progress", "Done", FixtureStart.AddDays(14)),
                Transition("Done", "Done", FixtureStart.AddDays(40)));

            var result = TestableBaseMetricsService.NamedDays(item, AllStatesInOrder, "Planned", "Done");

            Assert.That(result, Is.EqualTo(15));
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
        }
    }
}
