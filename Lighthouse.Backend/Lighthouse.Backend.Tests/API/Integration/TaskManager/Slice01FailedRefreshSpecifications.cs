using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 01 — a failed refresh says it failed.
    ///
    /// The contract: the terminal status pushed to the browser is the status that actually happened, and
    /// nothing else about the end of a refresh moves. The refresh log row, the operator's one summary
    /// line, the write-back round and the callers of the refresh endpoints all behave exactly as before —
    /// which is most of what these steps assert, because that is where the risk in this change lives.
    /// </summary>
    public partial class Slice01FailedRefreshTest : TaskManagerAcceptanceTest
    {
        private const string SummaryMarker = "Update completed";

        private readonly record struct SeededPortfolio(int Id, string Name);

        // --- Given ---

        private SeededPortfolio GivenAPortfolioThatIsRefreshedOnSchedule()
        {
            var connectionId = SeedConnection();
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";

            return new SeededPortfolio(SeedPortfolio(connectionId, portfolioName), portfolioName);
        }

        private void GivenTheTrackerIsUnreachable()
            => TheTrackerIsUnreachable(new InvalidOperationException("The work tracking system is unreachable"));

        private static void GivenTheTrackerAnswers()
        {
            // Nothing to arrange: the harness already answers every fetch with an empty result. Saying it
            // in the scenario keeps the positive control readable beside the failure it is the control for.
        }

        private static void GivenNothingInParticular()
        {
            // This scenario drives the queue directly and needs no seeded entity; the empty precondition
            // is written down so the scenario reads in the same three parts as every other one here.
        }

        // --- When ---

        private Task WhenTheScheduledRefreshRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        /// <summary>
        /// The other driving port onto the same refresh: the button an operator presses. It hands the work
        /// to the same queue and returns without waiting for it.
        /// </summary>
        private async Task<HttpResponseMessage> WhenSomebodyAsksForARefreshOf(SeededTeam team)
        {
            TheBrowserWasTold.Clear();

            using var client = Factory.CreateClient();
            return await client.PostAsync(new Uri($"/api/latest/teams/{team.Id}", UriKind.Relative), content: null);
        }

        private Exception? WhenSomebodyAwaitsAnUpdateThatThrows()
        {
            var queue = Factory.Services.GetRequiredService<IUpdateQueueService>();

            return Assert.CatchAsync(async () => await queue.EnqueueAndAwaitAsync(
                UpdateType.TeamDelete,
                id: 4711,
                _ => throw new InvalidOperationException("the delete could not be carried out")));
        }

        // --- Then ---

        private void ThenTheBrowserWasToldTheRefreshFailed(SeededTeam team)
            => ThenTheLastWordOn(UpdateType.Team, team.Id, UpdateProgress.Failed,
                "A refresh that threw and a refresh that worked must not arrive at the browser looking the same — "
                + "every surface this Epic builds afterwards reads this one value.");

        private void ThenTheBrowserWasToldTheRefreshFailed(SeededPortfolio portfolio)
            => ThenTheLastWordOn(UpdateType.Features, portfolio.Id, UpdateProgress.Failed,
                "The portfolio half of the cycle fails through the same path and has to report it the same way.");

        private void ThenTheBrowserWasToldTheRefreshCompleted(SeededTeam team)
            => ThenTheLastWordOn(UpdateType.Team, team.Id, UpdateProgress.Completed,
                "Reporting failure is only worth anything if success still reports success.");

        private void ThenTheLastWordOn(UpdateType updateType, int id, UpdateProgress expected, string because)
            => Assert.That(TheBrowserWasTold.LastStatusOf(updateType, id), Is.EqualTo(expected),
                $"{because} The browser was told: {TheBrowserWasTold.Describe()}");

        /// <summary>
        /// A refresh is pushed to the browser twice: once when it is taken on, once when it ends. Running
        /// is recorded in the status store but never pushed, which is why a browser cannot today tell a
        /// refresh that is running from one that is merely waiting — slice 02's subject, not this one's.
        /// Pinning both pushes here is what keeps this slice to changing the second one.
        /// </summary>
        private void ThenTheBrowserWasToldTheRefreshWasQueuedAndThenThatItFailed(SeededTeam team)
        {
            var announced = TheBrowserWasTold.For(UpdateType.Team, team.Id);

            Assert.That(announced, Is.EqualTo(new[] { UpdateProgress.Queued, UpdateProgress.Failed }),
                "The browser hears about a refresh when it starts and when it ends; this slice changes what it "
                + $"hears at the end and nothing else. The browser was told: {TheBrowserWasTold.Describe()}");
        }

        private void ThenTheRecordedRefreshSaysItDidNotSucceed(SeededTeam team)
        {
            var recorded = TheRecordedRefreshFor(RefreshType.Team, team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded, Is.Not.Null,
                    "A refresh that failed still ran, and the history under System Info is where an operator goes to see that it did.");
                Assert.That(recorded!.Success, Is.False,
                    "This row already told the truth before the slice; the slice is about the browser catching up with it, not about changing it.");
            }
        }

        private void ThenTheOperatorSeesOneSummaryLineSayingItDidNotSucceed()
        {
            var summaries = TheOperatorVisibleLines.Where(line => line.Contains(SummaryMarker, StringComparison.Ordinal)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summaries, Has.Count.EqualTo(1),
                    "One refresh, one line that says what it did — a failing refresh does not get to skip it. Operator-visible lines: "
                    + string.Join(" | ", TheOperatorVisibleLines));
                Assert.That(summaries[0], Does.Contain("success=False"),
                    "The line an operator greps is the same line either way; only the value on it changes.");
            }
        }

        private void ThenTheExecutionReportedItselfFinishedToItsRound()
            => Assert.That(WriteBackFlushes.Value, Is.EqualTo(1),
                "An execution that never reports itself finished leaves its round open, and a round that never "
                + "finishes silently drops every write it had staged. The failure path owes its round the same "
                + "report the success path does.");

        /// <summary>
        /// The round's line is written by whichever execution leaves it last, and taking the summary empties
        /// it. Exactly one line is therefore the observable proof that the round reached its end rather than
        /// being stranded by the failure.
        /// </summary>
        private void ThenTheRoundSaidItsPieceExactlyOnce()
            => Assert.That(TheOperatorVisibleLines.Count(line => line.Contains(SummaryMarker, StringComparison.Ordinal)), Is.EqualTo(1),
                "No line at all means the round never finished; two means it was counted twice. Operator-visible lines: "
                + string.Join(" | ", TheOperatorVisibleLines));

        private static void ThenTheRequestWasAccepted(HttpResponseMessage response)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The endpoint hands the refresh to the queue and returns; it has never waited for the outcome, "
                + "and a caller that starts seeing a 500 because the refresh later failed would be a failure mode "
                + "this slice invented.");

            response.Dispose();
        }

        private async Task ThenThatRefreshStillEndedAsFailed(SeededTeam team)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (TheBrowserWasTold.LastStatusOf(UpdateType.Team, team.Id) != UpdateProgress.Failed)
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The refresh asked for over the endpoint never reported failure. "
                        + $"The browser was told: {TheBrowserWasTold.Describe()}");
                }

                await Task.Delay(20);
            }
        }

        private static void ThenThatCallerSawTheFailure(Exception? awaited)
            => Assert.That(awaited, Is.InstanceOf<InvalidOperationException>(),
                "Callers that await an update — the two delete endpoints — are faulted by the queue today and must "
                + "still be. They never route through the updater whose swallowed exception this slice removes, so "
                + "this is the guard that says so out loud.");
    }
}
