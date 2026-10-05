using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class NeedBandTest
    {
        // Runs of one hundred, how many runs ended at each count of Work Items pulled.
        private static readonly Dictionary<int, int>[] Distributions =
        [
            new() { [10] = 5, [8] = 10, [6] = 20, [5] = 15, [3] = 50 },
            new() { [12] = 100 },
            new() { [0] = 100 },
            new() { [40] = 1, [25] = 9, [17] = 30, [9] = 40, [2] = 15, [0] = 5 },
            new() { [7] = 33, [6] = 33, [1] = 34 },
        ];

        [TestCase(2, 3, 8, RefinementVerdict.Below)]
        [TestCase(3, 3, 8, RefinementVerdict.In)]
        [TestCase(8, 3, 8, RefinementVerdict.In)]
        [TestCase(9, 3, 8, RefinementVerdict.Above)]
        [TestCase(0, 0, 0, RefinementVerdict.In)]
        [TestCase(1, 0, 0, RefinementVerdict.Above)]
        [TestCase(0, 1, 1, RefinementVerdict.Below)]
        [TestCase(2, 0, 0, RefinementVerdict.Above)]
        [TestCase(6, 1, 11, RefinementVerdict.In)]
        public void The_verdict_counts_both_ends_as_in_range(int readyCount, int low, int high, RefinementVerdict expected)
        {
            Assert.That(NeedBand.VerdictFor(readyCount, low, high), Is.EqualTo(expected));
        }

        // Reading the forecast at p instead of at 100 - p would give 3 for the high end and 5 for the low end.
        [TestCase(50, 5)]
        [TestCase(85, 8)]
        [TestCase(95, 10)]
        [TestCase(30, 3)]
        public void A_likelihood_reads_the_count_that_share_of_runs_reaches(int percentile, int expected)
        {
            var forecast = new HowManyForecast(Distributions[0], 6);

            Assert.That(NeedBand.ValueAt(forecast, percentile), Is.EqualTo(expected));
        }

        // The band reads whatever the forecast says, however many Work Items are waiting to be refined.
        [TestCase(2, 50, 0)]
        [TestCase(2, 85, 0)]
        [TestCase(1, 50, 12)]
        [TestCase(1, 85, 12)]
        public void A_zero_forecast_reads_zero_and_a_large_one_is_read_in_full(int distribution, int percentile, int expected)
        {
            var forecast = new HowManyForecast(Distributions[distribution], 6);

            Assert.That(NeedBand.ValueAt(forecast, percentile), Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(DistributionIndices))]
        public void A_likelier_end_is_never_higher(int distribution)
        {
            var forecast = new HowManyForecast(Distributions[distribution], 6);
            var misreadings = new List<string>();

            for (var low = 1; low < 99; low++)
            {
                for (var high = low + 1; high <= 99; high++)
                {
                    var lowValue = NeedBand.ValueAt(forecast, low);
                    var highValue = NeedBand.ValueAt(forecast, high);

                    if (lowValue > highValue)
                    {
                        misreadings.Add($"{low}/{high} read {lowValue}/{highValue}");
                    }
                }
            }

            Assert.That(misreadings, Is.Empty);
        }

        private static IEnumerable<int> DistributionIndices() => Enumerable.Range(0, Distributions.Length);
    }
}
