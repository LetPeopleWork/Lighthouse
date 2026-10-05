using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for comments, questions and the log. When an entry was recorded is checked once,
    /// against the instance's clock, and left out of the comparison of what each entry says.
    /// </summary>
    public partial class Slice12CommentsTest
    {
        private const string OnlyIfThePdfExportMovesOut = "only if the PDF export moves to its own Work Item";

        private const string NeedsTheDesignFirst = "needs the design for the export first";

        private const string WhichApiVersion = "Which API version?";

        private const string MarkupAndQuotes = "<b>bold</b> & \"quotes\" 'too'";

        private const string CommentRequired = "comment-required";

        private const string CommentTooLong = "comment-too-long";

        /// <summary>Every entry in these scenarios is recorded on the instance's today.</summary>
        private static readonly string RecordedToday = Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityRefinesAndNobodyHasVoted()
            => await GravityRefinesSixWorkItemsNobodyHasVotedOn();

        private async Task<TeamUnderTest> GivenJonasVotedYesOnConfigurationManagement()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Jonas, gravity, ConfigurationManagement, Answer.Yes);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Jonas, gravity, AdvancedReporting, Answer.Yes);
            await HasVoted(Ana, gravity, AdvancedReporting, Answer.No);
            await HasCommented(Mo, gravity, AdvancedReporting, WhichApiVersion);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenJonasAskedWhichApiVersionOnApiVersioning()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasCommented(Jonas, gravity, ApiVersioning, WhichApiVersion);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenAnaVotedAndCanOpenTheLogOfAdvancedReporting()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Ana, gravity, AdvancedReporting, Answer.Yes);

            using var opened = await OpensTheLog(Ana, gravity, AdvancedReporting);
            Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "the log of a listed Work Item does not open, so a refusal for another one proves nothing");

            return gravity;
        }

        // --- When ---

        private async Task WhenAnaSaysYesBut(TeamUnderTest team, string workItem, string condition)
            => await HasVoted(Ana, team, workItem, Answer.YesBut, condition);

        private async Task WhenAnaVotesWithAComment(TeamUnderTest team, Answer answer, string comment)
            => await HasVoted(Ana, team, AdvancedReporting, answer, comment);

        private async Task WhenAnaVotesWithoutAComment(TeamUnderTest team)
            => await HasVoted(Ana, team, AdvancedReporting, Answer.Yes);

        private async Task WhenJonasChangesHisMindTo(TeamUnderTest team, Answer answer)
            => await HasVoted(Jonas, team, ConfigurationManagement, answer);

        private async Task<JsonElement> WhenAnaOpensTheLogOf(TeamUnderTest team, string workItem)
            => await TheLogAsSeenBy(Ana, team, workItem);

        private async Task<(VotedRowReading Row, JsonElement Log)> WhenPriyaWhoNeverVotedLooksAt(TeamUnderTest team, string workItem)
        {
            var priya = ABrowserOf(PriyaSharma);
            var row = RowOf(await TheTabAsSeenBy(priya, team), workItem);
            var log = await TheLogAsSeenBy(priya, team, workItem);
            return (row, log);
        }

        private async Task WhenJonasAsks(TeamUnderTest team, string workItem, string question)
            => await HasCommented(Jonas, team, workItem, question);

        private async Task<HttpResponseMessage> WhenJonasTriesToAsk(TeamUnderTest team, string workItem, string question)
            => await Comments(Jonas, team, workItem, question);

        private async Task<HttpResponseMessage> WhenJonasTriesToVoteYesWithTheComment(TeamUnderTest team, string workItem, string comment)
            => await Votes(Jonas, team, workItem, Answer.Yes, comment);

        private async Task WhenTheyVote(Voter voter, TeamUnderTest team, string workItem, Answer answer)
            => await HasVoted(voter, team, workItem, answer);

        private async Task WhenAnaComments(TeamUnderTest team, string workItem, string comment)
            => await HasCommented(Ana, team, workItem, comment);

        private async Task WhenJonasVotesYesWithTheComment(TeamUnderTest team, string comment)
            => await HasVoted(Jonas, team, ConfigurationManagement, Answer.Yes, comment);

        // --- Then ---

        private async Task ThenAnaReadsInTheLog(TeamUnderTest team, string workItem, LogEntryReading expected)
            => ThenTheLogReads(await TheLogAsSeenBy(Ana, team, workItem), expected);

        private static void ThenTheLogReads(JsonElement log, params LogEntryReading[] expected)
        {
            var entries = EntriesIn(log);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries.Select(entry => entry with { RecordedAt = null }), Is.EqualTo(expected), $"Log: {log}");
                Assert.That(entries.Select(entry => entry.RecordedAt), Has.All.StartsWith(RecordedToday), "every entry says when it was recorded");
            }
        }

        private static void ThenPriyaReadsTheSplitAndTheLog(VotedRowReading row, JsonElement log, SplitReading split, params LogEntryReading[] expected)
        {
            var entries = EntriesIn(log).Select(entry => entry with { RecordedAt = null });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Split, Is.EqualTo(split), "the split is there for somebody who never voted");
                Assert.That(row.MyVote, Is.Null);
                Assert.That(entries, Is.EqualTo(expected), $"every comment is there for somebody who never voted. Log: {log}");
            }
        }

        private async Task ThenJonasReadsTheAnswersInOrder(TeamUnderTest team, string workItem, params string[] answers)
        {
            var entries = EntriesIn(await TheLogAsSeenBy(Jonas, team, workItem));

            Assert.That(entries.Select(entry => entry.Answer), Is.EqualTo(answers));
        }

        private async Task ThenTheRowReads(TeamUnderTest team, string workItem, int voteCount, bool hasOpenQuestion, bool hasComments)
        {
            var row = RowOf(await TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.VoteCount, Is.EqualTo(voteCount));
                Assert.That(row.HasOpenQuestion, Is.EqualTo(hasOpenQuestion));
                Assert.That(row.HasComments, Is.EqualTo(hasComments));
            }
        }

        private async Task ThenAnaStillCountsWith(TeamUnderTest team, string workItem, Answer answer)
        {
            var row = RowOf(await TheTabAsSeenBy(Ana, team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.MyVote, Is.EqualTo(answer.ToString()));
                Assert.That(row.VoteCount, Is.EqualTo(1));
                Assert.That(row.HasOpenQuestion, Is.False);
                Assert.That(row.HasComments, Is.True);
            }
        }

        private async Task ThenTheReadinessOfTheRowIs(TeamUnderTest team, string workItem, string readiness, int missingVotes)
        {
            var row = RowOf(await TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Readiness, Is.EqualTo(readiness));
                Assert.That(row.MissingVotes, Is.EqualTo(missingVotes));
            }
        }

        private async Task ThenTheCommentIsRefusedAndTheRowIsUntouched(HttpResponseMessage refused, TeamUnderTest team, string workItem, HttpStatusCode status, string? expectedCode)
        {
            var code = await RefusalCodeOf(refused);
            var row = RowOf(await TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(status));
                if (expectedCode is not null)
                {
                    Assert.That(code, Is.EqualTo(expectedCode));
                }

                Assert.That(row.VoteCount, Is.Zero, "a refused vote must not be counted");
                Assert.That(row.HasComments, Is.False, "a refused comment must leave no trace on the row");
                Assert.That(row.HasOpenQuestion, Is.False);
            }
        }

        private async Task ThenJonasReadsHisComment(TeamUnderTest team, string comment)
        {
            var entries = EntriesIn(await TheLogAsSeenBy(Jonas, team, ConfigurationManagement));

            List<string?> onlyHisComment = [comment];

            Assert.That(entries.Select(entry => entry.Comment), Is.EqualTo(onlyHisComment));
        }
    }
}
