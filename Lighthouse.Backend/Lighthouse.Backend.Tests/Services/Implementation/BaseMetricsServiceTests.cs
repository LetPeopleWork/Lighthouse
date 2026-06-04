using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class BaseMetricsServiceTests
    {
        private static readonly DateTime FixtureStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly IReadOnlyList<int> RequestedPercentiles = [50, 70, 85, 95];

        private static readonly string[] WorkflowOrder = ["In Progress", "Review", "Test"];

        private static readonly string[] InProgressAndTest = ["In Progress", "Test"];

        [Test]
        public void ComputeAgeInStatePercentiles_CompletedItemsAcrossThreeStates_ReturnsExactCumulativeAgeAtExitPercentilesPerState()
        {
            var completedItems = Enumerable.Range(1, 20)
                .Select(BuildThreeStateCompletedItem)
                .ToList();

            var result = TestableBaseMetricsService.Compute(completedItems, WorkflowOrder, RequestedPercentiles).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatesInOrder(result), Is.EqualTo(WorkflowOrder));
                AssertPercentiles(result, "In Progress", (50, 10), (70, 14), (85, 17), (95, 19));
                AssertPercentiles(result, "Review", (50, 30), (70, 34), (85, 37), (95, 39));
                AssertPercentiles(result, "Test", (50, 50), (70, 54), (85, 57), (95, 59));
            }
        }

        [Test]
        public void ComputeAgeInStatePercentiles_AllReturnedStates_RisesMonotonicallyLeftToRight()
        {
            var completedItems = Enumerable.Range(1, 20)
                .Select(BuildThreeStateCompletedItem)
                .ToList();

            var result = TestableBaseMetricsService.Compute(completedItems, WorkflowOrder, RequestedPercentiles).ToList();

            foreach (var percentile in RequestedPercentiles)
            {
                var valuesLeftToRight = result
                    .Select(state => state.Percentiles.Single(p => p.Percentile == percentile).Value)
                    .ToList();

                Assert.That(valuesLeftToRight, Is.Ordered.Ascending, $"Percentile {percentile} must rise left to right");
            }
        }

        [Test]
        public void ComputeAgeInStatePercentiles_NonLastStateWithNoCompletedVisits_OmitsThatStateEntry()
        {
            var item = ItemStartedAtFixtureStart();
            item = WithTransitions(item,
                Transition("To Do", "In Progress", FixtureStart),
                Transition("In Progress", "Test", FixtureStart.AddDays(2)),
                Transition("Test", "Done", FixtureStart.AddDays(5)));

            var result = TestableBaseMetricsService.Compute([item], WorkflowOrder, RequestedPercentiles).ToList();

            Assert.That(StatesInOrder(result), Is.EqualTo(InProgressAndTest));
        }

        [Test]
        public void ComputeAgeInStatePercentiles_SingleObservationForState_CollapsesAllPercentilesToThatValue()
        {
            var item = ItemStartedAtFixtureStart();
            item = WithTransitions(item,
                Transition("To Do", "In Progress", FixtureStart),
                Transition("In Progress", "Done", FixtureStart.AddDays(6)));

            var result = TestableBaseMetricsService.Compute([item], WorkflowOrder, RequestedPercentiles).ToList();

            AssertPercentiles(result, "In Progress", (50, 7), (70, 7), (85, 7), (95, 7));
        }

        [Test]
        public void ComputeAgeInStatePercentiles_ItemRevisitsState_RecordsOnlyTheLastExitCumulativeAge()
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

            var result = TestableBaseMetricsService.Compute([item], WorkflowOrder, RequestedPercentiles).ToList();

            AssertPercentiles(result, "Review", (50, 9), (70, 9), (85, 9), (95, 9));
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

        private static readonly string[] CumulativeWorkflowOrder = ["In Progress", "Review", "Test", "Done"];

        private static readonly int[] CumulativeWorkflowOrderIndices = [0, 1, 2, 3];

        private static readonly string[] KnownReferenceIds = ["DONE-1", "DONE-2", "DONE-3"];

        [Test]
        public void ComputeCumulativeStateTime_CompletedVisitsAndInFlightItems_SplitsFullPerVisitDurationsIntoCompletedAndOngoingSegments()
        {
            var includedItems = new List<WorkItem>
            {
                CompletedItemWithReference("DONE-1", inProgressDays: 5, reviewDays: 10, testDays: 15),
                CompletedItemWithReference("DONE-2", inProgressDays: 5, reviewDays: 20, testDays: 15),
                CompletedItemWithReference("DONE-3", inProgressDays: 10, reviewDays: 30, testDays: 0),
                InFlightItem("In Progress", ongoingDays: 40),
                InFlightItem("Test", ongoingDays: 25),
            };

            var result = TestableBaseMetricsService.ComputeBars(includedItems, CumulativeWorkflowOrder, FixtureStart.AddDays(200));

            using (Assert.EnterMultipleScope())
            {
                var inProgress = Row(result, "In Progress");
                Assert.That(inProgress.CompletedContributionDays, Is.EqualTo(20.0).Within(Tolerance));
                Assert.That(inProgress.OngoingContributionDays, Is.EqualTo(40.0).Within(Tolerance));
                Assert.That(inProgress.TotalDays, Is.EqualTo(60.0).Within(Tolerance));

                var review = Row(result, "Review");
                Assert.That(review.CompletedContributionDays, Is.EqualTo(60.0).Within(Tolerance));
                Assert.That(review.OngoingContributionDays, Is.Zero);
                Assert.That(review.TotalDays, Is.EqualTo(60.0).Within(Tolerance));

                var test = Row(result, "Test");
                Assert.That(test.CompletedContributionDays, Is.EqualTo(30.0).Within(Tolerance));
                Assert.That(test.OngoingContributionDays, Is.EqualTo(25.0).Within(Tolerance));
                Assert.That(test.TotalDays, Is.EqualTo(55.0).Within(Tolerance));
            }
        }

        [Test]
        public void ComputeCumulativeStateTime_ItemEnteredStateBeforeFirstTransition_CountsFullDurationFromStartedDate()
        {
            var straddling = StraddlingReviewItem(reviewEnter: FixtureStart, reviewExit: FixtureStart.AddDays(50));

            var result = TestableBaseMetricsService.ComputeBars([straddling], CumulativeWorkflowOrder, FixtureStart.AddDays(200));

            var review = Row(result, "Review");
            Assert.That(review.TotalDays, Is.EqualTo(50.0).Within(Tolerance));
        }

        [Test]
        public void ComputeCumulativeStateTime_KnownVisits_ReportsContributingItemAndVisitCountsAndMeanMedianPerState()
        {
            var includedItems = new List<WorkItem>
            {
                CompletedItemWithReference("DONE-1", inProgressDays: 5, reviewDays: 10, testDays: 15),
                CompletedItemWithReference("DONE-2", inProgressDays: 5, reviewDays: 20, testDays: 15),
                CompletedItemWithReference("DONE-3", inProgressDays: 10, reviewDays: 30, testDays: 0),
                InFlightItem("In Progress", ongoingDays: 40),
            };

            var result = TestableBaseMetricsService.ComputeBars(includedItems, CumulativeWorkflowOrder, FixtureStart.AddDays(200));

            using (Assert.EnterMultipleScope())
            {
                var inProgress = Row(result, "In Progress");
                Assert.That(inProgress.CompletedItemCount, Is.EqualTo(3));
                Assert.That(inProgress.OngoingItemCount, Is.EqualTo(1));
                Assert.That(inProgress.ItemCount, Is.EqualTo(4));
                Assert.That(inProgress.MeanDays, Is.GreaterThan(0.0));
                Assert.That(inProgress.MedianDays, Is.Not.Null);
            }
        }

        [Test]
        public void ComputeCumulativeStateTime_StateWithNoContribution_EmitsPlaceholderBarWithZeroTotalInWorkflowOrder()
        {
            var noReviewItem = ItemWalkingInProgressThenTestSkippingReview();

            var result = TestableBaseMetricsService.ComputeBars([noReviewItem], CumulativeWorkflowOrder, FixtureStart.AddDays(200)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Select(r => r.State), Is.EqualTo(CumulativeWorkflowOrder));
                Assert.That(result.Select(r => r.WorkflowOrder), Is.EqualTo(CumulativeWorkflowOrderIndices));
                Assert.That(Row(result, "Review").TotalDays, Is.Zero);
            }
        }

        [Test]
        public void ComputeCumulativeStateTime_NoIncludedItems_ReturnsEmptyBars()
        {
            var result = TestableBaseMetricsService.ComputeBars([], CumulativeWorkflowOrder, FixtureStart.AddDays(200));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputeCumulativeStateTimeItems_KnownState_ReturnsOneRowPerContributingItemWhoseDaysSumToBarTotal()
        {
            var includedItems = new List<WorkItem>
            {
                CompletedItemWithReference("DONE-1", inProgressDays: 5, reviewDays: 10, testDays: 15),
                CompletedItemWithReference("DONE-2", inProgressDays: 5, reviewDays: 20, testDays: 15),
                CompletedItemWithReference("DONE-3", inProgressDays: 10, reviewDays: 30, testDays: 0),
            };

            var items = TestableBaseMetricsService.ComputeItems(includedItems, "Review", FixtureStart.AddDays(200)).ToList();
            var barTotal = Row(TestableBaseMetricsService.ComputeBars(includedItems, CumulativeWorkflowOrder, FixtureStart.AddDays(200)), "Review").TotalDays;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.Select(i => i.ReferenceId), Is.EquivalentTo(KnownReferenceIds));
                Assert.That(items.Sum(i => i.DaysContributed), Is.EqualTo(barTotal).Within(Tolerance));
            }
        }

        private const double Tolerance = 0.1;

        private static CumulativeStateTimeStateRowDto Row(IEnumerable<CumulativeStateTimeStateRowDto> result, string state)
        {
            return result.Single(row => row.State == state);
        }

        private static WorkItem CompletedItemWithReference(string referenceId, int inProgressDays, int reviewDays, int testDays)
        {
            var inProgressExit = FixtureStart.AddDays(inProgressDays);
            var reviewExit = inProgressExit.AddDays(reviewDays);
            var testExit = reviewExit.AddDays(testDays);

            var transitions = new List<WorkItemStateTransition>
            {
                Transition("In Progress", "Review", inProgressExit),
                Transition("Review", "Test", reviewExit),
            };
            transitions.Add(testDays > 0
                ? Transition("Test", "Done", testExit)
                : Transition("Review", "Done", reviewExit));

            return new WorkItem
            {
                Id = NextItemId(),
                ReferenceId = referenceId,
                State = "Done",
                StateCategory = StateCategories.Done,
                StartedDate = FixtureStart,
                ClosedDate = testExit,
                SyncedTransitions = transitions,
            };
        }

        private static int itemIdSequence;

        private static int NextItemId()
        {
            return System.Threading.Interlocked.Increment(ref itemIdSequence);
        }

        private static WorkItem StraddlingReviewItem(DateTime reviewEnter, DateTime reviewExit)
        {
            return new WorkItem
            {
                ReferenceId = "STRADDLE",
                State = "Done",
                StateCategory = StateCategories.Done,
                StartedDate = reviewEnter,
                ClosedDate = reviewExit,
                SyncedTransitions =
                [
                    Transition("In Progress", "Review", reviewEnter),
                    Transition("Review", "Done", reviewExit),
                ],
            };
        }

        private static WorkItem ItemWalkingInProgressThenTestSkippingReview()
        {
            return new WorkItem
            {
                ReferenceId = "NOREVIEW",
                State = "Done",
                StateCategory = StateCategories.Done,
                StartedDate = FixtureStart,
                ClosedDate = FixtureStart.AddDays(12),
                SyncedTransitions =
                [
                    Transition("In Progress", "Test", FixtureStart.AddDays(4)),
                    Transition("Test", "Done", FixtureStart.AddDays(12)),
                ],
            };
        }

        private static WorkItem InFlightItem(string state, int ongoingDays)
        {
            var enteredAt = FixtureStart.AddDays(200 - ongoingDays);
            return new WorkItem
            {
                Id = NextItemId(),
                ReferenceId = $"WIP-{state}",
                State = state,
                StateCategory = StateCategories.Doing,
                StartedDate = enteredAt,
                ClosedDate = null,
                CurrentStateEnteredAt = enteredAt,
                SyncedTransitions = [],
            };
        }

        private sealed class TestableBaseMetricsService(IServiceProvider serviceProvider)
            : BaseMetricsService(1, serviceProvider)
        {
            public static IEnumerable<AgeInStatePercentilesDto> Compute(
                IEnumerable<WorkItem> completedItemsInWindow,
                IReadOnlyList<string> doingStatesInWorkflowOrder,
                IReadOnlyList<int> requestedPercentiles)
            {
                var items = completedItemsInWindow.ToList();
                var cycleTimes = items.Select(item => item.CycleTime).Where(cycleTime => cycleTime > 0).ToList();
                var cycleTimePercentiles = requestedPercentiles
                    .Select(percentile => new PercentileValue(percentile, PercentileCalculator.CalculatePercentile(cycleTimes, percentile)))
                    .ToList();

                return ComputeAgeInStatePercentiles(items, doingStatesInWorkflowOrder, requestedPercentiles, cycleTimePercentiles);
            }

            public static IReadOnlyList<CumulativeStateTimeStateRowDto> ComputeBars(
                IEnumerable<WorkItem> includedItems,
                IReadOnlyList<string> workflowStateOrder,
                DateTime nowSnapshot)
            {
                return ComputeCumulativeStateTime(includedItems, workflowStateOrder, nowSnapshot);
            }

            public static IReadOnlyList<CumulativeStateTimeItemDto> ComputeItems(
                IEnumerable<WorkItem> includedItems,
                string state,
                DateTime nowSnapshot)
            {
                return ComputeCumulativeStateTimeItems(includedItems, state, nowSnapshot);
            }
        }
    }
}
