using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// The one commit in this slice with no exact net, and this is what stands in for it.
    ///
    /// Every other commit is proved by reproducing a recorded run number for number. This one cannot be:
    /// it replaces where the numbers come from, so there is no earlier run to match draw for draw. What can
    /// be shown is that the forecast still samples the same distribution - the dates it produces land inside
    /// the range the released product's own runs wander over.
    ///
    /// The released product drew a fresh random number for every draw, paying no attention to where the draw
    /// sat. That is reproduced here exactly, so the comparison is against the real thing rather than against
    /// a description of it.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class TheDrawSourceChangedTheDistributionDidNotTest
    {
        private const int HowManyRunsShowTheReleasedProductsOwnSpread = 7;

        private static readonly long[] StartingNumbersTheNewSourceIsAskedFor = [1, 20260824, 987654321];

        private static readonly int[] ThePercentilesLighthouseShows = [50, 70, 85, 95];

        [Test]
        public async Task TheDatesFromTheNewDrawSource_LandWhereTheReleasedProductsOwnRunsLand()
        {
            var howFarTheReleasedProductWanders = await TheSpreadBetweenRunsOfTheReleasedProduct();

            var complaints = new List<string>();

            foreach (var startingNumber in StartingNumbersTheNewSourceIsAskedFor)
            {
                var fromTheNewSource = await new GoldForecastFixture()
                    .ForecastTheGoldPortfolio(new DrawsFromAPinnedStartingNumber(startingNumber));

                complaints.AddRange(WhereItLandsOutside(fromTheNewSource, howFarTheReleasedProductWanders, startingNumber));
            }

            Assert.That(complaints, Is.Empty,
                "The new draw source moved the distribution rather than only the numbers: " +
                string.Join("; ", complaints));
        }

        /// <summary>
        /// What is allowed is the range the released product's own runs covered, widened by that range again
        /// on either side. Two runs of an unpinned forecast already differ, so a bound tighter than its own
        /// wander would fail on the released product itself; three times it still catches a distribution that
        /// has actually moved, which shows up as many days rather than one or two.
        /// </summary>
        private static IEnumerable<string> WhereItLandsOutside(
            GoldForecastFixture.GoldPercentiles[] fromTheNewSource,
            Dictionary<(string ReferenceId, int Percentile), (int Lowest, int Highest)> howFarTheReleasedProductWanders,
            long startingNumber)
        {
            foreach (var feature in fromTheNewSource)
            {
                foreach (var percentile in ThePercentilesLighthouseShows)
                {
                    var wander = howFarTheReleasedProductWanders[(feature.ReferenceId, percentile)];
                    var width = wander.Highest - wander.Lowest;

                    var landed = PercentileOf(feature, percentile);

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
            var runs = new List<GoldForecastFixture.GoldPercentiles[]>();

            for (var run = 0; run < HowManyRunsShowTheReleasedProductsOwnSpread; run++)
            {
                runs.Add(await new GoldForecastFixture().ForecastTheGoldPortfolio(new DrawsAfreshEveryTime()));
            }

            return runs
                .SelectMany(run => run.SelectMany(feature => ThePercentilesLighthouseShows
                    .Select(percentile => (feature.ReferenceId, percentile, read: PercentileOf(feature, percentile)))))
                .GroupBy(read => (read.ReferenceId, read.percentile))
                .ToDictionary(
                    byPlace => byPlace.Key,
                    byPlace => (byPlace.Min(read => read.read), byPlace.Max(read => read.read)));
        }

        private static int PercentileOf(GoldForecastFixture.GoldPercentiles feature, int percentile) => percentile switch
        {
            50 => feature.P50,
            70 => feature.P70,
            85 => feature.P85,
            95 => feature.P95,
            _ => throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Not a percentile Lighthouse shows."),
        };
    }
}
