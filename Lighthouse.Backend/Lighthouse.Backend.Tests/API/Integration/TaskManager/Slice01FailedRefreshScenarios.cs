using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 01 / ADO Bug #5788: a refresh that
    /// failed says it failed. Driving port: the scheduled refresh. US-01, AC-01.1 … AC-01.6.
    ///
    /// Every later slice of this Epic reads the status these scenarios pin. A task list that shows a
    /// failed refresh as "Completed" is worse than no task list, so this slice ships on its own and first.
    ///
    /// AC-01.4 (work held behind a failed refresh is still let go) lives in
    /// <c>Services/Implementation/BackgroundServices/Update/UpdateQueueServiceTests</c>. A hold only parks
    /// while the key it waits on is Queued rather than running, and the window between a refresh being
    /// admitted and the queue picking it up is not something a test can stand in deterministically from
    /// outside. That class owns the status dictionary, so there it is exact.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-01")]
    public partial class Slice01FailedRefreshTest
    {
        // @walking_skeleton @driving_port @real-io @error @AC-01.1
        [Test]
        public async Task A_team_refresh_that_fails_tells_the_browser_it_failed()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerIsUnreachable();

            await WhenTheScheduledRefreshRuns(team);

            ThenTheBrowserWasToldTheRefreshFailed(team);
        }

        // @driving_port @real-io @error @AC-01.1 — the other half of the refresh cycle, same promise.
        [Test]
        public async Task A_portfolio_refresh_that_fails_tells_the_browser_it_failed()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            GivenTheTrackerIsUnreachable();

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheBrowserWasToldTheRefreshFailed(portfolio);
        }

        // @driving_port @real-io @AC-01.1 — the positive control. Without it, "always report Failed"
        // satisfies every other scenario in this file.
        [Test]
        public async Task A_team_refresh_that_works_still_tells_the_browser_it_completed()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswers();

            await WhenTheScheduledRefreshRuns(team);

            ThenTheBrowserWasToldTheRefreshCompleted(team);
        }

        // @driving_port @real-io @error @AC-01.1 — only the last word changes. A refresh is announced
        // when it is taken on and again when it ends, and this slice moves nothing but the second one.
        [Test]
        public async Task A_failing_refresh_is_announced_as_queued_and_then_as_failed()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerIsUnreachable();

            await WhenTheScheduledRefreshRuns(team);

            ThenTheBrowserWasToldTheRefreshWasQueuedAndThenThatItFailed(team);
        }

        // @driving_port @real-io @error @AC-01.2 — the record that already told the truth keeps telling it.
        [Test]
        public async Task A_failed_refresh_still_records_that_it_did_not_succeed()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerIsUnreachable();

            await WhenTheScheduledRefreshRuns(team);

            ThenTheRecordedRefreshSaysItDidNotSucceed(team);
            ThenTheOperatorSeesOneSummaryLineSayingItDidNotSucceed();
        }

        // @driving_port @real-io @error @AC-01.3 — a round that never finishes silently drops every
        // write it had staged. The failure path has to leave the round exactly as the success path does.
        [Test]
        public async Task A_failed_refresh_still_finishes_its_write_back_round()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerIsUnreachable();

            await WhenTheScheduledRefreshRuns(team);

            ThenTheExecutionReportedItselfFinishedToItsRound();
            ThenTheRoundSaidItsPieceExactlyOnce();
        }

        // @driving_port @real-io @error @AC-01.5 — the blast radius. Somebody who pressed Refresh is
        // told the refresh was accepted; whether it later fails is the task list's business, not the
        // button's, and a 500 here would be a new failure mode invented by this slice.
        [Test]
        public async Task Asking_for_a_refresh_that_goes_on_to_fail_is_still_accepted()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerIsUnreachable();

            var response = await WhenSomebodyAsksForARefreshOf(team);

            ThenTheRequestWasAccepted(response);
            await ThenThatRefreshStillEndedAsFailed(team);
        }

        // @driving_port @real-io @error @AC-01.5 — the other half of the blast radius: the awaitable
        // path already faults its caller, and this slice must not change that.
        [Test]
        public void A_caller_awaiting_an_update_still_sees_the_failure_it_always_saw()
        {
            GivenNothingInParticular();

            var awaited = WhenSomebodyAwaitsAnUpdateThatThrows();

            ThenThatCallerSawTheFailure(awaited);
        }
    }
}
