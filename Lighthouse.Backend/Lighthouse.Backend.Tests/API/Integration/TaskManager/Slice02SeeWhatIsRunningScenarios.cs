using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 02 / ADO #5840: see what
    /// Lighthouse is doing right now. Driving port: the System-Administrator-guarded task list on
    /// <c>UpdateController</c>, exercised over HTTP. US-02, AC-02.1 … AC-02.9.
    ///
    /// Everything here is observed through the endpoint rather than through the status store, so the
    /// shape of the port the controller reads stays DELIVER's decision. What these scenarios fix is the
    /// answer an operator gets.
    ///
    /// Two acceptance criteria are answered elsewhere, neither silently:
    ///
    /// AC-02.1's second half — the Redis store answers about work admitted by *any* replica — is a
    /// promise about an adapter method that does not exist yet, so there is nothing to drive from
    /// outside. It is authored at the start of DELIVER beside the port change, in
    /// <c>Integration/Containers/ScalabilityTests</c> where the cross-pod fixtures already live.
    ///
    /// AC-02.4 (labels render the tenant's Terminology), AC-02.5 (the popover updates itself),
    /// AC-02.7 (nothing running says so in words) and AC-02.8 (<c>OAuthHealthIcon</c> stays) are
    /// frontend promises; they live in the popover's own specs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-02")]
    public partial class Slice02SeeWhatIsRunningTest
    {
        // @walking_skeleton @driving_port @real-io @AC-02.1 @AC-02.3 @AC-02.9
        [Test]
        public async Task A_refresh_that_is_running_is_listed_by_name()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);

            await ThenTheTaskListShowsExactly(OneRunningTeamNamed(team));
        }

        // @driving_port @real-io @AC-02.1 @AC-02.3 — the other half of the refresh cycle.
        [Test]
        public async Task A_portfolio_refresh_that_is_running_is_listed_by_name()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatPortfolioIsUnderWay(portfolio);

            await ThenTheTaskListShowsExactly(OneRunningPortfolioNamed(portfolio));
        }

        // @driving_port @real-io @AC-02.1 @AC-02.3 — the question the list exists to answer is "what is
        // waiting", not only "what is busy". One lane means the second refresh is genuinely queued.
        [Test]
        public async Task A_refresh_waiting_its_turn_is_listed_as_queued()
        {
            var running = GivenATeamThatIsRefreshedOnSchedule();
            var waiting = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(running);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(waiting);

            await ThenTheTaskListShows(OneRunningTeamNamed(running), OneQueuedTeamNamed(waiting));
        }

        // @driving_port @real-io @AC-02.3 — the naming half of deferred item G, pulled in on 2026-09-14.
        // #5877 is a user who watched three Teams sit behind one Portfolio for 3h38m and read it as hung;
        // a row that says only "queued" is what let that happen.
        [Test]
        public async Task A_queued_refresh_names_what_is_holding_the_lane()
        {
            var holdingTheLane = GivenATeamThatIsRefreshedOnSchedule();
            var waiting = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(holdingTheLane);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(waiting);

            await ThenTheQueuedRowSaysItIsWaitingBehind(waiting, holdingTheLane);
            await ThenTheRunningRowSaysItIsWaitingBehindNothing(holdingTheLane);
        }

        // @driving_port @real-io @error @AC-02.3 — a Team deleted while its refresh was in flight must
        // not take the whole list down with it.
        [Test]
        public async Task A_refresh_whose_entity_has_gone_is_still_listed_by_what_it_is()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            GivenThatTeamIsDeletedWhileItsRefreshIsStillRunning(team);

            await ThenTheTaskListStillDescribesThatWorkByTypeAndId(team);
        }

        // @driving_port @real-io @AC-02.7 — the backend half: nothing running is an empty list, not an
        // error. What an operator reads instead of an empty box is the popover's promise.
        [Test]
        public async Task With_nothing_running_the_task_list_is_empty_and_says_so_without_failing()
        {
            GivenATeamThatIsRefreshedOnSchedule();

            await ThenTheTaskListIsAnsweredAndEmpty();
        }

        // @driving_port @real-io @error @AC-02.6 — the same refusal the refresh log already gives.
        [Test]
        public async Task The_task_list_is_refused_to_somebody_who_is_not_a_system_administrator()
        {
            GivenATeamThatIsRefreshedOnSchedule();

            await ThenTheTaskListRefusesANonAdministratorTheSameWayTheRefreshLogDoes();
        }

        // @driving_port @real-io @AC-02.2 — the endpoint being replaced is already wrong under Redis with
        // more than one replica: it counts a dictionary only the answering pod can see. Reading through
        // the port is the fix, and work admitted through the port is how that is observable from outside.
        [Test]
        public async Task The_task_list_reports_work_admitted_through_the_shared_store()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            GivenWorkIsAdmittedThroughTheStoreWithoutThisControllerEverSeeingIt(team);

            await ThenTheTaskListShowsExactly(OneQueuedTeamNamed(team));
        }

        // @driving_port @real-io @AC-02.3 — UpdateType has five members and the frontend union knows
        // three (S11). The delete types reach this list, so they have to read as something.
        [Test]
        public async Task A_delete_that_is_waiting_is_listed_as_something_a_reader_can_understand()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            GivenADeleteOfThatTeamIsAdmitted(team);

            await ThenTheTaskListDescribesThatWorkWithoutSayingUndefined(team);
        }
    }
}
