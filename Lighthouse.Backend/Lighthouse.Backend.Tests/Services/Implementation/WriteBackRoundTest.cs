using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// A refresh and the forecast it triggers are one round, so the operator reads one line about them:
    /// the refresh's own story, with the forecast folded in as the time it cost and how it ended.
    /// </summary>
    [TestFixture]
    public class WriteBackRoundTest
    {
        private static readonly RefreshRoundSummary Refresh = new("Portfolio", "Checkout", 1200, true);

        [Test]
        public void A_round_that_only_refreshed_reports_the_refresh_with_no_forecast_in_it()
        {
            var round = new WriteBackRound();
            round.ReportRefresh(Refresh);

            var summary = round.TakeSummary();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary!.EntityName, Is.EqualTo("Checkout"));
                Assert.That(summary.ForecastDurationMs, Is.Null);
                Assert.That(summary.ForecastSucceeded, Is.True);
                Assert.That(summary.ForecastCancelled, Is.False);
            }
        }

        [Test]
        public void A_round_that_only_forecast_reports_the_forecast_as_its_whole_story()
        {
            var round = new WriteBackRound();
            var forecast = new RefreshRoundSummary("Portfolio", "Checkout", 300, true) { ForecastDurationMs = 300 };
            round.ReportForecast(forecast);

            var summary = round.TakeSummary();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary, Is.SameAs(forecast));
                Assert.That(summary!.ForecastSucceeded, Is.True);
            }
        }

        [Test]
        public void A_forecast_that_was_stopped_folds_into_the_refresh_as_stopped_not_as_broken()
        {
            var round = new WriteBackRound();
            round.ReportRefresh(Refresh);
            round.ReportForecast(new RefreshRoundSummary("Portfolio", "Checkout", 450, false)
            {
                ForecastDurationMs = 450,
                ForecastSucceeded = false,
                ForecastCancelled = true,
            });

            var summary = round.TakeSummary();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary!.DurationMs, Is.EqualTo(1200));
                Assert.That(summary.ForecastDurationMs, Is.EqualTo(450));
                Assert.That(summary.ForecastSucceeded, Is.False);
                Assert.That(summary.ForecastCancelled, Is.True);
            }
        }

        [Test]
        public void A_round_that_is_asked_twice_speaks_only_once()
        {
            var round = new WriteBackRound();
            round.ReportRefresh(Refresh);

            round.TakeSummary();

            Assert.That(round.TakeSummary(), Is.Null);
        }

        [Test]
        public void A_round_finishes_only_once_every_execution_that_joined_it_has_left()
        {
            var round = new WriteBackRound();
            round.Join();

            var firstWasLast = round.Leave();
            var finishedAfterFirst = round.HasFinished;
            var secondWasLast = round.Leave();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstWasLast, Is.False);
                Assert.That(finishedAfterFirst, Is.False);
                Assert.That(secondWasLast, Is.True);
                Assert.That(round.HasFinished, Is.True);
            }
        }

        [Test]
        public void What_was_staged_is_handed_over_once()
        {
            var round = new WriteBackRound();
            round.Stage(Connection(1), [Update("PAY-1")]);

            round.TakeStaged();

            Assert.That(round.TakeStaged(), Is.Empty);
        }

        private static WorkTrackingSystemConnection Connection(int id)
            => new() { Id = id, Name = $"Connection {id}", WorkTrackingSystem = WorkTrackingSystems.Jira };

        private static WriteBackFieldUpdate Update(string workItemId)
            => new() { WorkItemId = workItemId, TargetFieldReference = "customfield_10001", Value = "2026-11-02" };
    }
}
