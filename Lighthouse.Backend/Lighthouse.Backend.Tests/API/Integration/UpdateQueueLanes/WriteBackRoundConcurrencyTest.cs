using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// DISTILL specifications (ADO #5877), slice 01's precursor commit: a refresh round survives two
    /// executions touching it at once.
    ///
    /// These are not acceptance scenarios and they do not go through a driving port, deliberately. A
    /// round's staging area is not something any port exposes — what a port can show is that the round
    /// spoke once and that the tracker was written to once, which slice 01's scenarios assert. What
    /// cannot be reached from outside is whether two stagings landing at the same instant both survive,
    /// and that is the whole subject of the precursor commit: today <c>Stage</c>, <c>TakeStaged</c>,
    /// <c>ReportRefresh</c>, <c>ReportForecast</c> and <c>TakeSummary</c> mutate plain dictionaries with
    /// no synchronisation, and the only thing making that safe is that one reader means one execution
    /// is ever inside them.
    ///
    /// The guard is behavioural rather than structural. "A lock exists" is not the promise; "nothing
    /// staged is lost and nothing is sent twice" is, and a future change that keeps the lock and breaks
    /// the promise has to fail.
    ///
    /// <c>[Ignore]</c>d at hand-off like everything else in this story. DELIVER unskips these first —
    /// round safety lands before the second consumer exists, not after.
    /// </summary>
    [TestFixture]
    [Category("story-5877-update-queue-lanes")]
    [Category("slice-01")]
    public class WriteBackRoundConcurrencyTest
    {
        private const string AFieldEveryUpdateWrites = "customfield_10001";

        /// <summary>
        /// Enough stagings from each side that an unsynchronised dictionary has to lose one, and few
        /// enough that a failure names a number a reader can hold in their head.
        /// </summary>
        private const int UpdatesFromEachExecution = 500;

        [Test]
        public async Task Everything_two_executions_stage_at_once_is_still_there_to_be_written()
        {
            var round = new WriteBackRound();
            var connection = AConnection();

            await Task.WhenAll(
                StageFrom(round, connection, "refresh"),
                StageFrom(round, connection, "forecast"));

            var staged = round.TakeStaged();
            var written = staged.SelectMany(perConnection => perConnection.Updates).ToList();

            Assert.That(written, Has.Count.EqualTo(UpdatesFromEachExecution * 2),
                "A portfolio refresh and the forecast it triggers are one round, and with a lane each they stage "
                + "into it at the same time. Anything dropped here is a field the operator asked to be written "
                + "back to their tracker that silently never was.");
        }

        [Test]
        public void The_round_hands_over_what_it_is_holding_to_exactly_one_caller()
        {
            var round = new WriteBackRound();
            var connection = AConnection();

            round.Stage(connection, [AnUpdate("refresh", 1)]);

            var first = round.TakeStaged();
            var second = round.TakeStaged();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.Not.Empty,
                    "The first caller out owes the round its write, and it can only make it with what the round "
                    + "was holding.");
                Assert.That(second, Is.Empty,
                    "Draining before the write is what makes the write terminal. A second caller that got the "
                    + "same set would send every field to the tracker twice.");
            }
        }

        [Test]
        public void Only_one_execution_gets_to_speak_for_the_round()
        {
            var round = new WriteBackRound();

            round.ReportRefresh(new RefreshRoundSummary("Portfolio", "A portfolio", 10, true));

            var first = round.TakeSummary();
            var second = round.TakeSummary();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.Not.Null,
                    "A round that resolved something has a line to write about it.");
                Assert.That(second, Is.Null,
                    "Every execution reads whether the round has finished and then acts on the answer. With a "
                    + "lane each, two of them can be inside that gap at once, and the operator reads one refresh "
                    + "as two.");
            }
        }

        [Test]
        public void Staging_into_a_round_that_has_already_been_written_is_refused_rather_than_swallowed()
        {
            var round = new WriteBackRound();
            var connection = AConnection();

            round.Stage(connection, [AnUpdate("refresh", 1)]);
            round.TakeStaged();

            Assert.Throws<InvalidOperationException>(
                () => round.Stage(connection, [AnUpdate("late", 2)]),
                "The alternative is losing the write quietly, which is the exact failure this whole commit exists "
                + "to prevent - and quiet is what makes it unfindable afterwards.");
        }

        private static Task StageFrom(WriteBackRound round, WorkTrackingSystemConnection connection, string execution)
        {
            return Task.Run(() =>
            {
                for (var update = 0; update < UpdatesFromEachExecution; update++)
                {
                    round.Stage(connection, [AnUpdate(execution, update)]);
                }
            });
        }

        /// <summary>
        /// Each execution writes to its own work items, so the union of the two is the sum of them. Two
        /// executions staging the same field is last-stage-wins by design and would hide a lost update
        /// behind a rule that is meant to be there.
        /// </summary>
        private static WriteBackFieldUpdate AnUpdate(string execution, int number)
            => new()
            {
                WorkItemId = $"{execution}-{number}",
                TargetFieldReference = AFieldEveryUpdateWrites,
                Value = number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };

        private static WorkTrackingSystemConnection AConnection()
            => new() { Id = 1, Name = "A connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
    }
}
