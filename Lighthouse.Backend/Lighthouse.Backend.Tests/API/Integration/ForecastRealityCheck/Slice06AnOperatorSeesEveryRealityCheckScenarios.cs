namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Acceptance scenarios for the one backend change of the at-a-glance reality check: every check a caller
    /// is allowed to run leaves one line in the log an operator reads at the default level, naming the Team
    /// and the forecast filter choice the caller asked for. A request for a Team that does not exist, or one
    /// refused because the caller cannot read the Team, leaves no such line.
    ///
    /// Driving port: the check itself, over HTTP, on both of its routes. Step definitions live in
    /// Slice06AnOperatorSeesEveryRealityCheckSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4172-forecast-backtest-sweep")]
    [Category("slice-06a")]
    public partial class Slice06AnOperatorSeesEveryRealityCheckTest
    {
        // @driving_port @real-io @us-06 @slice-06a @contract-shape:bounded-change
        [Ignore(Pending)]
        [TestCase(NoOptions, "none", LatestRoute)]
        [TestCase("{\"applyFilterOverride\":null}", "none", LatestRoute)]
        [TestCase("{\"applyFilterOverride\":true}", "on", LatestRoute)]
        [TestCase("{\"applyFilterOverride\":false}", "off", LatestRoute)]
        [TestCase(NoOptions, "none", VersionedRoute)]
        [TestCase("{\"applyFilterOverride\":true}", "on", VersionedRoute)]
        public async Task Every_check_run_writes_one_line_an_operator_sees_naming_the_Team_and_the_filter_choice_asked_for(
            string request, string filterOverride, string route)
        {
            var oceanExplorer = GivenOceanExplorerWithWorkFinishedEveryDay();
            GivenTheLogIsReadFromNowOn();

            await WhenMariaRunsTheCheck(oceanExplorer, request, route);

            ThenTheLogHoldsExactlyOneLineOfARealityCheckAndItReads(
                $"Reality check run for Team {oceanExplorer} (filter override: {filterOverride})");
        }

        // @driving_port @real-io @us-06 @slice-06a @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_check_asked_for_a_Team_that_does_not_exist_writes_no_line()
        {
            var oceanExplorer = GivenOceanExplorerWithWorkFinishedEveryDay();
            await GivenMariaRanTheCheckAndItWasLogged(oceanExplorer);

            await WhenTheCheckIsAskedForATeamNobodyCreated();

            ThenNothingWasLoggedForTheTeamNobodyCreated();
            ThenTheOnlyLineOfARealityCheckIsTheOneFor(oceanExplorer);
        }

        // @driving_port @real-io @us-06 @slice-06a @error @contract-shape:unbounded-preservation
        [Ignore(Pending)]
        [TestCase("{\"applyFilterOverride\":\"yes\"}")]
        [TestCase("{\"applyFilterOverride\":")]
        public async Task A_request_whose_filter_choice_cannot_be_read_is_refused_and_writes_no_line(string request)
        {
            var oceanExplorer = GivenOceanExplorerWithWorkFinishedEveryDay();
            await GivenMariaRanTheCheckAndItWasLogged(oceanExplorer);

            await WhenMariaAsksForTheCheckWithAFilterChoiceNobodyCanRead(oceanExplorer, request);

            ThenTheOnlyLineOfARealityCheckIsTheOneFor(oceanExplorer);
        }

        // @driving_port @real-io @us-06 @slice-06a @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_check_refused_to_somebody_who_cannot_read_the_Team_writes_no_line()
        {
            var oceanExplorer = GivenOceanExplorerWithWorkFinishedEveryDay();
            var harbourPilots = GivenHarbourPilots();
            await GivenMariaRanTheCheckAndItWasLogged(oceanExplorer);

            await WhenSomebodyWhoCanReadOnlyHarbourPilotsAsksForTheCheckOf(oceanExplorer, harbourPilots);

            ThenTheOnlyLineOfARealityCheckIsTheOneFor(oceanExplorer);
        }
    }
}
