using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for readiness. The setting is saved the way an admin saves it, through the Team
    /// settings write; votes are cast by the scenario's voters; the row's readiness is read from the tab as
    /// somebody who has not voted sees it, because readiness is shown to everybody.
    /// </summary>
    public partial class Slice13ReadinessTest
    {
        private const string Ready = "Ready";

        private const string MoreYesNeeded = "MoreYesNeeded";

        private const string MoreVotersNeeded = "MoreVotersNeeded";

        private const string NeedsDiscussion = "NeedsDiscussion";

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        private static readonly Voter Priya = ABrowserOf(PriyaSharma);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityRefinesAndNobodyHasVoted()
            => await GravityRefinesSixWorkItemsNobodyHasVotedOn();

        private async Task<TeamUnderTest> GivenTheAdminChoseTwoYesFromTwoVoters()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 2);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasAndMoSaidYesAndAnaSaidYesButOn(string workItem)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await GivenJonasAndMoSaidYesAndAnaSaidYesButOn(gravity, workItem);
            return gravity;
        }

        private async Task GivenJonasAndMoSaidYesAndAnaSaidYesButOn(TeamUnderTest team, string workItem)
        {
            await HasVoted(Jonas, team, workItem, Answer.Yes);
            await HasVoted(Mo, team, workItem, Answer.Yes);
            await HasVoted(Ana, team, workItem, Answer.YesBut, "only if the PDF export moves to its own Work Item");
        }

        private async Task<TeamUnderTest> GivenOnlyJonasSaidYesOn(string workItem)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Jonas, gravity, workItem, Answer.Yes);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasAndMoSaidYesOn(string workItem)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Jonas, gravity, workItem, Answer.Yes);
            await HasVoted(Mo, gravity, workItem, Answer.Yes);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasAndMoSaidYesAndAnaSaidNoOn(string workItem)
        {
            var gravity = await GivenJonasAndMoSaidYesOn(workItem);
            await HasVoted(Ana, gravity, workItem, Answer.No);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenReadinessOfTwoYesFromThreeVotersAndTwoYesOn(string workItem)
        {
            var gravity = await GivenJonasAndMoSaidYesOn(workItem);
            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 3);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenAVetoOfOneCounting(VetoCounts counts)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await TheAdminHasSetReadiness(gravity, minYes: 3, minVoters: 3, vetoThreshold: 1, counts);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenAVetoOfOneNoAndThreeYesAndAnasNoOn(string workItem)
        {
            var gravity = await GivenAVetoOfOneCounting(VetoCounts.No);
            await ThreeYesAndOneNoFromAnaOn(gravity, workItem);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenAVetoOfTwoNoAndThreeYesAndOneNoOn(string workItem)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await TheAdminHasSetReadiness(gravity, minYes: 3, minVoters: 3, vetoThreshold: 2);
            await ThreeYesAndOneNoFromAnaOn(gravity, workItem);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenTwoWorkItemsReadyByVotesAndOneShortOfIt()
        {
            var gravity = await GivenJonasAndMoSaidYesAndAnaSaidYesButOn(ConfigurationManagement);
            await GivenJonasAndMoSaidYesAndAnaSaidYesButOn(gravity, UserActivityTracking);
            await HasVoted(Jonas, gravity, AdvancedReporting, Answer.Yes);
            await HasVoted(Mo, gravity, AdvancedReporting, Answer.Yes);
            return gravity;
        }

        private async Task ThreeYesAndOneNoFromAnaOn(TeamUnderTest team, string workItem)
        {
            await HasVoted(Jonas, team, workItem, Answer.Yes);
            await HasVoted(Mo, team, workItem, Answer.Yes);
            await HasVoted(Priya, team, workItem, Answer.Yes);
            await HasVoted(Ana, team, workItem, Answer.No);
        }

        // --- When ---

        private async Task<JsonElement> WhenTheAdminOpensTheTeamSettings(TeamUnderTest team)
        {
            TheCallerAdministersTheTeam(team);
            return await ReadTheTeamSettings(team);
        }

        private async Task<HttpResponseMessage> WhenTheAdminSavesReadiness(TeamUnderTest team, int minYes, int minVoters, int? vetoThreshold = null)
        {
            TheCallerAdministersTheTeam(team);
            return await SaveTheReadiness(team, minYes, minVoters, vetoThreshold);
        }

        private async Task<HttpResponseMessage> WhenTheSettingsAreSavedWithoutReadiness(TeamUnderTest team)
        {
            TheCallerAdministersTheTeam(team);
            return await SaveTheRefinementSection(team, readiness: null);
        }

        private async Task<JsonElement> WhenPriyaOpensTheRefinementTab(TeamUnderTest team)
            => await TheTabAsSeenBy(ABrowserOf(PriyaSharma), team);

        private async Task WhenAnaVotes(TeamUnderTest team, string workItem, Answer answer)
            => await HasVoted(Ana, team, workItem, answer);

        // --- Then ---

        private static void ThenTheReadinessIs(JsonElement settings, int minYes, int minVoters, int? vetoThreshold, string? vetoCounts)
            => Assert.That(ReadinessIn(settings), Is.EqualTo(new ReadinessReading(minYes, minVoters, vetoThreshold, vetoCounts)));

        private async Task ThenTheSaveIsRefusedAndReadinessIsUnchanged(HttpResponseMessage refused, TeamUnderTest team)
        {
            var readiness = ReadinessIn(await WhenTheAdminOpensTheTeamSettings(team));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(readiness, Is.EqualTo(new ReadinessReading(3, 3, null, null)), "a refused save must leave the readiness the Team had");
            }
        }

        private void ThenTheReadinessIsStoredAndTheWorkItemsKept(JsonElement settings, TeamUnderTest team, int heldBefore)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadinessIn(settings), Is.EqualTo(new ReadinessReading(1, 1, 2, nameof(VetoCounts.No))));
                Assert.That(WorkItemsStoredFor(team), Is.EqualTo(heldBefore));
            }
        }

        private static void ThenTheRowIsReadyAndTheTabCounts(JsonElement tab, string workItem, int readyByVotes)
        {
            var row = RowOf(tab, workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Readiness, Is.EqualTo(Ready));
                Assert.That(row.MissingVotes, Is.Null, "a Ready Work Item is missing nothing");
                Assert.That(ReadyByVotesIn(tab), Is.EqualTo(readyByVotes));
            }
        }

        private static void ThenTheRowNeeds(JsonElement tab, string workItem, string readiness, int missingVotes)
        {
            var row = RowOf(tab, workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Readiness, Is.EqualTo(readiness));
                Assert.That(row.MissingVotes, Is.EqualTo(missingVotes));
                Assert.That(ReadyByVotesIn(tab), Is.Zero);
            }
        }

        private static void ThenTheRowNeedsDiscussionAndIsNotCounted(JsonElement tab, string workItem)
        {
            var row = RowOf(tab, workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Readiness, Is.EqualTo(NeedsDiscussion));
                Assert.That(ReadyByVotesIn(tab), Is.Zero);
            }
        }
    }
}
