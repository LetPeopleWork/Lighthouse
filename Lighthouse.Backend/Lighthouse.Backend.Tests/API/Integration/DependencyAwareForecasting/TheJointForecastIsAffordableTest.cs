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
    /// times what the forecast costs on a developer machine, did exactly that. Every forecast in this
    /// namespace runs ten to twenty times slower on a build agent than on the machine the bound was taken
    /// on, including the ones that passed, so five times over was never going to be enough.
    ///
    /// What is left is a hang guard rather than a budget: a bound far above anything an agent has ever
    /// taken, which a forecast reaches only by looping or by doing orders of magnitude more work than it
    /// used to. The assertion that carries the weight is the one beside it, holding the dates to the
    /// recorded baseline, because a forecast that got faster by doing less work is the failure actually
    /// worth catching. Whether the joint clock made a forecast dearer is answered by the measurement, not
    /// here.
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
        private const int TheMostSecondsAForecastMayTakeBeforeItIsStuck = 180;

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
                    $"{stopwatch.Elapsed.TotalSeconds:F1} seconds. That is minutes beyond what the slowest " +
                    "build agent has ever taken, so the simulation is either looping or doing orders of " +
                    "magnitude more work than it used to.");

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
                    "seconds, which is minutes beyond what the slowest build agent has ever taken - the " +
                    "readiness check on every simulated day is either looping or never settling.");

                Assert.That(dates.Select(feature => feature.P85), Is.All.GreaterThan(0),
                    "It finished quickly because it produced no dates, which is not the same as being fast.");
            }
        }
    }
}
