using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class WorkItemAgeReportMetricTest
    {
        private static readonly ReportOwner Owner = ReportOwner.Team(7);

        private static readonly string[] Members = ["average", "median", "min", "max"];

        [TestCase("330,340,350,360", 345, 340, 330, 360, TestName = "An even number of days takes the lower middle day")]
        [TestCase("50,10,30", 30, 10, 10, 50, TestName = "An odd number of days takes the day just below the middle")]
        [TestCase("0,0,10,20", 7.5, 0, 0, 20, TestName = "Days with nothing in progress take part as a real 0")]
        [TestCase("1,2", 1.5, 1, 1, 2, TestName = "The average stays unrounded")]
        [TestCase("42", 42, 42, 42, 42, TestName = "A single day is every line")]
        public void Then_and_Now_summarise_the_daily_total_age_over_every_day_of_the_window(string dailyTotals, double average, double median, double min, double max)
        {
            int[] daily = [.. dailyTotals.Split(',').Select(int.Parse)];
            var thenWindow = WindowOf(new DateOnly(2026, 8, 31), daily.Length);
            var nowWindow = WindowOf(new DateOnly(2026, 10, 2), daily.Length);
            var subject = new WorkItemAgeReportMetric(SeriesAnswering((thenWindow, daily), (nowWindow, daily)));

            var read = subject.Read(subject.CaptureThen(Owner, thenWindow), Owner, nowWindow);

            var days = daily.Length;
            using (Assert.EnterMultipleScope())
            {
                foreach (var side in new[] { read.AtThen, read.AtNow })
                {
                    Assert.That(side.Keys, Is.EquivalentTo(Members));
                    Assert.That(side["average"], Is.EqualTo(CapturedValue.Shown(average, days)));
                    Assert.That(side["median"], Is.EqualTo(CapturedValue.Shown(median, days)));
                    Assert.That(side["min"], Is.EqualTo(CapturedValue.Shown(min, days)));
                    Assert.That(side["max"], Is.EqualTo(CapturedValue.Shown(max, days)));
                }
            }
        }

        [Test]
        public void Then_reads_its_own_window_and_Now_reads_its_own()
        {
            var thenWindow = WindowOf(new DateOnly(2026, 8, 31), 3);
            var nowWindow = WindowOf(new DateOnly(2026, 10, 2), 3);
            var subject = new WorkItemAgeReportMetric(SeriesAnswering((thenWindow, [10, 20, 30]), (nowWindow, [100, 200, 600])));

            var read = subject.Read(subject.CaptureThen(Owner, thenWindow), Owner, nowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen["average"], Is.EqualTo(CapturedValue.Shown(20, 3)));
                Assert.That(read.AtThen["max"], Is.EqualTo(CapturedValue.Shown(30, 3)));
                Assert.That(read.AtNow["average"], Is.EqualTo(CapturedValue.Shown(300, 3)));
                Assert.That(read.AtNow["max"], Is.EqualTo(CapturedValue.Shown(600, 3)));
            }
        }

        [Test]
        public void A_window_with_nothing_in_progress_is_a_real_zero_never_a_dash()
        {
            var window = WindowOf(new DateOnly(2026, 8, 31), 30);
            var subject = new WorkItemAgeReportMetric(SeriesAnswering((window, new int[30])));

            var read = subject.Read(subject.CaptureThen(Owner, window), Owner, window);

            var zeroOverThirtyDays = CapturedValue.Shown(0, 30);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Values, Is.All.EqualTo(zeroOverThirtyDays));
                Assert.That(read.AtNow.Values, Is.All.EqualTo(zeroOverThirtyDays));
            }
        }

        private static ReportWindow WindowOf(DateOnly end, int lengthDays) => ReportWindow.EndingOn(end, lengthDays)!;

        private static IReportMetricSeries SeriesAnswering(params (ReportWindow Window, int[] Daily)[] answers)
        {
            var series = new Mock<IReportMetricSeries>(MockBehavior.Strict);
            foreach (var (window, daily) in answers)
            {
                series.Setup(s => s.DailyTotalWorkItemAge(Owner, window)).Returns(daily);
            }

            return series.Object;
        }
    }
}
