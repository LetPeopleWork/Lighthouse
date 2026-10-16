using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Staleness;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models.Staleness
{
    /// <summary>
    /// The one staleness rule, at its boundaries. These are the cases the browser's copy of the rule
    /// used to carry; they live here once that copy is deleted.
    /// </summary>
    [TestFixture]
    [Category("story-6247")]
    public class StalenessPolicyTest
    {
        private const string Review = "Review";

        private static readonly DateOnly AsOfDay = new(2026, 5, 25);

        private static readonly TimeZoneInfo Zurich = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        // [days in state, time-in-state threshold, expected stale]
        [TestCase(5, 5, false)]
        [TestCase(6, 5, true)]
        [TestCase(1, 1, false)]
        [TestCase(2, 1, true)]
        public void A_Work_Item_in_progress_goes_stale_only_once_it_has_sat_longer_than_the_threshold(int daysInState, int threshold, bool expectedStale)
        {
            var verdict = PolicyWith(threshold, blockedThreshold: 3)
                .Evaluate(InProgress(daysInState), Zurich, AsOfDay);

            var expectedReasons = expectedStale
                ? new List<StaleReason> { new(StaleReasonKind.TimeInState, daysInState, Review) }
                : [];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.EqualTo(expectedStale));
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        // [days blocked, blocked threshold, expected stale]
        [TestCase(2, 3, false)]
        [TestCase(3, 3, true)]
        [TestCase(4, 3, true)]
        public void A_Blocked_Work_Item_goes_stale_once_it_has_been_Blocked_for_the_blocked_threshold(int daysBlocked, int blockedThreshold, bool expectedStale)
        {
            var verdict = PolicyWith(timeInStateThreshold: 0, blockedThreshold)
                .Evaluate(Blocked(daysInState: daysBlocked, daysBlocked), Zurich, AsOfDay);

            var expectedReasons = expectedStale
                ? new List<StaleReason> { new(StaleReasonKind.BlockedDuration, daysBlocked, null) }
                : [];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.EqualTo(expectedStale));
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        [Test]
        public void A_Blocked_Work_Item_long_in_its_state_is_judged_by_its_blocked_time_and_shows_both()
        {
            var verdict = PolicyWith(timeInStateThreshold: 5, blockedThreshold: 3)
                .Evaluate(Blocked(daysInState: 12, daysBlocked: 4), Zurich, AsOfDay);

            var expectedReasons = new List<StaleReason>
            {
                new(StaleReasonKind.BlockedDuration, 4, null),
                new(StaleReasonKind.TimeInStateWhileBlocked, 12, Review),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.True);
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        [Test]
        public void A_Blocked_Work_Item_under_the_blocked_threshold_is_not_stale_however_long_it_has_sat_in_its_state()
        {
            var verdict = PolicyWith(timeInStateThreshold: 5, blockedThreshold: 3)
                .Evaluate(Blocked(daysInState: 9, daysBlocked: 2), Zurich, AsOfDay);

            var expectedReasons = new List<StaleReason> { new(StaleReasonKind.TimeInStateWhileBlocked, 9, Review) };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        // [days in state, expected to show the time in state] - the same strict "longer than" as the
        // trigger, so a Blocked item names its time in state only once it is past the threshold.
        [TestCase(5, false)]
        [TestCase(6, true)]
        public void A_Blocked_Work_Item_shows_its_time_in_state_only_once_it_is_past_the_threshold(int daysInState, bool expectedToShow)
        {
            var verdict = PolicyWith(timeInStateThreshold: 5, blockedThreshold: 3)
                .Evaluate(Blocked(daysInState, daysBlocked: 2), Zurich, AsOfDay);

            var expectedReasons = expectedToShow
                ? new List<StaleReason> { new(StaleReasonKind.TimeInStateWhileBlocked, daysInState, Review) }
                : [];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        [Test]
        public void A_Blocked_Work_Item_whose_state_entry_is_unknown_is_still_judged_by_its_blocked_time()
        {
            var candidate = Blocked(daysInState: 30, daysBlocked: 4) with { EnteredStateAt = null };

            var verdict = PolicyWith(timeInStateThreshold: 5, blockedThreshold: 3).Evaluate(candidate, Zurich, AsOfDay);

            var expectedReasons = new List<StaleReason> { new(StaleReasonKind.BlockedDuration, 4, null) };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.True);
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        // [time-in-state threshold, blocked threshold, blocked]
        [TestCase(0, 0, false)]
        [TestCase(0, 0, true)]
        [TestCase(0, 3, false)]
        public void A_threshold_of_zero_turns_its_trigger_off(int timeInStateThreshold, int blockedThreshold, bool isBlocked)
        {
            var candidate = isBlocked ? Blocked(daysInState: 30, daysBlocked: 30) : InProgress(30);

            var verdict = PolicyWith(timeInStateThreshold, blockedThreshold).Evaluate(candidate, Zurich, AsOfDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.Empty);
            }
        }

        [Test]
        public void A_blocked_threshold_of_zero_leaves_a_long_Blocked_Work_Item_not_stale_but_still_shows_its_time_in_state()
        {
            var verdict = PolicyWith(timeInStateThreshold: 5, blockedThreshold: 0)
                .Evaluate(Blocked(daysInState: 30, daysBlocked: 30), Zurich, AsOfDay);

            var expectedReasons = new List<StaleReason> { new(StaleReasonKind.TimeInStateWhileBlocked, 30, Review) };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        [TestCase(StateCategories.ToDo)]
        [TestCase(StateCategories.Done)]
        [TestCase(StateCategories.Unknown)]
        public void Only_Work_Items_in_progress_can_be_stale(StateCategories category)
        {
            var candidate = InProgress(30) with { StateCategory = category };

            var verdict = PolicyWith(timeInStateThreshold: 5, blockedThreshold: 3).Evaluate(candidate, Zurich, AsOfDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.Empty);
            }
        }

        [Test]
        public void A_Work_Item_whose_state_entry_is_unknown_has_no_time_in_state_to_judge()
        {
            var candidate = InProgress(30) with { EnteredStateAt = null };

            var verdict = PolicyWith(timeInStateThreshold: 5, blockedThreshold: 3).Evaluate(candidate, Zurich, AsOfDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.Empty);
            }
        }

        [Test]
        public void A_Blocked_Work_Item_with_no_known_blocked_start_has_no_blocked_time_to_judge()
        {
            var candidate = Blocked(daysInState: 30, daysBlocked: 30) with { BlockedSince = null };

            var verdict = PolicyWith(timeInStateThreshold: 0, blockedThreshold: 3).Evaluate(candidate, Zurich, AsOfDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.Empty);
            }
        }

        [Test]
        public void Days_are_counted_on_the_instance_calendar_not_in_UTC()
        {
            // 22:30 UTC on the 19th is already the 20th in Zurich: six days to the 25th there, seven in UTC.
            var candidate = InProgress(1) with { EnteredStateAt = new DateTime(2026, 5, 19, 22, 30, 0, DateTimeKind.Utc) };

            var verdict = PolicyWith(timeInStateThreshold: 6, blockedThreshold: 0).Evaluate(candidate, Zurich, AsOfDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.False);
                Assert.That(verdict.Reasons, Is.Empty);
            }
        }

        // [days after the day asked about that the block began] - a start on that day, or one recorded
        // after it, still counts as one day.
        [TestCase(0)]
        [TestCase(1)]
        public void A_Work_Item_Blocked_on_the_day_asked_about_has_been_Blocked_for_one_day(int daysLater)
        {
            var candidate = Blocked(daysInState: 1, daysBlocked: 1) with { BlockedSince = AtNoonOn(AsOfDay.AddDays(daysLater)) };

            var verdict = PolicyWith(timeInStateThreshold: 0, blockedThreshold: 1).Evaluate(candidate, Zurich, AsOfDay);

            var expectedReasons = new List<StaleReason> { new(StaleReasonKind.BlockedDuration, 1, null) };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsStale, Is.True);
                Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
            }
        }

        [Test]
        public void The_policy_reads_both_thresholds_from_their_owner()
        {
            var team = new Team { StalenessThresholdDays = 5, BlockedStalenessThresholdDays = 3 };

            var verdict = StalenessPolicy.For(team).Evaluate(Blocked(daysInState: 4, daysBlocked: 4), Zurich, AsOfDay);

            var expectedReasons = new List<StaleReason> { new(StaleReasonKind.BlockedDuration, 4, null) };

            Assert.That(verdict.Reasons, Is.EqualTo(expectedReasons));
        }

        private static StalenessPolicy PolicyWith(int timeInStateThreshold, int blockedThreshold)
            => StalenessPolicy.For(new Team
            {
                StalenessThresholdDays = timeInStateThreshold,
                BlockedStalenessThresholdDays = blockedThreshold,
            });

        private static StalenessCandidate InProgress(int daysInState)
            => new(StateCategories.Doing, Review, EnteredDaysAgo(daysInState), IsBlocked: false, BlockedSince: null);

        private static StalenessCandidate Blocked(int daysInState, int daysBlocked)
            => new(StateCategories.Doing, Review, EnteredDaysAgo(daysInState), IsBlocked: true, BlockedSince: EnteredDaysAgo(daysBlocked));

        // "N days" counts the entered day and the day asked about, the way Work Item Age does.
        private static DateTime EnteredDaysAgo(int days) => AtNoonOn(AsOfDay.AddDays(-(days - 1)));

        private static DateTime AtNoonOn(DateOnly day)
            => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(new TimeOnly(12, 0)), Zurich);
    }
}
