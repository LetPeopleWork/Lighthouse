using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for stage rules. Rules are saved the way an admin saves them, through the Team
    /// settings write; tags are what the Team's tracker holds; votes are cast by the scenario's voters; the
    /// tab is read as somebody who has not voted.
    /// </summary>
    public partial class Slice03StageRulesTest
    {
        private const string PulsarRefinementState = "Refinement";

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityRefinesWithoutStages()
            => await GravityRefinesSixWorkItems();

        private async Task<TeamUnderTest> GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule()
            => await GravityWithItsReadyRule((UserActivityTracking, ReadyTag), (AdvancedSearch, ReadyTag));

        private async Task<TeamUnderTest> GivenGravityWithItsReadyRuleTagging(params (string WorkItem, string Tag)[] tags)
            => await GravityWithItsReadyRule(tags);

        private async Task<TeamUnderTest> GivenGravityWithBothRules(params (string WorkItem, string Tag)[] tags)
        {
            var gravity = await GravityRefinesSixWorkItems(tags);
            await TheAdminHasSetTheStageRules(gravity, ready: TagsContain(ReadyTag), beingRefined: TagsContain(AnalysingTag));
            return gravity;
        }

        private async Task<TeamUnderTest> GivenGravityWithOnlyABeingRefinedRuleTagging(params (string WorkItem, string Tag)[] tags)
        {
            var gravity = await GravityRefinesSixWorkItems(tags);
            await TheAdminHasSetTheStageRules(gravity, ready: null, beingRefined: TagsContain(AnalysingTag));
            return gravity;
        }

        /// <summary>
        /// Team Pulsar's only refinement state is its To Do state "Refinement", holding PU-110 to PU-115; four
        /// of them carry the tag "ready", and the admin has set the Ready rule "Tags contains ready".
        /// </summary>
        private async Task<TeamUnderTest> GivenPulsarRefinesInOneStateWithFourOfSixTaggedReady()
        {
            var pulsar = SeedTeam("Team Pulsar", [PulsarRefinementState], [Implementation], [Done], []);

            SeedTaggedWorkItems(pulsar,
                [("PU-110", ReadyTag), ("PU-111", ReadyTag), ("PU-112", ReadyTag), ("PU-113", ReadyTag)],
                [.. Enumerable.Range(110, 6).Select(number => new TrackerWorkItem(
                    $"PU-{number}", $"Pulsar Work Item {number}", PulsarRefinementState, StateCategories.ToDo, $"{number}"))]);

            await TheAdminHasChosen(pulsar, PulsarRefinementState);
            await TheAdminHasSetTheStageRules(pulsar, ready: TagsContain(ReadyTag), beingRefined: null);
            return pulsar;
        }

        /// <summary>
        /// Gravity's connection offered the additional field "Priority", and the admin set the Ready rule
        /// "Priority is not High" on it. Then the connection's additional fields were edited, which re-creates
        /// "Priority" under a new id: the rule now names a field the connection no longer offers.
        /// </summary>
        private async Task<TeamUnderTest> GivenGravityWithAReadyRuleOnAnAdditionalFieldTheConnectionNoLongerOffers()
        {
            var gravity = await GravityRefinesSixWorkItems();
            var priority = TheConnectionOffersPriority(gravity);
            await TheAdminHasSetTheStageRules(gravity, ready: ARuleOn($"additionalField.{priority}", "notequals", "High"), beingRefined: null);

            TheConnectionRecreatesPriority(gravity, priority);
            return gravity;
        }

        private int TheConnectionOffersPriority(TeamUnderTest team)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var priority = new AdditionalFieldDefinition { DisplayName = "Priority", Reference = "priority", WorkTrackingSystemConnectionId = team.ConnectionId };
            context.Add(priority);
            context.SaveChanges();
            return priority.Id;
        }

        private void TheConnectionRecreatesPriority(TeamUnderTest team, int priorityId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = context.WorkTrackingSystemConnections
                .Include(candidate => candidate.AdditionalFieldDefinitions)
                .Single(candidate => candidate.Id == team.ConnectionId);

            connection.AdditionalFieldDefinitions.RemoveAll(field => field.Id == priorityId);
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = "Priority", Reference = "priority" });
            context.SaveChanges();
        }

        private async Task GivenJonasSaidYesOn(TeamUnderTest team, string workItem)
            => await HasVoted(Jonas, team, workItem, Answer.Yes);

        private async Task GivenJonasSaidNoOn(TeamUnderTest team, string workItem)
            => await HasVoted(Jonas, team, workItem, Answer.No);

        private async Task GivenThreeVotersSaidYesOn(TeamUnderTest team, string workItem)
        {
            await HasVoted(Jonas, team, workItem, Answer.Yes);
            await HasVoted(Mo, team, workItem, Answer.Yes);
            await HasVoted(Ana, team, workItem, Answer.Yes);
        }

        // --- When ---

        private async Task<JsonElement> WhenTheCoachOpensTheRefinementTab(TeamUnderTest team)
            => await ReadTheRefinementTab(team);

        private async Task<HttpResponseMessage> WhenTheAdminSavesTheReadyRule(TeamUnderTest team, JsonObject ready)
            => await SaveTheRefinementSectionWith(team, "stageRules", StageRules(ready, null));

        /// <summary>The settings form saved with the refinement section exactly as the settings read gave it.</summary>
        private async Task<HttpResponseMessage> WhenTheAdminSavesTheSettingsAsTheyReadThem(TeamUnderTest team)
        {
            TheCallerAdministersTheTeam(team);
            var settings = await ReadTheTeamSettings(team);

            var payload = TheTeamSettingsFormFor(team);
            payload["refinement"] = JsonNode.Parse(RefinementSectionOf(settings).GetRawText());

            return await PutTheTeamSettings(team, payload);
        }

        private async Task WhenTheSettingsAreSaved(TeamUnderTest team, SaveShape shape)
        {
            using var save = await SaveTheSettingsShaped(team, shape);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), await save.Content.ReadAsStringAsync());
        }

        // --- Then ---

        private static void ThenTheStageRulesAre(JsonElement settings, string? ready, string? beingRefined)
            => Assert.That(StageRulesIn(settings), Is.EqualTo(new StageRulesReading(TagsContainReading(ready), TagsContainReading(beingRefined))));

        private static void ThenTheTabHasNoStages(JsonElement tab)
        {
            var rows = RowsIn(tab).Select(row => StageRowOf(tab, row.ReferenceId!)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StagesConfiguredIn(tab), Is.False, "the tab must say the Team uses no stages");
                Assert.That(rows.Select(row => row.Stage), Is.Not.Empty.And.All.Null, "no row has a stage when the Team sets no stage rule");
                Assert.That(rows.Select(row => row.SignalsDisagree), Is.All.False, "with one signal there is nothing to disagree with");
            }
        }

        private static void ThenTheReadyCountIs(JsonElement tab, int ready, string source)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadyCountIn(tab), Is.EqualTo(ready));
                Assert.That(ReadySourceIn(tab), Is.EqualTo(source), "the tab must say which signal it counted");
            }
        }

        private static void ThenTheRowReads(JsonElement tab, StageRowReading expected)
            => Assert.That(StageRowOf(tab, expected.ReferenceId), Is.EqualTo(expected));

        private static void ThenTheStagesAre(JsonElement tab, params (string WorkItem, string Stage)[] expected)
        {
            var stages = expected.Select(entry => (entry.WorkItem, StageRowOf(tab, entry.WorkItem).Stage)).ToList();

            Assert.That(stages, Is.EqualTo(expected.Select(entry => (entry.WorkItem, (string?)entry.Stage)).ToList()));
        }

        private static void ThenTheWorkItemIsNotListed(JsonElement tab, string workItem)
            => Assert.That(ReferencesListedIn(tab), Does.Not.Contain(workItem));

        private async Task ThenTheRowCarriesOneVoteAndItsReadyStage(TeamUnderTest team, string workItem)
        {
            var tab = await ReadTheRefinementTab(team);
            var votes = RowOf(tab, workItem);
            var stage = StageRowOf(tab, workItem).Stage;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(votes.VoteCount, Is.EqualTo(1), "the vote on a Ready Work Item is taken and counted");
                Assert.That(stage, Is.EqualTo(ReadyStage), "and its stage is still Ready");
            }
        }

        private async Task ThenTheSaveIsRefusedAndTheReadyRuleIsStill(HttpResponseMessage refused, TeamUnderTest team, string ready)
        {
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(StageRulesIn(settings).Ready, Is.EqualTo(TagsContainReading(ready)), "the refused save changed the stored Ready rule");
            }
        }

        private async Task ThenTheSaveIsAcceptedWithAReadyRuleReading(HttpResponseMessage save, TeamUnderTest team, StageRuleReading expected)
        {
            var body = await save.Content.ReadAsStringAsync();
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(StageRulesIn(settings).Ready, Is.EqualTo(expected));
            }
        }

        private async Task ThenTheSaveIsAcceptedAndTheTeamHasNoReadyRule(HttpResponseMessage save, TeamUnderTest team)
        {
            var body = await save.Content.ReadAsStringAsync();
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(StageRulesIn(settings).Ready, Is.Null, "a rule whose only field went away is no rule");
            }
        }

        private async Task ThenTheTeamStillHoldsItsWorkItemsAndTheReadyRule(TeamUnderTest team, int workItemsBefore, string ready)
        {
            var settings = await ReadTheTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WorkItemsStoredFor(team), Is.EqualTo(workItemsBefore));
                Assert.That(StageRulesIn(settings).Ready, Is.EqualTo(TagsContainReading(ready)), "the rule was saved, so the kept Work Items prove something");
            }
        }
    }
}
