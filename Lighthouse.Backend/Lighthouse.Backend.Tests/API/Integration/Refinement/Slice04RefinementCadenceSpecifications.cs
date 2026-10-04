using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the Refinement cadence. The cadence is saved the way an admin saves it, through
    /// the Team settings write; the day is the instance's clock; the date is read off the tab as a fact, an
    /// ISO calendar day, never a sentence.
    /// </summary>
    public partial class Slice04RefinementCadenceTest
    {
        private static readonly CadenceReading ThursdaysEveryWeek = new(Thursday, 1, null);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityRefinesWithoutACadence()
            => await GravityRefinesSixWorkItems();

        private async Task<TeamUnderTest> GivenGravityRefinesOnThursdaysEveryWeek()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();
            await TheAdminHasSetTheCadence(gravity, [Thursday], 1, null);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenGravityRefinesOnTuesdaysEveryOtherWeekFromTheFifth()
        {
            var gravity = await GivenGravityRefinesWithoutACadence();
            await TheAdminHasSetTheCadence(gravity, [Tuesday], 2, "2026-10-05");
            return gravity;
        }

        // --- When ---

        private async Task<JsonElement> WhenTheCoachOpensTheRefinementTab(TeamUnderTest team)
            => await ReadTheRefinementTab(team);

        private async Task<HttpResponseMessage> WhenTheAdminSavesTheCadence(TeamUnderTest team, JsonObject cadence)
            => await SaveTheRefinementSectionWith(team, "cadence", cadence);

        private async Task WhenTheSettingsAreSaved(TeamUnderTest team, SaveShape shape)
        {
            using var save = await SaveTheSettingsShaped(team, shape);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), await save.Content.ReadAsStringAsync());
        }

        // --- Then ---

        private static void ThenTheCadenceIs(JsonElement settings, CadenceReading expected)
            => Assert.That(CadenceIn(settings), Is.EqualTo(expected));

        private static void ThenTheNextRefinementIs(JsonElement tab, string nextRefinement, bool isRefinementDay)
            => Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading(nextRefinement, isRefinementDay)));

        private static void ThenTheNextRefinementIsDaysAway(JsonElement tab, string nextRefinement, bool isRefinementDay, int daysAway)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading(nextRefinement, isRefinementDay)));
                Assert.That(NumberOf(tab, "daysUntilNextRefinement"), Is.EqualTo(daysAway), $"Body: {tab}");
            }
        }

        private async Task ThenTheSaveIsRefusedAndTheCadenceIsStillThursdaysEveryWeek(HttpResponseMessage refused, TeamUnderTest team)
        {
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(CadenceIn(settings), Is.EqualTo(ThursdaysEveryWeek), "the refused save changed the stored cadence");
            }
        }

        private async Task ThenTheTeamHasNoCadenceAndTheTabNamesNoDate(TeamUnderTest team)
        {
            var settings = await ReadTheTeamSettings(team);
            var tab = await ReadTheRefinementTab(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CadenceIn(settings), Is.Null);
                Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading(null, false)));
            }
        }

        private async Task ThenTheTeamStillHoldsItsWorkItemsAndRefinesOnThursdays(TeamUnderTest team, int workItemsBefore)
        {
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WorkItemsStoredFor(team), Is.EqualTo(workItemsBefore));
                Assert.That(CadenceIn(settings), Is.EqualTo(ThursdaysEveryWeek), "the cadence was saved, so the kept Work Items prove something");
            }
        }

        /// <summary>No date is a stated fact - false and an explicit null - not a missing field.</summary>
        private static void ThenTheTabListsAllSixAndNamesNoDate(JsonElement tab)
        {
            var namesTheDay = tab.TryGetProperty("nextRefinementDate", out var date) && date.ValueKind == JsonValueKind.Null;
            var countsNoDays = tab.TryGetProperty("daysUntilNextRefinement", out var days) && days.ValueKind == JsonValueKind.Null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RowsIn(tab), Has.Count.EqualTo(6));
                Assert.That(namesTheDay, Is.True, $"the tab must say there is no next Refinement. Body: {tab}");
                Assert.That(countsNoDays, Is.True, $"the tab must say there are no days to count. Body: {tab}");
                Assert.That(CadenceFactsIn(tab).IsRefinementDay, Is.False);
            }
        }

        private async Task ThenTheVoteCountsAndTheTabNamesNoDate(TeamUnderTest team, string workItem)
        {
            var tab = await ReadTheRefinementTab(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RowOf(tab, workItem).VoteCount, Is.EqualTo(1));
                Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading(null, false)));
            }
        }
    }
}
