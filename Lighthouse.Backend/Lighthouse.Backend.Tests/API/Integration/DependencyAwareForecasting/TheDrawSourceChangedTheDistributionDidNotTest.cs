using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// The one change in this slice with no exact net, and this is what stands in for it.
    ///
    /// Every other change is proved by reproducing a recorded run number for number. Replacing where the
    /// numbers come from cannot be: there is no earlier run to match draw for draw. What can be shown is
    /// that the forecast still samples the same distribution - the dates it produces land inside the range
    /// the released product's own runs wander over.
    ///
    /// The released product drew a fresh random number for every draw, paying no attention to where the draw
    /// sat, so that is what it is compared against: the real behaviour rather than a description of it.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class TheDrawSourceChangedTheDistributionDidNotTest
    {
        private const int HowManyRunsShowTheReleasedProductsOwnSpread = 5;

        private const int OneDayEitherSideOfADayBoundary = 1;

        private static readonly long[] StartingNumbersTheNewSourceIsAskedFor = [1, 20260824, 987654321];

        [Test]
        public async Task TheDatesFromTheNewDrawSource_LandWhereTheReleasedProductsOwnRunsLand()
        {
            var howFarTheReleasedProductWanders = await TheSpreadBetweenRunsOfTheReleasedProduct();

            var complaints = new List<string>();

            foreach (var startingNumber in StartingNumbersTheNewSourceIsAskedFor)
            {
                var fromTheNewSource = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio(
                    new DrawsFromAPinnedStartingNumber(startingNumber));

                complaints.AddRange(WhereItLandsOutside(fromTheNewSource, howFarTheReleasedProductWanders, startingNumber));
            }

            Assert.That(complaints, Is.Empty,
                "The new draw source moved the distribution rather than only the numbers: " +
                string.Join("; ", complaints));
        }

        /// <summary>
        /// What is allowed is the range the released product's own runs covered, widened by that range again
        /// on either side, and never by less than a day. Two runs of an unpinned forecast already differ, so
        /// a bound tighter than its own wander would fail on the released product itself - and on a Portfolio
        /// this size ten thousand runs settle so closely that its own range is often nothing at all, while a
        /// date is still counted in whole days and two samples of one distribution can fall either side of a
        /// day boundary. A distribution that has really moved shows up as many days, not one.
        /// </summary>
        private static IEnumerable<string> WhereItLandsOutside(
            SharedClockBaselineFixture.BaselinePercentiles[] fromTheNewSource,
            Dictionary<(string ReferenceId, int Percentile), (int Lowest, int Highest)> howFarTheReleasedProductWanders,
            long startingNumber)
        {
            foreach (var feature in fromTheNewSource)
            {
                foreach (var percentile in SharedClockBaselineFixture.Percentiles)
                {
                    var wander = howFarTheReleasedProductWanders[(feature.ReferenceId, percentile)];
                    var width = Math.Max(wander.Highest - wander.Lowest, OneDayEitherSideOfADayBoundary);
                    var landed = feature.At(percentile);

                    if (landed < wander.Lowest - width || landed > wander.Highest + width)
                    {
                        yield return
                            $"{feature.ReferenceId} at {percentile}% read {landed} from starting number " +
                            $"{startingNumber}, where the released product read between {wander.Lowest} and {wander.Highest}";
                    }
                }
            }
        }

        private static async Task<Dictionary<(string, int), (int Lowest, int Highest)>> TheSpreadBetweenRunsOfTheReleasedProduct()
        {
            var runs = new List<SharedClockBaselineFixture.BaselinePercentiles[]>();

            for (var run = 0; run < HowManyRunsShowTheReleasedProductsOwnSpread; run++)
            {
                runs.Add(await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio(new DrawsAfreshEveryTime()));
            }

            return runs
                .SelectMany(run => run.SelectMany(feature => SharedClockBaselineFixture.Percentiles
                    .Select(percentile => (feature.ReferenceId, percentile, read: feature.At(percentile)))))
                .GroupBy(read => (read.ReferenceId, read.percentile))
                .ToDictionary(
                    byPlace => byPlace.Key,
                    byPlace => (byPlace.Min(read => read.read), byPlace.Max(read => read.read)));
        }
    }
}
