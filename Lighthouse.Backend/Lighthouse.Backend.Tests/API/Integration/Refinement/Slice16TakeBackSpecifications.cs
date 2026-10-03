using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for taking a vote back. Ana reads the log because she keeps her vote throughout,
    /// so the log stays open to her.
    /// </summary>
    public partial class Slice16TakeBackTest
    {
        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        // --- Given ---

        private async Task<TeamUnderTest> GivenOnlyAnaSaidYesOnConfigurationManagement()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await HasVoted(Ana, gravity, ConfigurationManagement, Answer.Yes);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasAndAnaSaidYesOnConfigurationManagement()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await HasVoted(Jonas, gravity, ConfigurationManagement, Answer.Yes);
            await HasVoted(Ana, gravity, ConfigurationManagement, Answer.Yes);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasMoAndAnaSaidYesSoConfigurationManagementIsReady()
        {
            var gravity = await GivenJonasAndAnaSaidYesOnConfigurationManagement();
            await HasVoted(Mo, gravity, ConfigurationManagement, Answer.Yes);
            return gravity;
        }

        // --- When ---

        private async Task WhenJonasTakesBackHisVote(TeamUnderTest team)
            => await TheVoteIsTakenBack(Jonas, team);

        private async Task WhenMoTakesBackHisVote(TeamUnderTest team)
            => await TheVoteIsTakenBack(Mo, team);

        private async Task TheVoteIsTakenBack(Voter voter, TeamUnderTest team)
        {
            using var takeBack = await TakesBackTheirVote(voter, team, ConfigurationManagement);
            Assert.That(takeBack.IsSuccessStatusCode, Is.True,
                $"{voter.Name}'s vote was not taken back: {(int)takeBack.StatusCode} {await takeBack.Content.ReadAsStringAsync()}");
        }

        // --- Then ---

        private async Task ThenJonasNoLongerCountsAndAnaReadsInTheLog(TeamUnderTest team, params string[] kinds)
        {
            var jonasSees = RowOf(await TheTabAsSeenBy(Jonas, team), ConfigurationManagement);
            var entries = EntriesIn(await TheLogAsSeenBy(Ana, team, ConfigurationManagement));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jonasSees.VoteCount, Is.EqualTo(1));
                Assert.That(jonasSees.MyVote, Is.Null);
                Assert.That(entries.Select(entry => entry.Kind), Is.EqualTo(kinds));
                Assert.That(entries[^1].VoterName, Is.EqualTo(JonasWeber), "the take-back is recorded under the voter who took it back");
            }
        }

        private async Task ThenItIsAnsweredWithoutErrorAndAnaReadsInTheLog(HttpResponseMessage takeBack, TeamUnderTest team, params string[] kinds)
        {
            var anaSees = RowOf(await TheTabAsSeenBy(Ana, team), ConfigurationManagement);
            var entries = EntriesIn(await TheLogAsSeenBy(Ana, team, ConfigurationManagement));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(takeBack.IsSuccessStatusCode, Is.True, $"{(int)takeBack.StatusCode}");
                Assert.That(entries.Select(entry => entry.Kind), Is.EqualTo(kinds));
                Assert.That(anaSees.MyVote, Is.EqualTo(nameof(Answer.Yes)), "Ana's vote still counts as hers");
            }
        }

        private async Task ThenItIsRefusedAndAnasVoteStillCounts(HttpResponseMessage refused, TeamUnderTest team, HttpStatusCode status, string code)
        {
            var refusal = await RefusalCodeOf(refused);
            var anaSees = RowOf(await TheTabAsSeenBy(Ana, team), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(status));
                Assert.That(refusal, Is.EqualTo(code));
                Assert.That(anaSees.VoteCount, Is.EqualTo(1));
                Assert.That(anaSees.MyVote, Is.EqualTo(nameof(Answer.Yes)));
            }
        }

        private async Task ThenConfigurationManagementNeeds(TeamUnderTest team, string readiness, int? missingVotes)
        {
            var row = RowOf(await TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Readiness, Is.EqualTo(readiness));
                Assert.That(row.MissingVotes, Is.EqualTo(missingVotes));
            }
        }

        private async Task ThenJonasSeesNoSplitAndAClosedLog(TeamUnderTest team)
        {
            var row = RowOf(await TheTabAsSeenBy(Jonas, team), ConfigurationManagement);
            var log = await TheLogAsSeenBy(Jonas, team, ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Split, Is.Null);
                Assert.That(LogIsHidden(log), Is.True, $"Log: {log}");
            }
        }
    }
}
