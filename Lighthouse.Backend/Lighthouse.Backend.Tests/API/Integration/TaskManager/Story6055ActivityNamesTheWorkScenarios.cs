using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (User Story #6055 — the Activity list names entities, not work),
    /// slice 02: nothing in the list says it is waiting behind itself. Driving port: the
    /// System-Administrator-guarded task list on <c>UpdateController</c>, exercised over HTTP.
    /// AC-02.1 … AC-02.7.
    ///
    /// Every scenario here is observed through the endpoint. What they fix is the answer an operator
    /// gets, not how the controller arrives at it.
    ///
    /// Slice 01 is entirely a frontend promise — what a row reads — and lives in the popover's own
    /// specs. AC-02.8 (the comparison happens in one place) is likewise asserted there, by handing the
    /// browser a payload whose flag and whose identity disagree and requiring the flag to win.
    ///
    /// Pending until DELIVER: every test carries <c>[Ignore]</c>, so the suite is green at hand-off and
    /// each is un-skipped as its step is implemented. They are RED rather than BROKEN when un-skipped —
    /// the assertions read JSON, so nothing here fails to compile against today's response.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6055-activity-names-the-work")]
    [Category("slice-02")]
    public partial class Story6055ActivityNamesTheWorkTest
    {
        // @driving_port @real-io @AC-02.1 — the reported defect's second half. A Portfolio refresh
        // triggers a forecast of the same Portfolio, so this pair is the ordinary case, not an edge.
        [Test]
        [Ignore(Pending)]
        public async Task A_queued_forecast_does_not_say_it_is_waiting_behind_its_own_portfolio()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();

            GivenARefreshOfThatPortfolioIsRunning(portfolio);
            GivenAForecastOfThatPortfolioIsQueued(portfolio);

            await ThenTheQueuedRowSaysItIsBehindItsOwn(UpdateType.Forecasts, portfolio.Id, UpdateType.Features);
            await ThenNoRowNamesItsOwnEntityAsWhatItIsWaitingFor();
        }

        // @driving_port @real-io @AC-02.2 — the already-shipped reading, which must not regress. A row
        // waiting for something else still learns its name.
        [Test]
        [Ignore(Pending)]
        public async Task A_queued_team_still_learns_the_name_of_the_portfolio_holding_the_lane()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            var team = GivenATeamThatIsRefreshedOnSchedule();

            GivenARefreshOfThatPortfolioIsRunning(portfolio);
            GivenARefreshOfThatTeamIsQueued(team);

            await ThenTheQueuedRowSaysItIsBehindTheEntityNamed(UpdateType.Team, team.Id, portfolio.Name);
        }

        // @driving_port @real-io @AC-02.3 — the clause names what the HOLDER is doing, never what the
        // queued row is doing. A removal waiting behind a refresh reads as a refresh.
        [Test]
        [Ignore(Pending)]
        public async Task A_queued_removal_behind_its_own_refresh_names_the_refresh()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();

            GivenARefreshOfThatPortfolioIsRunning(portfolio);
            GivenARemovalOfThatPortfolioIsQueued(portfolio);

            await ThenTheQueuedRowSaysItIsBehindItsOwn(UpdateType.PortfolioDelete, portfolio.Id, UpdateType.Features);
        }

        // @driving_port @real-io @AC-02.3 — and the mirror, so the clause cannot be reading the queued
        // row's own type by accident. Both scenarios pass against an implementation that echoes the
        // wrong side only if the two types happen to match, which is why both exist.
        [Test]
        [Ignore(Pending)]
        public async Task A_queued_refresh_behind_its_own_removal_names_the_removal()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();

            GivenARemovalOfThatPortfolioIsRunning(portfolio);
            GivenARefreshOfThatPortfolioIsQueued(portfolio);

            await ThenTheQueuedRowSaysItIsBehindItsOwn(UpdateType.Features, portfolio.Id, UpdateType.PortfolioDelete);
        }

        // @driving_port @real-io @error @AC-02.4 — THE criterion. A Team and a Portfolio that happen to
        // share an integer are two entities, not one. An implementation comparing ids alone passes every
        // other scenario in this file and fails this one; that is the whole reason it is written.
        [Test]
        [Ignore(Pending)]
        public async Task A_team_queued_behind_a_portfolio_with_the_same_id_is_not_waiting_behind_itself()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();

            GivenARefreshOfThatPortfolioIsRunning(portfolio);
            GivenARefreshIsQueuedForATeamWhoseIdIs(portfolio.Id);

            await ThenTheQueuedRowSaysItIsBehindTheEntityNamed(UpdateType.Team, portfolio.Id, portfolio.Name);
        }

        // @driving_port @real-io @AC-02.5 — work whose lane is free is waiting for nothing, and the
        // honest answer is no clause rather than an arbitrary running row.
        [Test]
        [Ignore(Pending)]
        public async Task Work_that_is_waiting_for_nothing_says_nothing()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            GivenARefreshOfThatTeamIsQueued(team);

            await ThenTheRowIsWaitingForNothing(UpdateType.Team, team.Id);
        }

        // @driving_port @real-io @AC-02.6 — the contract guard. ADR-205 changes one field's type and is
        // affordable only because it changes nothing else; this is where that claim is checked, on the
        // serialised payload, where a consumer would break.
        [Test]
        [Ignore(Pending)]
        public async Task Every_other_field_of_a_task_row_is_what_it_has_always_been()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            GivenARefreshOfThatTeamIsQueued(team);

            await ThenTheRowStillCarriesEveryFieldItAlwaysDid(UpdateType.Team, team.Id, team.Name);
        }

        // @driving_port @real-io @AC-02.7 @production-data — the dogfood check. Named here so it is part
        // of the suite's record rather than a step somebody remembers to do; it stays ignored in CI
        // because it needs a real connection, and is run by hand at slice close.
        [Test]
        [Ignore(ProductionData)]
        public async Task On_a_real_instance_a_triggered_forecast_does_not_name_its_own_portfolio()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();

            await WhenARealRefreshOfThatPortfolioTriggersItsForecast(portfolio);

            await ThenNoRowNamesItsOwnEntityAsWhatItIsWaitingFor();
        }
    }
}
