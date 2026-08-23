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
    [TestFixture]
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
    }
}
