using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for readiness. The setting is saved the way an admin saves it, through the Team
    /// settings write; votes are cast by the scenario's voters; the row's readiness is read from the tab as
    /// somebody who has not voted sees it.
    /// </summary>
    public partial class Slice13ReadinessTest
    {
        private const string Ready = "Ready";

        private const string MoreYesNeeded = "MoreYesNeeded";

        private const string MoreVotersNeeded = "MoreVotersNeeded";

        private const string NeedsDiscussion = "NeedsDiscussion";

        private const string YesIfCondition = "only if the PDF export moves to its own Work Item";

        private static readonly DiscussWhen TheDefaultDiscussion = new(No: 1, YesIf: 2);

        private static readonly DiscussWhen NoDiscussionOnNo = new(No: null, YesIf: 2);

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        private static readonly Voter Priya = ABrowserOf(PriyaSharma);

        private static readonly Voter[] VotersInOrder = [Jonas, Mo, Ana, Priya];

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityRefinesAndNobodyHasVoted()
            => await GravityRefinesSixWorkItemsNobodyHasVotedOn();

        private async Task<TeamUnderTest> GivenTheAdminChoseTwoYesFromTwoVotersAndNoDiscussionOnNo()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 2, new DiscussWhen(No: null, YesIf: 3));
            return gravity;
        }

        private async Task<TeamUnderTest> GivenTheDiscussionRules(DiscussWhen discussWhen)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await TheAdminHasSetReadiness(gravity, minYes: 3, minVoters: 3, discussWhen);
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
            await HasVoted(Ana, team, workItem, Answer.YesBut, YesIfCondition);
        }

        private async Task<TeamUnderTest> GivenThreeVotersOfWhomSomeSaidYesIfOn(string workItem, int yesIfVotes)
        {
            var answers = Enumerable.Range(0, 3).Select(voter => voter < 3 - yesIfVotes ? Answer.Yes : Answer.YesBut).ToArray();

            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await GivenTheVotersAnsweredOn(gravity, workItem, answers);
            return gravity;
        }

        private async Task GivenTheVotersAnsweredOn(TeamUnderTest team, string workItem, Answer[] answers)
        {
            foreach (var (voter, answer) in VotersInOrder.Zip(answers))
            {
                await HasVoted(voter, team, workItem, answer, answer == Answer.YesBut ? YesIfCondition : null);
            }
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

        private async Task<TeamUnderTest> GivenNoDiscussionOnNoAndJonasAndMoSaidYesAndAnaSaidNoOn(string workItem)
        {
            var gravity = await GivenTheDiscussionRules(NoDiscussionOnNo);
            await HasVoted(Jonas, gravity, workItem, Answer.Yes);
            await HasVoted(Mo, gravity, workItem, Answer.Yes);
            await HasVoted(Ana, gravity, workItem, Answer.No);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenReadinessOfTwoYesFromThreeVotersAndTwoYesOn(string workItem, DiscussWhen? discussWhen = null)
        {
            var gravity = await GivenJonasAndMoSaidYesOn(workItem);
            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 3, discussWhen);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenThreeYesAndAnasNoOn(string workItem)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Jonas, gravity, workItem, Answer.Yes);
            await HasVoted(Mo, gravity, workItem, Answer.Yes);
            await HasVoted(Priya, gravity, workItem, Answer.Yes);
            await HasVoted(Ana, gravity, workItem, Answer.No);
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

        // --- When ---

        private async Task<JsonElement> WhenTheAdminOpensTheTeamSettings(TeamUnderTest team)
        {
            TheCallerAdministersTheTeam(team);
            return await ReadTheTeamSettings(team);
        }

        private async Task<HttpResponseMessage> WhenTheAdminSavesReadiness(TeamUnderTest team, int minYes, int minVoters, DiscussWhen? discussWhen = null)
        {
            TheCallerAdministersTheTeam(team);
            return await SaveTheReadiness(team, minYes, minVoters, discussWhen);
        }

        private async Task<HttpResponseMessage> WhenTheAdminSavesOnlyTheReadinessField(TeamUnderTest team, string field, int value)
        {
            TheCallerAdministersTheTeam(team);
            return await SaveTheRefinementSection(team, new JsonObject { [field] = value });
        }

        private async Task<HttpResponseMessage> WhenTheAdminSavesOnlyTheDiscussionRules(TeamUnderTest team, DiscussWhen discussWhen)
        {
            TheCallerAdministersTheTeam(team);
            return await SaveTheRefinementSection(team, new JsonObject
            {
                ["discussWhen"] = new JsonObject { ["no"] = discussWhen.No, ["yesIf"] = discussWhen.YesIf },
            });
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

        private static void ThenTheReadinessIs(JsonElement settings, int minYes, int minVoters, DiscussWhen discussWhen)
            => Assert.That(ReadinessIn(settings), Is.EqualTo(new ReadinessReading(minYes, minVoters, discussWhen)));

        private async Task ThenTheSaveIsRefusedAndReadinessIsUnchanged(HttpResponseMessage refused, TeamUnderTest team)
        {
            var readiness = ReadinessIn(await WhenTheAdminOpensTheTeamSettings(team));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(readiness, Is.EqualTo(new ReadinessReading(3, 3, TheDefaultDiscussion)), "a refused save must leave the readiness the Team had");
            }
        }

        private void ThenTheReadinessIsStoredAndTheWorkItemsKept(JsonElement settings, TeamUnderTest team, int heldBefore)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadinessIn(settings), Is.EqualTo(new ReadinessReading(1, 1, new DiscussWhen(No: 2, YesIf: null))));
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
