using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;
using Serilog.Events;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Step definitions for the operator's log line, read from the log the host writes through its own
    /// pipeline at the level an operator sees by default.
    ///
    /// A "this was not logged" assertion is only worth something when the capture is known to be working, so
    /// every negative scenario first runs a check that is logged, and reads the log without clearing it.
    /// </summary>
    public partial class Slice06AnOperatorSeesEveryRealityCheckTest : ForecastRealityCheckAcceptanceTest
    {
        private const string Pending = "Pending: the reality check writes nothing to the log yet (epic 4172, slice 06a, story 6094).";

        private const string ALineOfARealityCheck = "Reality check run for Team";

        private const int ANumberNoTeamWasEverGiven = 987_654;

        // --- Given ---

        private int GivenOceanExplorerWithWorkFinishedEveryDay()
        {
            var team = GivenATeam("Ocean Explorer", 30);
            GivenTheTeamFinishedAWorkItemEveryDay(team);
            return team;
        }

        private int GivenHarbourPilots() => GivenAnotherTeam("Harbour Pilots");

        /// <summary>Host start-up and seeding log through the same capture, so counting starts at the check.</summary>
        private void GivenTheLogIsReadFromNowOn() => CapturedLogs.Clear();

        private async Task GivenMariaRanTheCheckAndItWasLogged(int teamId)
        {
            GivenTheLogIsReadFromNowOn();
            await WhenMariaRunsTheCheck(teamId, NoOptions, LatestRoute);
            ThenTheLogHoldsExactlyOneLineOfARealityCheckAndItReads(
                $"{ALineOfARealityCheck} {teamId} (filter override: none)");
        }

        // --- When ---

        private async Task WhenMariaRunsTheCheck(int teamId, string request, string route)
        {
            using var response = await WhenTheCheckIsRunBy(
                client => client.AsTeamViewer(teamId, "maria-santos"), teamId, request, route);
            await ReadTheAnswer(response);
        }

        private async Task WhenTheCheckIsAskedForATeamNobodyCreated()
        {
            using var response = await WhenTheCheckIsRunBy(client => client.AsSystemAdmin(), ANumberNoTeamWasEverGiven);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "a check for a Team nobody created is answered as not found");
        }

        private async Task WhenMariaAsksForTheCheckWithAFilterChoiceNobodyCanRead(int teamId, string request)
        {
            using var response = await WhenTheCheckIsRunBy(
                client => client.AsTeamViewer(teamId, "maria-santos"), teamId, request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "a filter choice that is not yes, no or unset is refused before anything is checked");
        }

        private async Task WhenSomebodyWhoCanReadOnlyHarbourPilotsAsksForTheCheckOf(int teamId, int harbourPilots)
        {
            using var response = await WhenTheCheckIsRunBy(
                client => client.AsTeamViewer(harbourPilots, "harbour-pilot"), teamId);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "somebody who cannot read the Team is refused without learning that it exists");
        }

        // --- Then ---

        private List<string> TheLinesOfARealityCheck() =>
            [.. CapturedLogs.At(LogEventLevel.Information).Where(line => line.StartsWith(ALineOfARealityCheck, StringComparison.Ordinal))];

        private void ThenTheLogHoldsExactlyOneLineOfARealityCheckAndItReads(string line)
        {
            Assert.That(TheLinesOfARealityCheck(), Is.EqualTo(new List<string> { line }),
                "every check writes exactly one line at Information, naming only the Team id and the filter choice");
        }

        private void ThenNothingWasLoggedForTheTeamNobodyCreated()
            => CapturedLogs.AssertNothingLoggedMatching($"{ALineOfARealityCheck} {ANumberNoTeamWasEverGiven}");

        private void ThenTheOnlyLineOfARealityCheckIsTheOneFor(int teamId)
        {
            Assert.That(TheLinesOfARealityCheck(), Is.EqualTo(new List<string> { $"{ALineOfARealityCheck} {teamId} (filter override: none)" }),
                "a request that was refused or found no Team adds no line of its own");
        }
    }
}
