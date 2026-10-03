using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for casting a vote without sign-in. Each voter is a browser of their own; what a
    /// scenario counts is only ever what the Refinement tab tells a voter.
    /// </summary>
    public partial class Slice11CastAVoteTest
    {
        private const int TeamThatDoesNotExist = 987654;

        private const int EntriesAMinute = 30;

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly string[] EveryWorkItemInRefinement = [UserActivityTracking, AdvancedReporting, ApiVersioning, ConfigurationManagement, LoadTesting];

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityRefinesAndNobodyHasVoted()
            => await GravityRefinesSixWorkItemsNobodyHasVotedOn();

        private async Task<TeamUnderTest> GivenJonasHasVotedYesOnConfigurationManagement()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Jonas, gravity, ConfigurationManagement, Answer.Yes);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasHasVotedYesThirtyTimesWithinAMinute()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            for (var entry = 0; entry < EntriesAMinute; entry++)
            {
                await HasVoted(Jonas, gravity, ConfigurationManagement, Answer.Yes);
            }

            return gravity;
        }

        private async Task<TeamState> TheTeamAsItStands(TeamUnderTest team)
            => new(JsonSerializer.Serialize(await ReadTheTeamSettings(team)), WorkItemsStoredFor(team));

        // --- When ---

        private async Task<HttpResponseMessage> WhenJonasVotes(TeamUnderTest team, string workItem, Answer answer)
            => await Votes(Jonas, team, workItem, answer);

        private async Task WhenAnaVotesFromHerOwnBrowser(TeamUnderTest team, string workItem, Answer answer)
            => await HasVoted(Ana, team, workItem, answer);

        private async Task WhenTwoBrowsersBothCalledAnaLimaVoteYes(TeamUnderTest team, string workItem)
        {
            await HasVoted(ABrowserOf(AnaLima), team, workItem, Answer.Yes);
            await HasVoted(ABrowserOf(AnaLima), team, workItem, Answer.Yes);
        }

        private async Task<HttpResponseMessage> WhenABrowserVotesHavingDeclared(TeamUnderTest team, string? declaredName)
            => await Votes(new Voter(declaredName, NewVoterKey()), team, ConfigurationManagement, Answer.Yes);

        private async Task<HttpResponseMessage> WhenJonasVotesFromABrowserKeeping(TeamUnderTest team, string? voterKey)
            => await Votes(new Voter(JonasWeber, voterKey), team, ConfigurationManagement, Answer.Yes);

        private async Task<HttpResponseMessage> WhenJonasSendsTheAnswer(TeamUnderTest team, string? answer)
            => await SendsAVote(Jonas, team.TeamId, ConfigurationManagement, new JsonObject
            {
                ["answer"] = answer,
                ["channel"] = nameof(Channel.Web),
                ["voterName"] = JonasWeber,
            });

        private async Task<HttpResponseMessage> WhenJonasVotesSayingItCameFrom(TeamUnderTest team, string? channel)
            => await SendsAVote(Jonas, team.TeamId, ConfigurationManagement, new JsonObject
            {
                ["answer"] = nameof(Answer.Yes),
                ["channel"] = channel,
                ["voterName"] = JonasWeber,
            });

        private async Task<HttpResponseMessage> WhenJonasVotesOnATeamThatDoesNotExist()
            => await SendsAVote(Jonas, TeamThatDoesNotExist, ConfigurationManagement, new JsonObject
            {
                ["answer"] = nameof(Answer.Yes),
                ["channel"] = nameof(Channel.Web),
                ["voterName"] = JonasWeber,
            });

        private async Task<List<string>> WhenJonasVotesAndOpensTheTab(TeamUnderTest team)
        {
            using var vote = await Votes(Jonas, team, ConfigurationManagement, Answer.Yes);
            Assert.That(vote.IsSuccessStatusCode, Is.True, $"Jonas's vote was not accepted: {(int)vote.StatusCode}");
            var voteAnswer = await vote.Content.ReadAsStringAsync();

            using var tab = await OpensTheRefinementTab(Jonas, team.TeamId);
            var tabAnswer = await tab.Content.ReadAsStringAsync();

            return [voteAnswer, tabAnswer];
        }

        private async Task WhenTheAdminTakesBacklogOutOfRefinementAndPutsItBack(TeamUnderTest team)
        {
            await TheAdminHasChosen(team, Analysing, Next);
            var withoutBacklog = await TheTabAsSeenBy(Jonas, team);
            Assert.That(IsListed(withoutBacklog, ConfigurationManagement), Is.False,
                $"{ConfigurationManagement} is still listed after Backlog left refinement, so its coming back proves nothing");

            await TheAdminHasChosen(team, Backlog, Analysing, Next);
        }

        private async Task WhenJonasVotesOnEveryWorkItemInRefinement(TeamUnderTest team)
        {
            foreach (var workItem in EveryWorkItemInRefinement)
            {
                await HasVoted(Jonas, team, workItem, Answer.Yes);
            }
        }

        // --- Then ---

        private async Task ThenTheVoteIsTakenAndTheRowCountsIt(HttpResponseMessage answer, TeamUnderTest team, string workItem, int voteCount, Answer myVote)
        {
            using (answer)
            {
                var body = await answer.Content.ReadAsStringAsync();
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"The vote was not taken: {body}");

                using var document = JsonDocument.Parse(body);
                var theRowItAnswered = VotedRowReading.From(document.RootElement);
                var theRowTheTabShows = RowOf(await TheTabAsSeenBy(Jonas, team), workItem);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(theRowItAnswered, Is.EqualTo(theRowTheTabShows), "the vote answers with the row as the tab now shows it");
                    Assert.That(theRowTheTabShows.VoteCount, Is.EqualTo(voteCount));
                    Assert.That(theRowTheTabShows.MyVote, Is.EqualTo(myVote.ToString()));
                }
            }
        }

        private async Task ThenJonasSeesTheRow(TeamUnderTest team, string workItem, int voteCount, Answer? myVote)
        {
            var row = RowOf(await TheTabAsSeenBy(Jonas, team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.VoteCount, Is.EqualTo(voteCount));
                Assert.That(row.MyVote, Is.EqualTo(myVote?.ToString()));
            }
        }

        private async Task ThenEachVoterSeesTwoVotesAndTheirOwnAnswer(TeamUnderTest team)
        {
            var jonasSees = RowOf(await TheTabAsSeenBy(Jonas, team), ConfigurationManagement);
            var anaSees = RowOf(await TheTabAsSeenBy(Ana, team), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jonasSees.VoteCount, Is.EqualTo(2));
                Assert.That(jonasSees.MyVote, Is.EqualTo(nameof(Answer.Yes)));
                Assert.That(anaSees.VoteCount, Is.EqualTo(2));
                Assert.That(anaSees.MyVote, Is.EqualTo(nameof(Answer.No)));
            }
        }

        /// <summary>
        /// The refusal, and that no listed Work Item counts a vote it did not count before. Opening the tab
        /// as a fresh browser shows every row as anybody sees it.
        /// </summary>
        private async Task ThenTheVoteIsRefusedAndNothingIsCounted(HttpResponseMessage answer, TeamUnderTest team, HttpStatusCode status, string? expectedCode, int alreadyCounted = 0)
        {
            var code = await RefusalCodeOf(answer);
            var tab = await TheTabAsSeenBy(ABrowserOf(PriyaSharma), team);
            var counted = EveryWorkItemInRefinement.Sum(workItem => RowOf(tab, workItem).VoteCount ?? -1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(status));
                if (expectedCode is not null)
                {
                    Assert.That(code, Is.EqualTo(expectedCode));
                }

                Assert.That(counted, Is.EqualTo(alreadyCounted), "a refused vote must not be counted on any Work Item");
            }
        }

        private static void ThenNothingCarriesJonassKey(List<string> whatCameBack)
            => Assert.That(whatCameBack, Has.None.Contains(Jonas.Key!).And.All.Not.Empty);

        private async Task ThenTheTeamIsAsItWas(TeamUnderTest team, TeamState before)
            => Assert.That(await TheTeamAsItStands(team), Is.EqualTo(before));

        private async Task ThenJonasIsToldToSlowDownAndHisYesStillCounts(HttpResponseMessage thirtyFirst, TeamUnderTest team)
        {
            var row = RowOf(await TheTabAsSeenBy(Jonas, team), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(thirtyFirst.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
                Assert.That(row.MyVote, Is.EqualTo(nameof(Answer.Yes)));
                Assert.That(row.VoteCount, Is.EqualTo(1));
            }
        }

        private sealed record TeamState(string Settings, int WorkItems);
    }
}
