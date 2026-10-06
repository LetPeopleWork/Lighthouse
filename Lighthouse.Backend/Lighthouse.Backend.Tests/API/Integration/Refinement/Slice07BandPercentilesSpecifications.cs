using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the band. The band is saved the way an admin saves it, through the Team settings
    /// write; its effect is read off the tab's need facts against a forecast the scenario chose.
    /// </summary>
    public partial class Slice07BandPercentilesTest
    {
        private static readonly BandReading SixtyAndNinetyFive = new(60, 95);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityRefinesWithoutABand()
            => await GravityRefinesSixWorkItems();

        private async Task<TeamUnderTest> GivenGravityReadsItsRangeAt60And95()
        {
            var gravity = await GivenGravityRefinesWithoutABand();
            await TheAdminHasSetTheBand(gravity, 60, 95);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenGravityHasTwoReadyAndIsLikelyToPullFiveToTenOverItsCycle()
            => await GravityWithTwoReadyLikelyToPullFiveToEightOverItsCycle();

        // --- When ---

        private async Task<JsonElement> WhenTheCoachOpensTheRefinementTab(TeamUnderTest team)
            => await ReadTheRefinementTab(team);

        private async Task<HttpResponseMessage> WhenTheAdminSavesTheBand(TeamUnderTest team, JsonObject band)
            => await SaveTheRefinementSectionWith(team, "band", band);

        private async Task WhenTheSettingsAreSaved(TeamUnderTest team, SaveShape shape)
        {
            using var save = await SaveTheSettingsShaped(team, shape);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), await save.Content.ReadAsStringAsync());
        }

        // --- Then ---

        private static void ThenTheBandIs(JsonElement settings, int lowPercentile, int highPercentile)
            => Assert.That(BandIn(settings), Is.EqualTo(new BandReading(lowPercentile, highPercentile)));

        private static void ThenTheNeedIs(JsonElement tab, NeedReading expected)
            => Assert.That(NeedIn(tab), Is.EqualTo(expected));

        /// <summary>The refusal names both values it judged, so the admin can see which pair was wrong.</summary>
        private async Task ThenTheSaveIsRefusedNamingAndTheBandIsStill60And95(HttpResponseMessage refused, TeamUnderTest team, int lowPercentile, int highPercentile)
        {
            var reason = await refused.Content.ReadAsStringAsync();
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(reason, Does.Contain($"'{lowPercentile}' to '{highPercentile}'"), "the refusal must name both values");
                Assert.That(BandIn(settings), Is.EqualTo(SixtyAndNinetyFive), "the refused save changed the stored band");
            }
        }

        private async Task ThenTheSaveIsAcceptedAndTheBandIs(HttpResponseMessage save, TeamUnderTest team, int lowPercentile, int highPercentile)
        {
            var body = await save.Content.ReadAsStringAsync();
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(BandIn(settings), Is.EqualTo(new BandReading(lowPercentile, highPercentile)));
            }
        }

        private async Task ThenTheTeamStillHoldsItsWorkItemsAndReadsItsRangeAt60And95(TeamUnderTest team, int workItemsBefore)
        {
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WorkItemsStoredFor(team), Is.EqualTo(workItemsBefore));
                Assert.That(BandIn(settings), Is.EqualTo(SixtyAndNinetyFive), "the band was saved, so the kept Work Items prove something");
            }
        }
    }
}
