using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class ThroughputReportMetricTest
    {
        private static readonly ReportOwner Owner = ReportOwner.Team(7);

        private static readonly string[] Members = ["total", "perDay"];

        [TestCase(90, 52, 65)]
        [TestCase(30, 45, 0)]
        [TestCase(7, 3, 14)]
        [TestCase(1, 1, 2)]
        public void Then_freezes_and_Now_shows_the_total_and_the_unrounded_average_per_day_over_every_day_of_the_window(int lengthDays, int finishedInThen, int finishedInNow)
        {
            var thenWindow = WindowOf(new DateOnly(2026, 7, 31), lengthDays);
            var nowWindow = WindowOf(new DateOnly(2026, 10, 2), lengthDays);
            var subject = new ThroughputReportMetric(SeriesAnswering(
                (thenWindow, SpreadOver(lengthDays, finishedInThen)),
                (nowWindow, SpreadOver(lengthDays, finishedInNow))));

            var read = subject.Read(subject.CaptureThen(Owner, thenWindow), Owner, nowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Keys, Is.EquivalentTo(Members));
                Assert.That(read.AtThen["total"], Is.EqualTo(CapturedValue.Shown(finishedInThen, lengthDays)));
                Assert.That(read.AtThen["perDay"], Is.EqualTo(CapturedValue.Shown((double)finishedInThen / lengthDays, lengthDays)));
                Assert.That(read.AtNow.Keys, Is.EquivalentTo(Members));
                Assert.That(read.AtNow["total"], Is.EqualTo(CapturedValue.Shown(finishedInNow, lengthDays)));
                Assert.That(read.AtNow["perDay"], Is.EqualTo(CapturedValue.Shown((double)finishedInNow / lengthDays, lengthDays)));
            }
        }

        [Test]
        public void A_window_where_nothing_finished_is_a_real_zero_never_a_dash()
        {
            var window = WindowOf(new DateOnly(2026, 8, 31), 30);
            var subject = new ThroughputReportMetric(SeriesAnswering((window, new int[30])));

            var read = subject.Read(subject.CaptureThen(Owner, window), Owner, window);

            var zeroOverThirtyDays = CapturedValue.Shown(0, 30);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Values, Is.All.EqualTo(zeroOverThirtyDays));
                Assert.That(read.AtNow.Values, Is.All.EqualTo(zeroOverThirtyDays));
            }
        }

        [Test]
        public void A_report_that_froze_nothing_for_throughput_shows_no_Then_value_and_still_computes_Now()
        {
            var nowWindow = WindowOf(new DateOnly(2026, 10, 2), 30);
            var subject = new ThroughputReportMetric(SeriesAnswering((nowWindow, SpreadOver(30, 3))));

            var read = subject.Read(null, Owner, nowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen, Is.Empty);
                Assert.That(read.AtNow["total"], Is.EqualTo(CapturedValue.Shown(3, 30)));
                Assert.That(read.AtNow["perDay"], Is.EqualTo(CapturedValue.Shown(0.1, 30)));
            }
        }

        [Test]
        public void The_average_per_day_divides_by_the_days_actually_summed_not_the_stored_window_length()
        {
            var window = WindowOf(new DateOnly(2026, 10, 2), 30);
            var subject = new ThroughputReportMetric(SeriesAnswering((window, SpreadOver(20, 10))));

            var read = subject.Read(null, Owner, window);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtNow["total"], Is.EqualTo(CapturedValue.Shown(10, 20)));
                Assert.That(read.AtNow["perDay"], Is.EqualTo(CapturedValue.Shown(0.5, 20)));
            }
        }

        private static ReportWindow WindowOf(DateOnly end, int lengthDays) => ReportWindow.EndingOn(end, lengthDays)!;

        private static int[] SpreadOver(int lengthDays, int finished)
            => [.. Enumerable.Range(0, lengthDays).Select(day => finished / lengthDays + (day < finished % lengthDays ? 1 : 0))];

        private static IReportMetricSeries SeriesAnswering(params (ReportWindow Window, int[] Daily)[] answers)
        {
            var series = new Mock<IReportMetricSeries>(MockBehavior.Strict);
            foreach (var (window, daily) in answers)
            {
                series.Setup(s => s.DailyThroughput(Owner, window)).Returns(daily);
            }

            return series.Object;
        }
    }
}
