using System.Diagnostics;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// Every simulated run now lasts until the slowest Team finishes rather than ending as soon as its own
    /// Team is done, so the joint clock could have made the forecast dearer. Whether it did was measured by
    /// hand, before and after, on one machine, and both numbers are written into the slice brief.
    ///
    /// This is not that measurement, and it deliberately does not pretend to be. A wall clock recorded on
    /// one machine means nothing on another, so a test comparing against a number checked in from somebody's
    /// laptop would go red in CI for a reason that is not a defect - and the first bound written here, five
    /// times what the forecast costs on a developer machine, did exactly that. A build agent takes ten to
    /// twenty times longer than the machine such a number is taken on, so five times over was never going
    /// to be enough.
    ///
    /// What is left is a guard against a forecast that is stuck rather than a budget for one that got
    /// dearer. The benchmark Portfolio forecasts in a fraction of a second, so the bound below sits orders
    /// of magnitude above it and a run only reaches it by looping or by doing vastly more work than it used
    /// to. The assertion that carries the weight is the one beside it, holding the dates to the recorded
    /// baseline, because a forecast that got faster by doing less work is the failure actually worth
    /// catching.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class TheJointForecastIsAffordableTest
    {
        private const int TheMostSecondsAForecastMayTakeBeforeItIsStuck = 30;

        [Test]
        public async Task TheJointForecast_FinishesInTheTimeAForecastShouldTake_AndProducesTheRecordedDates()
        {
            var baseline = SharedClockBaselineFixture.ReadBaseline();

            var stopwatch = Stopwatch.StartNew();
            var dates = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio();
            stopwatch.Stop();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(TheMostSecondsAForecastMayTakeBeforeItIsStuck),
                    $"A forecast of {baseline.Features.Length} Features across a handful of Teams took " +
                    $"{stopwatch.Elapsed.TotalSeconds:F1} seconds, where the slowest build agent takes a " +
                    "fraction of one, so the simulation is either looping or doing orders of magnitude more " +
                    "work than it used to.");

                Assert.That(dates, Is.EqualTo(baseline.Features),
                    "It finished in time but produced different dates, which is the cheaper kind of fast.");
            }
        }

        /// <summary>
        /// The timing above is taken over a Portfolio in which nothing waits on anything, which is the one
        /// shape that skips the work this epic added: with no waits at all, asking whether anything may be
        /// worked on is a single comparison. A Portfolio that does have waits pays for the readiness check
        /// on every simulated day, and that is the path worth bounding.
        /// </summary>
        [Test]
        public async Task AForecastWithWaitsInIt_CostsNoMoreThanAForecastShouldTake()
        {
            var stopwatch = Stopwatch.StartNew();
            var dates = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio(
                SharedClockBaselineFixture.PinnedDraws, SharedClockBaselineFixture.EachFeatureWaitsOnTheOneBefore);
            stopwatch.Stop();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(TheMostSecondsAForecastMayTakeBeforeItIsStuck),
                    $"A forecast whose Features wait on one another took {stopwatch.Elapsed.TotalSeconds:F1} " +
                    "seconds, where the slowest build agent takes a fraction of one - the readiness check on " +
                    "every simulated day is either looping or never settling.");

                Assert.That(dates.Select(feature => feature.P85), Is.All.GreaterThan(0),
                    "It finished quickly because it produced no dates, which is not the same as being fast.");
            }
        }
    }
}
