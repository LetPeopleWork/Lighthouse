using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class BaseMetricsServiceTests
    {
        private static readonly DateTime FixtureStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly IReadOnlyList<int> RequestedPercentiles = [50, 70, 85, 95];

        private static readonly string[] WorkflowOrder = ["In Progress", "Review", "Test"];

        [Test]
        public void ComputeAgeInStatePercentiles_CompletedItemsAcrossThreeStates_ReturnsExactCumulativeAgeAtExitPercentilesPerState()
        {
            var completedItems = Enumerable.Range(1, 20)
                .Select(BuildThreeStateCompletedItem)
                .ToList();

            var result = Subject().ComputeAgeInStatePercentiles(completedItems, WorkflowOrder, RequestedPercentiles).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(StatesInOrder(result), Is.EqualTo(WorkflowOrder));
                AssertPercentiles(result, "In Progress", (50, 10), (70, 14), (85, 17), (95, 19));
                AssertPercentiles(result, "Review", (50, 30), (70, 34), (85, 37), (95, 39));
                AssertPercentiles(result, "Test", (50, 50), (70, 54), (85, 57), (95, 59));
            });
        }

        [Test]
        public void ComputeAgeInStatePercentiles_AllReturnedStates_RisesMonotonicallyLeftToRight()
        {
            var completedItems = Enumerable.Range(1, 20)
                .Select(BuildThreeStateCompletedItem)
                .ToList();

            var result = Subject().ComputeAgeInStatePercentiles(completedItems, WorkflowOrder, RequestedPercentiles).ToList();

            foreach (var percentile in RequestedPercentiles)
            {
                var valuesLeftToRight = result
                    .Select(state => state.Percentiles.Single(p => p.Percentile == percentile).Value)
                    .ToList();

                Assert.That(valuesLeftToRight, Is.Ordered.Ascending, $"Percentile {percentile} must rise left to right");
            }
        }

        [Test]
        public void ComputeAgeInStatePercentiles_StateWithNoCompletedVisits_OmitsThatStateEntry()
        {
            var item = ItemStartedAtFixtureStart();
            item = WithTransitions(item,
                Transition("To Do", "In Progress", FixtureStart),
                Transition("In Progress", "Review", FixtureStart.AddDays(2)),
                Transition("Review", "Done", FixtureStart.AddDays(5)));

            var result = Subject().ComputeAgeInStatePercentiles([item], WorkflowOrder, RequestedPercentiles).ToList();

            Assert.That(StatesInOrder(result), Is.EqualTo(new[] { "In Progress", "Review" }));
        }

        [Test]
        public void ComputeAgeInStatePercentiles_SingleObservationForState_CollapsesAllPercentilesToThatValue()
        {
            var item = ItemStartedAtFixtureStart();
            item = WithTransitions(item,
                Transition("To Do", "In Progress", FixtureStart),
                Transition("In Progress", "Done", FixtureStart.AddDays(6)));

            var result = Subject().ComputeAgeInStatePercentiles([item], WorkflowOrder, RequestedPercentiles).ToList();

            AssertPercentiles(result, "In Progress", (50, 7), (70, 7), (85, 7), (95, 7));
        }

        [Test]
        public void ComputeAgeInStatePercentiles_ItemRevisitsState_ContributesOneObservationPerVisit()
        {
            var item = ItemStartedAtFixtureStart();
            item = WithTransitions(item,
                Transition("To Do", "In Progress", FixtureStart),
                Transition("In Progress", "Review", FixtureStart.AddDays(1)),
                Transition("Review", "In Progress", FixtureStart.AddDays(2)),
                Transition("In Progress", "Review", FixtureStart.AddDays(4)),
                Transition("Review", "In Progress", FixtureStart.AddDays(5)),
                Transition("In Progress", "Review", FixtureStart.AddDays(7)),
                Transition("Review", "Done", FixtureStart.AddDays(8)));

            var result = Subject().ComputeAgeInStatePercentiles([item], WorkflowOrder, RequestedPercentiles).ToList();

            AssertPercentiles(result, "Review", (50, 3), (70, 6), (85, 6), (95, 6));
        }

        private static void AssertPercentiles(
            IReadOnlyCollection<AgeInStatePercentilesDto> result,
            string state,
            params (int Percentile, int Value)[] expected)
        {
            var stateDto = result.Single(dto => dto.State == state);

            foreach (var (percentile, value) in expected)
            {
                Assert.That(
                    stateDto.Percentiles.Single(p => p.Percentile == percentile).Value,
                    Is.EqualTo(value),
                    $"{state} p{percentile}");
            }
        }

        private static IEnumerable<string> StatesInOrder(IEnumerable<AgeInStatePercentilesDto> result)
        {
            return result.Select(dto => dto.State);
        }

        private static WorkItem BuildThreeStateCompletedItem(int sequence)
        {
            var leftInProgress = FixtureStart.AddDays(sequence - 1);
            var leftReview = FixtureStart.AddDays(sequence + 19);
            var leftTest = FixtureStart.AddDays(sequence + 39);

            var item = ItemStartedAtFixtureStart();
            item.ClosedDate = leftTest;

            return WithTransitions(item,
                Transition("To Do", "In Progress", FixtureStart),
                Transition("In Progress", "Review", leftInProgress),
                Transition("Review", "Test", leftReview),
                Transition("Test", "Done", leftTest));
        }

        private static WorkItem ItemStartedAtFixtureStart()
        {
            return new WorkItem
            {
                StartedDate = FixtureStart,
                StateCategory = StateCategories.Done,
                ClosedDate = FixtureStart.AddDays(60),
            };
        }

        private static WorkItem WithTransitions(WorkItem item, params WorkItemStateTransition[] transitions)
        {
            return new WorkItem
            {
                StartedDate = item.StartedDate,
                StateCategory = item.StateCategory,
                ClosedDate = item.ClosedDate,
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

        private static TestableBaseMetricsService Subject()
        {
            return new TestableBaseMetricsService(new Mock<IServiceProvider>().Object);
        }

        private sealed class TestableBaseMetricsService(IServiceProvider serviceProvider)
            : BaseMetricsService(1, serviceProvider)
        {
            public new IEnumerable<AgeInStatePercentilesDto> ComputeAgeInStatePercentiles(
                IEnumerable<WorkItem> completedItemsInWindow,
                IEnumerable<string> doingStatesInWorkflowOrder,
                IReadOnlyList<int> requestedPercentiles)
            {
                return base.ComputeAgeInStatePercentiles(completedItemsInWindow, doingStatesInWorkflowOrder, requestedPercentiles);
            }
        }
    }
}
