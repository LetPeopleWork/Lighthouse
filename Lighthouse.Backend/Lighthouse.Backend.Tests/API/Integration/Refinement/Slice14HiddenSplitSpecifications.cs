using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the hidden split. Jonas is the one who has not voted yet; Ana, Mo and Priya
    /// have.
    /// </summary>
    public partial class Slice14HiddenSplitTest
    {
        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        private static readonly Voter Priya = ABrowserOf(PriyaSharma);

        // --- Given ---

        private async Task<TeamUnderTest> GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await HasVoted(Ana, gravity, AdvancedReporting, Answer.Yes, "fits if we skip the PDF export");
            await HasVoted(Mo, gravity, AdvancedReporting, Answer.Yes);
            await HasVoted(Priya, gravity, AdvancedReporting, Answer.No);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenAnaMoAndPriyaSaidYesOnConfigurationManagement()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await HasVoted(Ana, gravity, ConfigurationManagement, Answer.Yes);
            await HasVoted(Mo, gravity, ConfigurationManagement, Answer.Yes);
            await HasVoted(Priya, gravity, ConfigurationManagement, Answer.Yes);
            return gravity;
        }

        // --- When ---

        private async Task<VotedRowReading> WhenJonasLooksAt(TeamUnderTest team, string workItem)
            => RowOf(await TheTabAsSeenBy(Jonas, team), workItem);

        private async Task<VotedRowReading> WhenAnaLooksAt(TeamUnderTest team, string workItem)
            => RowOf(await TheTabAsSeenBy(Ana, team), workItem);

        private async Task<JsonElement> WhenJonasOpensTheLogOf(TeamUnderTest team, string workItem)
            => await TheLogAsSeenBy(Jonas, team, workItem);

        private async Task WhenJonasVotes(TeamUnderTest team, string workItem, Answer answer)
            => await HasVoted(Jonas, team, workItem, answer);

        private async Task WhenJonasAsks(TeamUnderTest team, string workItem, string question)
            => await HasCommented(Jonas, team, workItem, question);

        // --- Then ---

        private static void ThenHeSeesOnlyTheCountAndTheReadiness(VotedRowReading row, int voteCount, string readiness)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.VoteCount, Is.EqualTo(voteCount));
                Assert.That(row.Readiness, Is.EqualTo(readiness), "readiness is shown whether or not you voted");
                Assert.That(row.Split, Is.Null, "the split is not sent to somebody who has not voted");
                Assert.That(row.MyVote, Is.Null);
            }
        }

        private static void ThenHeSeesTheSplit(VotedRowReading row, SplitReading split)
            => Assert.That(row.Split, Is.EqualTo(split));

        private static void ThenTheLogIsClosedSaying(JsonElement log, int voteCount)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(LogIsHidden(log), Is.True, $"Log: {log}");
                Assert.That(VoteCountOfAHiddenLog(log), Is.EqualTo(voteCount));
                Assert.That(EntriesIn(log), Is.Empty, "no entry, and so no comment, is sent before you vote");
            }
        }

        private static void ThenTheLogIsOpenWithEntriesFrom(JsonElement log, params string[] voters)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(LogIsHidden(log), Is.False, $"Log: {log}");
                Assert.That(EntriesIn(log).Select(entry => entry.VoterName), Is.EqualTo(voters));
            }
        }

        private async Task ThenJonasStillSeesNeitherTheSplitNorTheLog(TeamUnderTest team, string workItem)
        {
            var row = await WhenJonasLooksAt(team, workItem);
            var log = await WhenJonasOpensTheLogOf(team, workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Split, Is.Null);
                Assert.That(LogIsHidden(log), Is.True, $"Log: {log}");
            }
        }

        private static void ThenOnlyAnaSeesTheSplit(VotedRowReading anaSees, VotedRowReading jonasSees)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(anaSees.Split, Is.EqualTo(new SplitReading(Yes: 2, YesBut: 0, No: 1)));
                Assert.That(jonasSees.Split, Is.Null);
            }
        }
    }
}
