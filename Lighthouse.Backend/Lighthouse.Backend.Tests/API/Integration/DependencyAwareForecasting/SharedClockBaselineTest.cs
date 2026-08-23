namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// Every date the benchmark Portfolio produces, against the ones written down when the forecast's draw
    /// source was replaced. Putting the Teams on one clock and then running the simulated runs side by side
    /// are both meant to leave the output exactly where it was, and this is what says whether they did.
    ///
    /// Exact equality, not "close enough". A comparison with a tolerance cannot tell "the restructure is
    /// right" apart from "the restructure is wrong by less than the tolerance", and the point of addressing
    /// draws by coordinate was to buy the exact comparison.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class SharedClockBaselineTest
    {
        [Test]
        public async Task ForecastOfTheBenchmarkPortfolio_MatchesTheRecordedBaselineExactly()
        {
            var baseline = SharedClockBaselineFixture.ReadBaseline();

            var actual = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(baseline.StartingNumber, Is.EqualTo(SharedClockBaselineFixture.TheStartingNumberTheBaselineWasRecordedFrom),
                    "The baseline was recorded from a different starting number, so its dates are not comparable.");

                Assert.That(actual, Is.EqualTo(baseline.Features),
                    $"Dates differ from the baseline recorded at {baseline.RecordedAt}.");
            }
        }

        /// <summary>
        /// The same forecast twice in one process. It is what stops the comparison above passing for the
        /// wrong reason on a build where the run has quietly stopped depending on the starting number at all.
        /// </summary>
        [Test]
        public async Task TheSameForecastTwice_ProducesTheSameDates()
        {
            var once = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio();
            var again = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio();

            Assert.That(again, Is.EqualTo(once));
        }
    }
}
