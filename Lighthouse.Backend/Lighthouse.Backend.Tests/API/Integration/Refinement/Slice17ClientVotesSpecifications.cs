using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the client half. A client is a voter with a key of its own, exactly as a
    /// browser is; what makes it a client is the channel it declares.
    /// </summary>
    public partial class Slice17ClientVotesTest
    {
        private const string OnlyIfThePdfExportMovesOut = "only if the PDF export moves to its own Work Item";

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        private static readonly Voter AnasBrowser = ABrowserOf(AnaLima);

        private static readonly Voter AnasClient = AClientOf(AnaLima);

        private static readonly Voter PriyasClient = AClientOf(PriyaSharma);

        // --- Given ---

        /// <summary>
        /// A veto of one No is set. GR-054 has three Yes votes and one No, so it needs discussion; GR-073 has
        /// three Yes votes and is Ready. Priya has voted on neither.
        /// </summary>
        private async Task<TeamUnderTest> GivenApiVersioningNeedsDiscussionAndConfigurationManagementIsReady()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await TheAdminHasSetReadiness(gravity, minYes: 3, minVoters: 3, vetoThreshold: 1);

            foreach (var voter in new[] { Jonas, Mo, AnasBrowser })
            {
                await HasVoted(voter, gravity, ApiVersioning, Answer.Yes);
                await HasVoted(voter, gravity, ConfigurationManagement, Answer.Yes);
            }

            await HasVoted(ABrowserOf("Lena Brandt"), gravity, ApiVersioning, Answer.No);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenAnasCommandLineVotedYesAndJonasVotedYesOnAdvancedReporting()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await HasVoted(AnasClient, gravity, AdvancedReporting, Answer.Yes, channel: Channel.Cli);
            await HasVoted(Jonas, gravity, AdvancedReporting, Answer.Yes);
            return gravity;
        }

        // --- When ---

        private async Task<JsonElement> WhenPriyasClientReadsTheRefinement(TeamUnderTest team)
            => await TheTabAsSeenBy(PriyasClient, team);

        private async Task WhenAnasCommandLineSays(TeamUnderTest team, string workItem, Answer answer, string comment)
            => await HasVoted(AnasClient, team, workItem, answer, comment, Channel.Cli);

        // --- Then ---

        private static void ThenTheClientReads(JsonElement tab, params (string WorkItem, string Readiness)[] expected)
            => Assert.That(expected.Select(row => (row.WorkItem, RowOf(tab, row.WorkItem).Readiness)), Is.EqualTo(expected.Select(row => (row.WorkItem, (string?)row.Readiness))));

        private static void ThenTheClientSeesCountsButNoSplitOn(JsonElement tab, string workItem, int voteCount)
        {
            var row = RowOf(tab, workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.VoteCount, Is.EqualTo(voteCount));
                Assert.That(row.Split, Is.Null);
                Assert.That(row.MyVote, Is.Null);
            }
        }

        private async Task ThenAnaReadsHerEntryMarkedAsFrom(TeamUnderTest team, string workItem, string channel, string answer, string? comment)
        {
            var entries = EntriesIn(await TheLogAsSeenBy(AnasClient, team, workItem));
            List<LogEntryReading> expected = [new LogEntryReading("Vote", answer, comment, AnaLima, channel, null, IsMine: true)];

            Assert.That(entries.Select(entry => entry with { RecordedAt = null }), Is.EqualTo(expected));
        }

        private async Task ThenTheClientIsRefusedAndNothingCounts(HttpResponseMessage refused, TeamUnderTest team, HttpStatusCode status, string code)
        {
            var refusal = await RefusalCodeOf(refused);
            var row = RowOf(await TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), AdvancedReporting);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(status));
                Assert.That(refusal, Is.EqualTo(code));
                Assert.That(row.VoteCount, Is.Zero);
            }
        }

        private static void ThenTheRowHasAnOpenQuestion(JsonElement tab, string workItem)
            => Assert.That(RowOf(tab, workItem).HasOpenQuestion, Is.True);

        private async Task ThenJonasReadsTheLogKinds(HttpResponseMessage takeBack, TeamUnderTest team, params string[] kinds)
        {
            var entries = EntriesIn(await TheLogAsSeenBy(Jonas, team, AdvancedReporting));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(takeBack.IsSuccessStatusCode, Is.True, $"{(int)takeBack.StatusCode}");
                Assert.That(entries.Select(entry => entry.Kind), Is.EqualTo(kinds));
            }
        }
    }
}
