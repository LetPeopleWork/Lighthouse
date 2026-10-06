using System.Net;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for votes under an account. Jonas holds Viewer on Team Gravity; Lena holds no
    /// role on it.
    /// </summary>
    public partial class Slice15VotesWithAnAccountTest
    {
        // --- Given ---

        private async Task<(TeamUnderTest Team, Voter Jonas)> GivenJonasReadsTeamGravity()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            return (gravity, ASignedInReaderOf(gravity, "jonas", JonasWeber));
        }

        private async Task<(TeamUnderTest Team, Voter Jonas)> GivenJonasReadsTeamGravityAndHasVotedYes()
        {
            var (gravity, jonas) = await GivenJonasReadsTeamGravity();
            await HasVoted(jonas, gravity, ConfigurationManagement, Answer.Yes);
            return (gravity, jonas);
        }

        // --- When ---

        private async Task WhenTheyVote(Voter voter, TeamUnderTest team, string workItem, Answer answer)
            => await HasVoted(voter, team, workItem, answer);

        private async Task WhenTheyVoteClaimingToBe(Voter voter, TeamUnderTest team, string workItem, string claimedName)
        {
            using var vote = await SendsAVote(voter, team.TeamId, workItem, new JsonObject
            {
                ["answer"] = nameof(Answer.Yes),
                ["channel"] = nameof(Channel.Web),
                ["voterName"] = claimedName,
            });

            Assert.That(vote.StatusCode, Is.EqualTo(HttpStatusCode.OK), "a signed-in vote is taken whatever name it carries");
        }

        // --- Then ---

        private async Task ThenTheLogShowsTheVoteUnder(Voter voter, TeamUnderTest team, string workItem, string accountName)
        {
            var entries = EntriesIn(await TheLogAsSeenBy(voter, team, workItem));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries, Has.Count.EqualTo(1));
                Assert.That(entries.Select(entry => entry.VoterName), Has.All.EqualTo(accountName));
                Assert.That(entries.Select(entry => entry.IsMine), Has.All.True);
            }
        }

        private async Task ThenBothBrowsersSeeOneVoteAndItIsHis(TeamUnderTest team, Voter laptop, Voter phone, Answer latest)
        {
            var onTheLaptop = RowOf(await TheTabAsSeenBy(laptop, team), ConfigurationManagement);
            var onThePhone = RowOf(await TheTabAsSeenBy(phone, team), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(onTheLaptop.VoteCount, Is.EqualTo(1));
                Assert.That(onTheLaptop.MyVote, Is.EqualTo(latest.ToString()));
                Assert.That(onThePhone, Is.EqualTo(onTheLaptop));
            }
        }

        private async Task ThenTheRowCounts(TeamUnderTest team, string workItem, int voteCount)
        {
            var row = RowOf(await TheTabAsSeenBy(ASignedInAdminOf(team, "priya", PriyaSharma), team), workItem);

            Assert.That(row.VoteCount, Is.EqualTo(voteCount));
        }

        private async Task ThenBothAreNotFoundAndOnlyJonassVoteCounts(HttpResponseMessage tab, HttpResponseMessage vote, TeamUnderTest team, Voter jonas)
        {
            var row = RowOf(await TheTabAsSeenBy(jonas, team), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tab.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(vote.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "a vote on a Team you cannot read is answered as if the Team did not exist");
                Assert.That(row.VoteCount, Is.EqualTo(1));
            }
        }

        private async Task ThenBothAreNotFoundAndTheLogHoldsOnlyJonassVote(HttpResponseMessage log, HttpResponseMessage comment, TeamUnderTest team, Voter jonas)
        {
            var entries = EntriesIn(await TheLogAsSeenBy(jonas, team, ConfigurationManagement));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(log.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(comment.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(entries.Select(entry => entry.VoterName).ToList(), Has.Count.EqualTo(1).And.All.EqualTo(JonasWeber));
            }
        }
    }
}
