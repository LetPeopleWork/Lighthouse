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
    /// laptop would go red in CI for a reason that is not a defect. What it does instead is bound the run
    /// loosely enough to survive the slowest build agent while still catching a restructure that made the
    /// forecast many times dearer - and, more usefully, hold the dates it produced to the recorded baseline,
    /// because a forecast that got faster by doing less work is the failure actually worth catching.
    /// </summary>
    // Not run alongside anything else. Every forecast in this project now uses every core it can get, so
    // a timing taken while two other fixtures are each running ten thousand simulated runs measures the
    // contention, not the change - the same shape as the ReleaseServiceTest failures that look like
    // regressions and are not.
    [TestFixture]
    [NonParallelizable]
    [Category("acceptance")]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class TheJointForecastIsAffordableTest
    {
        private const int TheMostSecondsAForecastOfThisPortfolioMayTake = 10;

        [Test]
        public async Task TheJointForecast_FinishesInTheTimeAForecastShouldTake_AndProducesTheRecordedDates()
        {
            var baseline = SharedClockBaselineFixture.ReadBaseline();

            var stopwatch = Stopwatch.StartNew();
            var dates = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio();
            stopwatch.Stop();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(TheMostSecondsAForecastOfThisPortfolioMayTake),
                    $"A forecast of {baseline.Features.Length} Features across a handful of Teams took " +
                    $"{stopwatch.Elapsed.TotalSeconds:F1} seconds. Even on a slow build agent that is far " +
                    "beyond what this has ever cost, so something in the simulation is doing much more work " +
                    "than it used to.");

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
                Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(TheMostSecondsAForecastOfThisPortfolioMayTake),
                    $"A forecast whose Features wait on one another took {stopwatch.Elapsed.TotalSeconds:F1} " +
                    "seconds, which is far beyond what one without them costs.");

                Assert.That(dates.Select(feature => feature.P85), Is.All.GreaterThan(0),
                    "It finished quickly because it produced no dates, which is not the same as being fast.");
            }
        }
    }
}
