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

        // @driving_port @real-io — nothing the team finished ever ran this long, and the item is
        // still inside its target. A share of an empty set is undefined, so zero is a choice: the
        // item can still meet the target, and a hundred would say it cannot.
        [Test]
        public async Task An_item_no_finished_work_can_be_compared_against_is_given_zero()
        {
            var team = GivenATeamThatPromisesThirtyDays();
            GivenTheTeamHasFinished(2, 3, 4, 5, 6);
            var item = GivenAnItemOpenFor(20);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 0);
        }

        // @driving_port @real-io — nine comparable items is a small denominator and the answer moves
        // sharply when one arrives or leaves. It is still the team's own history, and a number a
        // reader can weigh beats a silence on the day an item is due. The volatility is real, measured
        // and accepted rather than hidden: docs/evolution/epic-4127-sle-risk/OUT-4127-risk-stability.md.
        [Test]
        public async Task An_item_only_a_little_finished_work_can_speak_for_is_still_given_a_number()
        {
            var team = GivenATeamThatPromisesThirtyDays();
            GivenTheTeamHasFinishedSeveralOfEach(9, 20);
            GivenTheTeamHasFinishedSeveralOfEach(50, 1);
            var item = GivenAnItemOpenFor(20);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 0);
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

        // @driving_port @real-io — a team with a target and no finished work at all. The item is
        // inside its target and there is nothing to divide by, which is the same shape as the
        // no-comparable-work case and gets the same answer.
        [Test]
        public async Task A_team_that_has_finished_nothing_yet_is_given_zero()
        {
            var team = GivenATeamThatPromisesTenDays();
            var item = GivenAnItemOpenFor(4);

            await WhenTheRiskIsAskedFor(team);

            ThenTheItemsChanceOfMissingIs(item, 0);
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

        // @driving_port @real-io — the scenario this whole slice exists for. A dialog reading 27%
        // against a board field reading 18, for the same item on the same day, is what a coach
        // actually saw. Both paths are driven here in one run so the two numbers can be compared
        // against each other rather than each against an expectation.
        [Test]
        public async Task The_screens_and_the_board_are_told_the_same_number_for_the_same_item()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(3, 4, 8, 12, 16);
            // Items either side of the target, so a single coincidence cannot carry the assertion.
            var young = GivenAnItemOpenFor(3);
            var older = GivenAnItemOpenFor(9);
            var past = GivenAnItemOpenFor(14);

            await WhenTheRiskIsAskedFor(team);

            ThenTheBoardWouldBeWrittenTheSameNumbersTheScreensShow(team, young, older, past);
        }

        // @driving_port @real-io — the date range is a control for looking at past metrics. The risk
        // is a claim about now, so a number that moved when a range widened would be a function of a
        // UI control rather than of the work. The route takes no dates at all, and stray ones sent by
        // an older bundle are ignored rather than honoured.
        [Test]
        public async Task A_range_the_caller_sends_anyway_changes_nothing()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(3, 4, 8, 12, 16);
            var item = GivenAnItemOpenFor(3);

            await WhenTheRiskIsAskedFor(team);
            ThenTheItemsChanceOfMissingIs(item, 50);

            await WhenTheRiskIsAskedForWithAStrayRange(team);

            ThenTheItemsChanceOfMissingIs(item, 50);
        }

        // @driving_port @real-io — the target is half the arithmetic, and it is a setting a
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

        // @driving_port @real-io — the evidence window is the other half of the key, and widening it
        // admits older work that changes the answer. A key carrying only the target would serve the
        // narrower window's answer here and look entirely correct doing it.
        [Test]
        public async Task Widening_the_history_admits_older_work_rather_than_repeating_the_old_answer()
        {
            var team = GivenATeamThatPromisesTenDays();
            GivenTheTeamHasFinishedSeveralOfEach(3, 4, 8, 12, 16);
            GivenTheTeamAlsoFinishedLongAgo(40, 40, 40);
            var item = GivenAnItemOpenFor(3);

            await WhenTheRiskIsAskedFor(team);
            ThenTheItemsChanceOfMissingIs(item, 50);

            GivenTheTeamNowLooksBackFurther();

            await WhenTheRiskIsAskedFor(team);

            // Twelve items inside the narrow window, six of them past ten days. Widening admits three
            // more, all of which missed: nine in fifteen.
            ThenTheItemsChanceOfMissingIs(item, 60);
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
