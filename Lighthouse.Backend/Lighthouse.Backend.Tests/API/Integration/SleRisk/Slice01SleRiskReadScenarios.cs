using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.SleRisk
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #4127 — SLE risk for in-flight work), slice 01 / ADO Story
    /// #6016. Driving port: GET /api/latest/teams/{id}/metrics/sleRisk. US-01, AC-01.1 … AC-01.11.
    ///
    /// The number these scenarios pin is read as one sentence: of every item that was still open at
    /// this age, what fraction went on to take longer than the target. Slices 02, 03 and 04 all show
    /// the same number somewhere else, so if it is wrong here it is wrong in four places.
    ///
    /// The frontend half of US-01 — the column, its sort, its export and its absence without a target
    /// — lives in WorkItemsDialog.test.tsx. Those are claims about a grid, and driving a grid from
    /// here would prove nothing about what a reader sees.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4127-sle-risk")]
    [Category("slice-01")]
    public partial class Slice01SleRiskReadTest
    {
        // @walking_skeleton @driving_port @real-io @AC-01.1 @AC-01.2
        //
        // The whole feature in one scenario. Twenty finished items, a published target of 10 days, and
        // an item open for 5: six of the twenty took longer than 10 days, thirteen were still open on
        // day 5, and six in thirteen is 46%. Every other scenario here varies one part of that.
        [Test]
        public async Task A_team_with_a_target_is_told_each_open_item_s_chance_of_missing_it()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(3, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 13, 15, 18, 22, 30);
            var item = GivenAnItemOpenFor(5);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 46);
        }

        // @driving_port @real-io @AC-01.2 — the same distribution read at three more ages. Written as
        // one scenario per age rather than a loop so a failure names the age it failed at.
        [TestCase(2, 32)]
        [TestCase(9, 86)]
        [TestCase(10, 100)]
        public async Task The_longer_an_item_stays_open_the_worse_its_chances_get(int age, int expected)
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(3, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 13, 15, 18, 22, 30);
            var item = GivenAnItemOpenFor(age);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, expected);
        }

        // @driving_port @real-io @AC-01.3 — past the target, certainty is arithmetic rather than a
        // rule: every item that ran this long necessarily ran longer than the target, so the fraction
        // is one. A special case here would be a second place for the answer to be decided.
        [Test]
        public async Task An_item_already_past_the_target_is_certain_to_have_missed_it()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(6, 2, 4, 6, 8, 12, 16, 40);
            var item = GivenAnItemOpenFor(14);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 100);
        }

        // @driving_port @real-io @error @AC-01.4 — nothing ever ran this long, so there is nothing to
        // divide by. Saying "certain to miss" here would dress an absence of evidence as certainty,
        // and the reader cannot tell the two apart once it is a number.
        [Test]
        public async Task An_item_older_than_anything_ever_finished_is_given_no_answer()
        {
            var team = GivenATeamThatPromisesThirtyDays();
            GivenTheTeamHasFinished(2, 3, 4, 5, 6);
            var item = GivenAnItemOpenFor(20);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemIsBeyondWhatTheHistoryCanAnswer(item);
        }

        // @driving_port @real-io @error — nine items ran this long, and nine is not enough to divide
        // by: one more or one fewer moves the answer eleven points overnight, on the item a coach is
        // being told to look at first. The measurement behind the number is
        // docs/evolution/epic-4127-sle-risk/OUT-4127-risk-stability.md.
        [Test]
        public async Task An_item_too_little_finished_work_can_be_compared_against_is_given_no_answer()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(9, 20);
            GivenTheTeamHasFinishedSeveralOfEach(30, 1);
            var item = GivenAnItemOpenFor(20);

            await WhenTheRiskIsAskedFor(team);

            ThenTooLittleRanThatLongToSay(item, comparableItems: 9);
        }

        // @driving_port @real-io — one more item over the same line, and the answer arrives. Written
        // beside the scenario above because a threshold nobody crosses in a test is a threshold
        // nobody has checked the direction of.
        [Test]
        public async Task One_more_comparable_item_is_enough_to_be_told_the_answer()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(10, 20);
            GivenTheTeamHasFinishedSeveralOfEach(30, 1);
            var item = GivenAnItemOpenFor(20);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 100);
        }

        // @driving_port @real-io @error @AC-01.5 — a team that never published a target has not made
        // a promise, so there is no promise to be at risk of breaking. An empty answer, not zero.
        [Test]
        public async Task A_team_that_never_published_a_target_is_told_nothing_rather_than_zero()
        {
            var team = GivenATeamWithNoTarget();
            GivenTheTeamHasFinished(2, 4, 6, 8, 12, 40);
            GivenAnItemOpenFor(9);

            await WhenTheRiskIsAskedFor(team);

            ThenNothingIsSaidAboutAnyItem();
        }

        // @driving_port @real-io @error @AC-01.2 — a team with a target but nothing finished has the
        // same problem as the beyond-history case and gets the same answer.
        [Test]
        public async Task A_team_that_has_finished_nothing_yet_is_given_no_answer()
        {
            var team = GivenATeamThatPromisesTenDays();
            var item = GivenAnItemOpenFor(4);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemIsBeyondWhatTheHistoryCanAnswer(item);
        }

        // @driving_port @real-io @AC-01.11 — an item that took exactly the target MET it. Getting this
        // boundary wrong moves every number on the page by one item, and nothing on screen would say so.
        [Test]
        public async Task An_item_that_finished_on_the_target_day_did_not_miss_the_target()
        {
            var team = GivenATeamThatPromisesTenDays();
            // Three finished: one at exactly the target, two past it. If the one at exactly ten counts
            // as a miss the answer is 100%; it does not, so it is two in three.
            GivenTheTeamHasFinishedSeveralOfEach(4, 10, 12, 14);
            var item = GivenAnItemOpenFor(3);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 67);
        }

        // @driving_port @real-io @AC-01.11 — the other boundary. An item open for five days may still
        // close today at five, so a finished item that took five is one of the survivors it is being
        // compared against, not one that already got away.
        [Test]
        public async Task An_item_as_old_as_a_finished_one_still_counts_that_one_among_its_survivors()
        {
            var team = GivenATeamThatPromisesSixDays();
            // Survivors at age 5 are {5, 7}, of which {7} missed: one in two. Dropping the item that
            // took exactly five from the survivors would make it one in one.
            GivenTheTeamHasFinishedSeveralOfEach(6, 3, 5, 7);
            var item = GivenAnItemOpenFor(5);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 50);
        }

        // @driving_port @real-io @AC-01.2 — the window the caller asked about is the window that
        // counts, the same as every other number on the metrics page. Work that finished before it
        // is not evidence about today.
        [Test]
        public async Task Only_work_finished_inside_the_chosen_window_counts_as_evidence()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(3, 2, 4, 12, 14);
            GivenTheTeamAlsoFinishedLongAgo(30, 40, 50);
            var item = GivenAnItemOpenFor(2);

            await WhenTheRiskIsAskedFor(team);

            // Inside the window: four survivors, two of them past ten days. Counting the three ancient
            // ones would make it five in seven, or 71%.
            ThenTheItemsChanceOfMissingIs(item, 50);
        }

        // @driving_port @real-io @AC-01.2 — asking about a week that has already passed asks how things
        // stood THEN. An item's age is counted to the end of the window, not to today, the same way
        // every other number on the metrics page is read. Nothing else here would notice if it were
        // not: every other scenario asks about a window that ends today, where the two coincide.
        [Test]
        public async Task A_window_that_ended_in_the_past_is_answered_as_of_that_day()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(5, 2, 4, 12, 14);
            var item = GivenAnItemOpenFor(20);

            await WhenTheRiskIsAskedForAWindowEnding(team, tenDaysAgo: true);

            // As of ten days ago the item was 10 days old, so its survivors are {12, 14} and both
            // missed: 100%. Counted to today it would be 20 days old, beyond every finished item, and
            // the answer would be no answer at all.
            ThenTheItemsChanceOfMissingIs(item, 100);
        }

        // @driving_port @real-io @AC-01.2 — two windows, one team, one request after the other. The
        // answer is remembered between requests, so a memory that ignores which window was asked about
        // hands the second caller the first caller's answer — and nothing on screen would say so.
        [Test]
        public async Task Two_windows_asked_one_after_the_other_get_their_own_answers()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(5, 2, 4, 12, 14);
            var item = GivenAnItemOpenFor(20);

            await WhenTheRiskIsAskedForAWindowEnding(team, tenDaysAgo: true);
            ThenTheItemsChanceOfMissingIs(item, 100);

            await WhenTheRiskIsAskedFor(team);

            // As of today the item is 20 days old and nothing finished ever ran that long.
            ThenTheItemIsBeyondWhatTheHistoryCanAnswer(item);
        }

        // @driving_port @real-io @AC-01.2 — the target is half the arithmetic, and it is a setting a
        // coach changes in one click. An answer remembered against the old target is a wrong answer
        // that looks exactly like a right one.
        [Test]
        public async Task Tightening_the_target_changes_the_answer_rather_than_repeating_the_old_one()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(3, 4, 8, 12, 16);
            var item = GivenAnItemOpenFor(3);

            await WhenTheRiskIsAskedFor(team);
            // Four survivors at age three, two of them past ten days.
            ThenTheItemsChanceOfMissingIs(item, 50);

            GivenTheTeamNowPromises(6);

            await WhenTheRiskIsAskedFor(team);

            // The same four survivors, but now three of them are past the target.
            ThenTheItemsChanceOfMissingIs(item, 75);
        }

        // @driving_port @real-io @error — a window that ends before it starts is not a window, and
        // answering it with an empty list would read as "nothing is at risk".
        [Test]
        public async Task A_window_that_ends_before_it_starts_is_refused()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinished(2, 4, 12, 14);
            GivenAnItemOpenFor(3);

            await WhenTheRiskIsAskedForABackwardsWindow(team);

            ThenTheQuestionIsRejected();
        }

        // @driving_port @real-io @AC-01.2 — a single-day window is a question about one day, not a
        // malformed one. The guard that rejects a backwards range is one character away from
        // rejecting this too, and nothing else here asks about a window that starts where it ends.
        [Test]
        public async Task A_window_of_one_day_is_a_question_like_any_other()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinished(2, 4, 12, 14);
            GivenAnItemOpenFor(3);

            await WhenTheRiskIsAskedForASingleDay(team);

            ThenTheAnswerArrived();
        }

        // @driving_port @real-io @error — the risk names the team's own work, so it is readable by
        // exactly the people who may read the team. Every other metrics route on this controller is
        // scoped the same way and a new one is the easy place to forget.
        [Test]
        public async Task Someone_who_may_not_see_the_team_may_not_see_what_is_at_risk_on_it()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinished(2, 4, 12);
            GivenAnItemOpenFor(3);

            await WhenSomeoneWithoutAccessToTheTeamAsks(team);

            ThenTheyAreTurnedAway();
        }

        // @driving_port @real-io — the chart background that painted where the odds turn is gone, and
        // the per-item risk it was built beside is not. Asking for both in one run is the point: a
        // test that only checked the 404 would pass just as well if the whole feature had been torn
        // out, which is the failure this deletion is most exposed to.
        [Test]
        public async Task The_chart_background_can_no_longer_be_asked_for_and_the_item_risk_still_can()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinished(2, 4, 12);
            GivenAnItemOpenFor(3);

            await WhenBothTheBackgroundAndThePerItemRiskAreAskedFor(team);

            ThenTheBackgroundIsGoneAndThePerItemRiskIsNot();
        }

        // @driving_port @error @AC-01.6 — portfolios are deliberately out of this Epic: a feature can
        // sit in several, each with its own target and its own history, so one feature would have
        // several answers and no way to choose. The route not existing is how that decision is kept.
        [Test]
        public async Task Portfolios_are_not_asked_this_question_at_all()
        {
            var portfolio = GivenAPortfolio();

            await WhenTheRiskIsAskedForThePortfolio(portfolio);

            ThenThereIsNoSuchQuestionToAsk();
        }
    }
}
