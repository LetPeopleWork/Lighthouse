using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for comments, questions and the log. Every Given says which day it is: Wednesday
    /// 7 October 2026, nine in the morning. When an entry was recorded is checked once, against that day, and
    /// left out of the comparison of what each entry says.
    /// </summary>
    public partial class Slice12CommentsTest
    {
        private const string BillingExport = RefinementPortGivens.BillingExport;

        private const string AdvancedReporting = RefinementPortGivens.AdvancedReporting;

        private const string ApiVersioning = RefinementPortGivens.ApiVersioning;

        private const string ConfigurationManagement = RefinementPortGivens.ConfigurationManagement;

        private const string JonasWeber = "Jonas Weber";

        private const string AnaLima = "Ana Lima";

        private const string MoOkafor = "Mo Okafor";

        private const string PriyaSharma = "Priya Sharma";

        private const string OnlyIfThePdfExportMovesOut = "only if the PDF export moves to its own Work Item";

        private const string NeedsTheDesignFirst = "needs the design for the export first";

        private const string WhichApiVersion = "Which API version?";

        private const string MarkupAndQuotes = "<b>bold</b> & \"quotes\" 'too'";

        /// <summary>Every entry in these scenarios is recorded on the instance's today.</summary>
        private const string RecordedToday = "2026-10-07";

        private static readonly bool[] OnlyTheThirdEntryIsOpen = [false, false, true];

        private static readonly PortVoter Jonas = ABrowserOf(JonasWeber);

        private static readonly PortVoter Ana = ABrowserOf(AnaLima);

        private static readonly PortVoter Mo = ABrowserOf(MoOkafor);

        // --- Given ---

        private int GivenGravityRefinesAndNobodyHasVoted()
        {
            TodayIs(2026, 10, 7);
            return Givens.GravityRefinesFiveWorkItems();
        }

        private int GivenJonasVotedYesOnConfigurationManagement()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();
            HasVoted(Jonas, gravity, ConfigurationManagement, SizingAnswer.Yes);
            return gravity;
        }

        private int GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();
            HasVoted(Jonas, gravity, AdvancedReporting, SizingAnswer.Yes);
            HasVoted(Ana, gravity, AdvancedReporting, SizingAnswer.No);
            HasCommented(Mo, gravity, AdvancedReporting, WhichApiVersion);
            return gravity;
        }

        private int GivenJonasAskedWhichApiVersionOnApiVersioning()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();
            HasCommented(Jonas, gravity, ApiVersioning, WhichApiVersion);
            return gravity;
        }

        // --- When ---

        private void WhenAnaSaysYesBut(int team, string workItem, string condition)
            => HasVoted(Ana, team, workItem, SizingAnswer.YesBut, condition);

        private void WhenAnaVotesWithAComment(int team, SizingAnswer answer, string comment)
            => HasVoted(Ana, team, AdvancedReporting, answer, comment);

        private void WhenAnaVotesWithoutAComment(int team)
            => HasVoted(Ana, team, AdvancedReporting, SizingAnswer.Yes);

        private void WhenJonasChangesHisMindTo(int team, SizingAnswer answer)
            => HasVoted(Jonas, team, ConfigurationManagement, answer);

        private SizingLogDto WhenAnaOpensTheLogOf(int team, string workItem)
            => TheLogAsSeenBy(Ana, team, workItem);

        private void WhenAnaChangesHerMindTo(int team, string workItem, SizingAnswer answer)
            => HasVoted(Ana, team, workItem, answer);

        private (VotedRowReading Row, SizingLogDto Log) WhenPriyaWhoNeverVotedLooksAt(int team, string workItem)
        {
            var priya = ABrowserOf(PriyaSharma);
            var row = RowOf(TheTabAsSeenBy(priya, team), workItem);
            var log = TheLogAsSeenBy(priya, team, workItem);
            return (row, log);
        }

        private void WhenJonasAsks(int team, string workItem, string question)
            => HasCommented(Jonas, team, workItem, question);

        private SizingOutcome WhenJonasTriesToAsk(int team, string workItem, string question)
            => TriesToComment(Jonas, team, workItem, question);

        private SizingOutcome WhenJonasTriesToVoteYesWithTheComment(int team, string workItem, string comment)
            => TriesToVote(Jonas, team, workItem, SizingAnswer.Yes, comment);

        private void WhenTheyVote(PortVoter voter, int team, string workItem, SizingAnswer answer)
            => HasVoted(voter, team, workItem, answer);

        private void WhenAnaComments(int team, string workItem, string comment)
            => HasCommented(Ana, team, workItem, comment);

        private void WhenJonasVotesYesWithTheComment(int team, string comment)
            => HasVoted(Jonas, team, ConfigurationManagement, SizingAnswer.Yes, comment);

        // --- Then ---

        private static void ThenTheLogReads(SizingLogDto log, params LogEntryReading[] expected)
        {
            var entries = EntriesIn(log);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries.Select(entry => entry with { RecordedAt = null }), Is.EqualTo(expected),
                    "the log must say who said what, from where, and whose entry is the reader's own, oldest first");
                Assert.That(entries.Select(entry => entry.RecordedAt), Has.All.StartsWith(RecordedToday), "every entry says when it was recorded");
            }
        }

        private static void ThenOnlyMosQuestionIsMarkedOpen(SizingLogDto log)
            => Assert.That(log.Entries.Select(entry => entry.IsOpenQuestion), Is.EqualTo(OnlyTheThirdEntryIsOpen),
                "the server must mark the one question whose asker has not voted, so every reader sees the same one");

        private void ThenPriyaReadsTheVoters(int team, string workItem, VotersReading expected)
        {
            var reader = ABrowserOf(PriyaSharma);
            var log = TheLogAsSeenBy(reader, team, workItem);
            var row = RowOf(TheTabAsSeenBy(reader, team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(log.Voters.Yes, Is.EqualTo(expected.Yes), "the names under Yes, in the order the current votes were cast");
                Assert.That(log.Voters.YesBut, Is.EqualTo(expected.YesBut));
                Assert.That(log.Voters.No, Is.EqualTo(expected.No));
                Assert.That(new SplitReading(log.Voters.Yes.Count, log.Voters.YesBut.Count, log.Voters.No.Count), Is.EqualTo(row.Split),
                    "the names under each answer and the row's split agree");
            }
        }

        private static void ThenPriyaReadsTheSplitAndTheLog(VotedRowReading row, SizingLogDto log, SplitReading split, params LogEntryReading[] expected)
        {
            var entries = EntriesIn(log).Select(entry => entry with { RecordedAt = null });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Split, Is.EqualTo(split), "the split is there for somebody who never voted");
                Assert.That(row.MyVote, Is.Null);
                Assert.That(entries, Is.EqualTo(expected), "every comment is there for somebody who never voted");
            }
        }

        private void ThenJonasReadsTheAnswersInOrder(int team, string workItem, params string[] answers)
            => Assert.That(EntriesIn(TheLogAsSeenBy(Jonas, team, workItem)).Select(entry => entry.Answer), Is.EqualTo(answers),
                "a changed mind adds an entry and never overwrites the earlier one");

        private void ThenTheRowReads(int team, string workItem, int voteCount, bool hasOpenQuestion, bool hasComments)
        {
            var row = RowOf(TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.VoteCount, Is.EqualTo(voteCount));
                Assert.That(row.HasOpenQuestion, Is.EqualTo(hasOpenQuestion));
                Assert.That(row.HasComments, Is.EqualTo(hasComments));
            }
        }

        private void ThenAnaStillCountsWith(int team, string workItem, SizingAnswer answer)
        {
            var row = RowOf(TheTabAsSeenBy(Ana, team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.MyVote, Is.EqualTo(answer.ToString()));
                Assert.That(row.VoteCount, Is.EqualTo(1));
                Assert.That(row.HasOpenQuestion, Is.False);
                Assert.That(row.HasComments, Is.True);
            }
        }

        private void ThenTheReadinessOfTheRowIs(int team, string workItem, string readiness, int missingVotes)
        {
            var row = RowOf(TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Readiness, Is.EqualTo(readiness));
                Assert.That(row.MissingVotes, Is.EqualTo(missingVotes));
            }
        }

        private void ThenTheCommentIsRefusedAndTheRowIsUntouched(SizingOutcome refused, int team, string workItem, SizingOutcome expectedOutcome)
        {
            var row = RowOf(TheTabAsSeenBy(ABrowserOf(PriyaSharma), team), workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused, Is.EqualTo(expectedOutcome), "the sizing log must refuse for exactly this reason");
                Assert.That(row.VoteCount, Is.Zero, "a refused vote must not be counted");
                Assert.That(row.HasComments, Is.False, "a refused comment must leave no trace on the row");
                Assert.That(row.HasOpenQuestion, Is.False);
            }
        }

        private void ThenJonasReadsHisComment(int team, string comment)
        {
            var entries = EntriesIn(TheLogAsSeenBy(Jonas, team, ConfigurationManagement));

            List<string?> onlyHisComment = [comment];

            Assert.That(entries.Select(entry => entry.Comment), Is.EqualTo(onlyHisComment), "a comment is kept whole and handed back exactly as written");
        }

        // --- Reading what the browser reads ---

        private static List<LogEntryReading> EntriesIn(SizingLogDto log)
            => [.. log.Entries.Select(entry => new LogEntryReading(
                entry.Kind.ToString(), entry.Answer?.ToString(), entry.Comment, entry.VoterName, entry.Channel.ToString(), entry.RecordedAt, entry.IsMine))];

        private static VotedRowReading RowOf(RefinementViewDto tab, string referenceId)
        {
            var row = tab.WorkItems.SingleOrDefault(candidate => candidate.ReferenceId == referenceId);
            Assert.That(row, Is.Not.Null, $"{referenceId} is not listed on the Refinement tab");

            return new VotedRowReading(
                row!.VoteCount,
                row.MyVote?.ToString(),
                new SplitReading(row.Split.Yes, row.Split.YesBut, row.Split.No),
                row.Readiness.ToString(),
                row.MissingVotes,
                row.HasComments,
                row.HasOpenQuestion);
        }

        private sealed record SplitReading(int Yes, int YesBut, int No);

        private sealed record VotersReading(string[] Yes, string[] YesBut, string[] No);

        private sealed record VotedRowReading(
            int VoteCount,
            string? MyVote,
            SplitReading Split,
            string Readiness,
            int? MissingVotes,
            bool HasComments,
            bool HasOpenQuestion);

        /// <summary>One entry of a Work Item's log, oldest first.</summary>
        private sealed record LogEntryReading(
            string Kind,
            string? Answer,
            string? Comment,
            string VoterName,
            string Channel,
            string? RecordedAt,
            bool IsMine);
    }
}
