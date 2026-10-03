using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for choosing the refinement states. What these observe is only what the Team
    /// settings read, the Team read and the terminology read answer, plus - for the one scenario about
    /// keeping Work Items - how many Work Items the Team still holds.
    /// </summary>
    public partial class Slice01RefinementStatesTest
    {
        private const string Refining = "Refining";

        private const string Grooming = "Grooming";

        private const string Icebox = "Icebox";

        // --- Given ---

        private TeamUnderTest GivenTeamGravityWithNoRefinementStates() => ATeamMappedLikeGravity();

        private async Task<TeamUnderTest> GivenGravitysAdminHasChosen(params string[] states)
        {
            var gravity = ATeamMappedLikeGravity();
            TheCallerAdministersTheTeam(gravity);

            using var save = await SaveTheTeamSettingsChoosing(gravity, states);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin's first choice of refinement states was not saved, so nothing after it can be about changing them. {await save.Content.ReadAsStringAsync()}");

            return gravity;
        }

        private TeamUnderTest GivenATeamThatMapsAnalysingAndGroomingAsRefining()
            => SeedTeam(
                "Team Orbit",
                [Backlog],
                [Refining, Implementation],
                [Done],
                [new StateMapping { Name = Refining, States = [Analysing, Grooming] }]);

        private async Task<TeamUnderTest> GivenGravitysChosenAnalysingStoppedBeingMappedWithOnlyBacklogChosen()
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog);
            return await GivenTheAdminStoppedMappingAnalysing(gravity);
        }

        private async Task<TeamUnderTest> GivenTheAdminStoppedMappingAnalysing(TeamUnderTest gravity)
        {
            using var unmapping = await WhenTheAdminStopsMappingAnalysing(gravity);
            Assert.That(unmapping.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin could not stop mapping {Analysing}, so the scenario describes a Team that still maps it. {await unmapping.Content.ReadAsStringAsync()}");

            return gravity with { DoingStates = [Next, Implementation] };
        }

        /// <summary>
        /// Gravity's tracker holds one Work Item in each of its To Do and Doing states. None of them is
        /// listed anywhere by refinement yet; they are simply what the Team already holds.
        /// </summary>
        private TeamUnderTest GivenTeamGravityHoldingWorkItemsInEveryState()
        {
            var gravity = ATeamMappedLikeGravity();

            SeedWorkItem(gravity, "GR-073", "Configuration management", Backlog, StateCategories.ToDo, "3");
            SeedWorkItem(gravity, "GR-051", "Advanced reporting module", Analysing, StateCategories.Doing, "1", startedDaysAgo: 4);
            SeedWorkItem(gravity, "GR-058", "User activity tracking", Next, StateCategories.Doing, "2", startedDaysAgo: 1);
            SeedWorkItem(gravity, "GR-040", "Billing export", Implementation, StateCategories.Doing, "4", startedDaysAgo: 9);

            return gravity;
        }

        // --- When ---

        private async Task<HttpResponseMessage> WhenTheAdminChoosesRefinementStates(TeamUnderTest team, params string[] states)
            => await SaveTheTeamSettingsChoosing(team, states);

        private async Task<HttpResponseMessage> WhenTheReaderTriesToChooseRefinementStates(TeamUnderTest team, params string[] states)
            => await WhenTheAdminChoosesRefinementStates(team, states);

        private async Task<HttpResponseMessage> WhenTheTeamSettingsAreSavedWithoutTheRefinementSection(TeamUnderTest team)
            => await SaveTheTeamSettingsLeavingRefinementOut(team);

        private async Task<HttpResponseMessage> WhenTheAdminStopsMappingAnalysing(TeamUnderTest team)
            => await SaveTheTeamSettingsWithDoingStates(team, Next, Implementation);

        private async Task<HttpResponseMessage> WhenTheAdminStopsMappingAnalysingWhileStillChoosingIt(TeamUnderTest team)
            => await SaveTheTeamSettingsChoosing(team with { DoingStates = [Next, Implementation] }, Backlog, Analysing, Next);

        // --- Then ---

        private static async Task ThenTheSaveIsAccepted(HttpResponseMessage save)
        {
            var body = await save.Content.ReadAsStringAsync();
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
        }

        private static async Task ThenTheSaveIsRefusedNaming(HttpResponseMessage save, string state)
        {
            var body = await save.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain(state),
                    "The refusal has to name the state, or the admin is left guessing which tick was wrong.");
            }
        }

        private static void ThenTheChangeIsForbidden(HttpResponseMessage save)
            => Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        private static void ThenTheChosenStatesAre(JsonElement settings, params string[] expected)
            => Assert.That(RefinementStatesIn(settings).Select(entry => entry.State), Is.EqualTo(expected),
                $"The refinement states read back are not the ones chosen, in the order chosen. Settings: {settings}");

        private static void ThenNoStateIsChosen(JsonElement settings)
            => Assert.That(RefinementStatesIn(settings), Is.Empty, $"Settings: {settings}");

        private static void ThenTheTeamSaysItHasRefinementStates(JsonElement team)
            => Assert.That(RefinementConfiguredOn(team), Is.True,
                "A Team with refinement states must say so, or its Refinement tab stays off for every reader.");

        private static void ThenTheTeamSaysItHasNoRefinementStates(JsonElement team)
            => Assert.That(RefinementConfiguredOn(team), Is.False,
                "A Team without refinement states must say so, or its Refinement tab opens onto nothing.");

        private void ThenTheTeamStillHoldsItsWorkItems(TeamUnderTest team, int heldBefore)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(heldBefore, Is.GreaterThan(0), "The Team held no Work Items to begin with, so keeping them proves nothing.");
                Assert.That(WorkItemsStoredFor(team), Is.EqualTo(heldBefore),
                    "Saving the refinement states made the Team throw away Work Items it already held.");
            }
        }

        private static void ThenTheTerminologyOffersRefinementInBothForms(JsonElement terminology)
        {
            var defaults = terminology.EnumerateArray()
                .Select(entry => (Key: TextOf(entry, "key"), Default: TextOf(entry, "defaultValue")))
                .Where(entry => entry.Key is not null)
                .ToDictionary(entry => entry.Key ?? string.Empty, entry => entry.Default, StringComparer.Ordinal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaults.GetValueOrDefault("refinement"), Is.EqualTo("Refinement"));
                Assert.That(defaults.GetValueOrDefault("refinements"), Is.EqualTo("Refinements"));
            }
        }
    }
}
