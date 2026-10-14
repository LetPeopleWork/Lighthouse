using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class DailySeriesSummaryTest
    {
        private static readonly string[] Members = ["average", "median", "min", "max"];

        private static readonly int[] UnsortedDays = [5, 1, 4, 2, 3, 6];

        [TestCase(new[] { 1, 2, 3, 4 }, 2.5, 2, 1, 4, TestName = "An even number of days takes the lower middle day, never their mean")]
        [TestCase(new[] { 1, 2, 3, 4, 5 }, 3, 2, 1, 5, TestName = "An odd number of days takes the day just below the middle, as the 50th percentile does")]
        [TestCase(new[] { 0, 0, 0 }, 0, 0, 0, 0, TestName = "Days with nothing to count are a real zero on every line")]
        [TestCase(new[] { 7, 1, 4, 3 }, 3.75, 3, 1, 7, TestName = "Unsorted days are summarised as if sorted")]
        public void Summarises_every_day_of_the_window_with_the_window_as_the_sample(int[] daily, double average, int median, int min, int max)
        {
            var summary = DailySeriesSummary.Of(daily);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.Keys, Is.EquivalentTo(Members));
                Assert.That(summary["average"], Is.EqualTo(CapturedValue.Shown(average, daily.Length)));
                Assert.That(summary["median"], Is.EqualTo(CapturedValue.Shown(median, daily.Length)));
                Assert.That(summary["min"], Is.EqualTo(CapturedValue.Shown(min, daily.Length)));
                Assert.That(summary["max"], Is.EqualTo(CapturedValue.Shown(max, daily.Length)));
            }
        }

        [Test]
        public void Summarising_leaves_the_days_in_the_order_they_were_handed_over()
        {
            int[] daily = [.. UnsortedDays];

            DailySeriesSummary.Of(daily);

            Assert.That(daily, Is.EqualTo(UnsortedDays).AsCollection);
        }
    }
}
