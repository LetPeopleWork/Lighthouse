using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// DISTILL acceptance scenarios (ADO #5877), slice 01: teams keep moving while a portfolio refresh
    /// runs. Driving ports: a refresh triggered on an updater and run by the production update queue,
    /// and the System-Administrator-guarded task list on <c>UpdateController</c> read over HTTP.
    /// US-01, AC-01.1 … AC-01.8.
    ///
    /// Two things these scenarios are deliberately built to resist:
    ///
    /// The portfolio refresh is <em>held open</em> rather than made slow. Starvation is the failure
    /// mode, and a scenario that waits for a slow refresh to finish passes against the bug — the team
    /// refresh does start, eventually, which is precisely the complaint that was reported.
    ///
    /// The cross-lane <c>waitingBehind</c> claim is asserted over repeated reads. The value is resolved
    /// by picking a running row out of a concurrent store, so one correct read proves nothing about the
    /// next one.
    ///
    /// Answered elsewhere, neither silently:
    ///
    /// AC-01.5's union-of-stagings half is a claim about <c>WriteBackRound</c>'s own contents rather
    /// than about anything a port answers, and it lives in <c>WriteBackRoundConcurrencyTest</c>. What
    /// this file carries is its observable half — a round says its piece once however many executions
    /// were in it.
    ///
    /// AC-01.8 is a production-data criterion on Tenant Zero: two <c>RefreshLog</c> rows whose run
    /// intervals overlap, one Team and one Portfolio, against real work-tracking connections. Doubles
    /// prove the lanes exist; only real connections prove they survive a real connector, a real
    /// database and a real write-back round. It is a dogfood check, not a test, and it is recorded as
    /// one in the feature delta rather than left to look like coverage.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-5877-update-queue-lanes")]
    [Category("slice-01")]
    public partial class Slice01TeamsKeepMovingTest
    {
        // @driving_port @real-io @AC-01.1 — the reported complaint, stated as the thing that must stop
        // being true. Three teams sat behind one portfolio refresh for 3h38m and the operator read the
        // instance as dead.
        [Test]
        public async Task A_team_refresh_runs_while_a_portfolio_refresh_is_held_open()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsEveryRefreshOpenUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);
            WhenARefreshOfThatTeamIsAskedFor(team);

            await ThenThatTeamRefreshGetsGoing(team);
            await ThenThatPortfolioRefreshIsStillGoing(portfolio);
        }

        // @driving_port @real-io @AC-01.2 — the deliberate retention. Two portfolio refreshes at once
        // would double what one tracker is asked for, which is the opposite of what the on-premise
        // instance that reported this needs.
        [Test]
        public async Task A_second_portfolio_refresh_waits_while_the_first_one_is_held_open()
        {
            var running = GivenAPortfolioThatIsRefreshedOnSchedule();
            var waiting = GivenAPortfolioThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsEveryRefreshOpenUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(running);
            await WhenARefreshOfThatPortfolioIsAlsoAskedFor(waiting);

            await ThenThatPortfolioRefreshNeverGetsGoingWhileTheOtherHoldsItsLane(waiting);
        }

        // @driving_port @real-io @AC-01.3 — the case the existing lane-holder specifications cannot
        // see. They queue one team behind another, which is one lane under either reading, so they pass
        // unchanged and say nothing about this. Asserted over repeated reads because the answer is
        // picked out of a concurrent store.
        [Test]
        public async Task A_queued_team_names_the_team_holding_its_own_lane_and_never_the_running_portfolio()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            var holdingTheTeamLane = GivenATeamThatIsRefreshedOnSchedule();
            var waiting = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsEveryRefreshOpenUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);
            await WhenARefreshOfThatTeamIsUnderWay(holdingTheTeamLane);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(waiting);

            await ThenEveryReadOfThatQueuedRowNames(waiting, holdingTheTeamLane, portfolio);
        }

        // @driving_port @real-io @error @AC-01.4 — the other half of the same defect, and the one that
        // tells an operator something false rather than something arbitrary: a row whose own lane is
        // free is not waiting for anything, and naming the portfolio would invent a dependency.
        [Test]
        public async Task A_queued_team_whose_own_lane_is_free_is_waiting_behind_nothing()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsEveryRefreshOpenUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);
            GivenARefreshOfThatTeamIsAdmittedWithoutTheQueueEverReachingIt(team);

            await ThenThatQueuedRowIsWaitingBehindNothing(team);
        }

        // @driving_port @real-io @AC-01.5 — the precursor commit's observable half. A portfolio refresh
        // and the forecast it triggers are two executions of one round; with lanes they can be in that
        // round at the same time, and the check that decides who speaks for the round is a read
        // followed by an act. Two lines means the operator reads one refresh as two; none means the
        // round dropped everything it had resolved to write, silently.
        [Test]
        public async Task A_portfolio_and_the_forecast_it_triggers_still_report_their_round_once_between_them()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();

            await WhenThatPortfolioRefreshRunsToTheEnd(portfolio);

            ThenTheRoundSaidItsPieceExactlyOnce(portfolio);
        }

        // @driving_port @real-io — the ordering guarantee the lanes removed without declaring it. Forecast
        // coalescing was built when a Team refresh and a Portfolio refresh could not run at once, so
        // whichever asked second always found the forecast still sitting in the queue and stood down. With
        // a lane each they overlap, and the operator watches one delivery date settle and then move. The
        // promise is pinned here, where the concurrency now lives, rather than only in the epic whose suite
        // happened to catch it.
        [Test]
        public async Task A_portfolio_refresh_and_a_team_refresh_in_different_lanes_still_produce_one_forecast()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            var portfolio = GivenAPortfolioDeliveredBy(team);
            GivenTheTrackerHoldsOnlyThePortfolioRefreshOpenUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);
            await WhenARefreshOfThatTeamRunsToTheEndBesideIt(team);

            await ThenThatPortfolioIsForecastExactlyOnce(portfolio);
        }

        // @driving_port @real-io @AC-01.6 — an operator stopping one runaway refresh must not stop the
        // instance. Cancel was already per key; what is new is that there is now something in another
        // lane for it to fail to hit.
        [Test]
        public async Task Stopping_a_portfolio_refresh_leaves_the_team_refresh_beside_it_running()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);
            await WhenARefreshOfThatTeamIsUnderWay(team);
            await WhenTheOperatorStops(KeyOf(portfolio));

            await ThenThatPortfolioRefreshStops(portfolio);
            await ThenThatTeamRefreshIsStillGoing(team);
        }

        // @driving_port @real-io @AC-01.7 — shutdown. A drain that returns while a lane is still
        // running leaves work in flight against a database the host is about to take away, and leaves
        // the key admitted in a store another replica can see.
        [Test]
        public async Task Shutting_down_waits_for_the_work_in_every_lane()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsEveryRefreshOpenUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);
            await WhenARefreshOfThatTeamIsUnderWay(team);

            await ThenShuttingDownReturnsOnlyOnceBothLanesAreDone(portfolio, team);
        }

        // @driving_port @real-io @error — deletes share their entity type's lane. A removal that could
        // run beside a refresh of the same team is two things writing one entity at once; a removal
        // that waited behind an unrelated portfolio would be the reported bug wearing a different hat.
        [Test]
        public async Task A_team_removal_waits_behind_the_team_refresh_and_not_behind_a_portfolio_refresh()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsEveryRefreshOpenUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);
            await WhenARefreshOfThatTeamIsUnderWay(team);
            GivenARemovalOfThatTeamIsAskedFor(team);

            await ThenThatRemovalIsWaitingForTheTeamRefreshAndNotForThePortfolioOne(team, portfolio);
        }
    }
}
