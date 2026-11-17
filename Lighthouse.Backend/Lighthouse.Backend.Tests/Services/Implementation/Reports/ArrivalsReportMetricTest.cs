using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class ArrivalsReportMetricTest
    {
        private static readonly string[] Members = ["total", "perDay", "throughputPerDay", "gapPerDay"];

        private const double Tolerance = 1e-9;

        [TestCase(90, 99, 72, 1.10, 0.80, 0.30)]
        [TestCase(30, 27, 30, 0.90, 1.00, -0.10)]
        [TestCase(30, 5, 0, 5.0 / 30, 0.0, 5.0 / 30)]
        [TestCase(30, 0, 4, 0.0, 4.0 / 30, -4.0 / 30)]
        [TestCase(1, 1, 2, 1.0, 2.0, -1.0)]
        public void Then_freezes_and_Now_shows_started_and_finished_per_day_with_the_gap_between_them_unrounded(
            int lengthDays, int started, int finished, double startedPerDay, double finishedPerDay, double gapPerDay)
        {
            var window = WindowOf(new DateOnly(2026, 10, 2), lengthDays);
            var subject = new ArrivalsReportMetric(SeriesAnswering(Owner, (window, SpreadOver(lengthDays, started), SpreadOver(lengthDays, finished))));

            var read = subject.Read(subject.CaptureThen(Owner, window), Owner, window);

            foreach (var side in new[] { read.AtThen, read.AtNow })
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(side.Keys, Is.EquivalentTo(Members));
                    Assert.That(side["total"], Is.EqualTo(CapturedValue.Shown(started, lengthDays)), "the total started must be a whole count over every day of the window");
                    Assert.That(side["perDay"].Value, Is.EqualTo(startedPerDay).Within(Tolerance), "started per day must be the total over the window's days, unrounded");
                    Assert.That(side["throughputPerDay"].Value, Is.EqualTo(finishedPerDay).Within(Tolerance), "finished per day must be the same average the Throughput panel shows");
                    Assert.That(side["gapPerDay"].Value, Is.EqualTo(gapPerDay).Within(Tolerance), "the gap must be started minus finished per day, signed and unrounded, so a client never restates it");
                    Assert.That(side["gapPerDay"].SampleSize, Is.EqualTo(lengthDays), "the gap must carry the window's days as its sample size");
                    Assert.That(side["gapPerDay"].AbsentReason, Is.Null, "a window where nothing started or finished is a real value, never absent");
                }
            }
        }

        [Test]
        public void Throughput_inside_the_arrivals_capture_is_the_figure_the_throughput_metric_reads_from_the_same_series()
        {
            var window = WindowOf(new DateOnly(2026, 10, 2), 30);
            var series = SeriesAnswering(Owner, (window, SpreadOver(30, 12), SpreadOver(30, 21)));

            var arrivals = new ArrivalsReportMetric(series).Read(null, Owner, window);
            var throughput = new ThroughputReportMetric(series).Read(null, Owner, window);

            Assert.That(arrivals.AtNow["throughputPerDay"], Is.EqualTo(throughput.AtNow["perDay"]), "both panels must show one finished-per-day number for the same window");
        }

        [Test]
        public void A_portfolio_reads_its_arrivals_the_same_way()
        {
            var portfolio = ReportOwner.Portfolio(3);
            var window = WindowOf(new DateOnly(2026, 10, 2), 30);
            var subject = new ArrivalsReportMetric(SeriesAnswering(portfolio, (window, SpreadOver(30, 6), SpreadOver(30, 3))));

            var read = subject.Read(null, portfolio, window);

            Assert.That(read.AtNow["gapPerDay"].Value, Is.EqualTo(0.1).Within(Tolerance), "a Portfolio's gap must be Features started minus finished per day");
        }

        [Test]
        public void Then_stays_as_it_was_frozen_and_is_never_rebuilt_from_live_data()
        {
            var thenWindow = WindowOf(new DateOnly(2026, 7, 31), 30);
            var nowWindow = WindowOf(new DateOnly(2026, 10, 2), 30);
            var series = new Mock<IReportMetricSeries>(MockBehavior.Strict);
            series.SetupSequence(s => s.DailyArrivals(Owner, thenWindow)).Returns(SpreadOver(30, 30)).Returns(SpreadOver(30, 3));
            series.SetupSequence(s => s.DailyThroughput(Owner, thenWindow)).Returns(SpreadOver(30, 15)).Returns(SpreadOver(30, 3));
            series.Setup(s => s.DailyArrivals(Owner, nowWindow)).Returns(SpreadOver(30, 6));
            series.Setup(s => s.DailyThroughput(Owner, nowWindow)).Returns(SpreadOver(30, 9));
            var subject = new ArrivalsReportMetric(series.Object);
            var frozen = subject.CaptureThen(Owner, thenWindow);

            var read = subject.Read(frozen, Owner, nowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen["total"].Value, Is.EqualTo(30), "Then must keep the started count it froze when the report was made");
                Assert.That(read.AtThen["gapPerDay"].Value, Is.EqualTo(0.5).Within(Tolerance), "Then must keep the gap it froze, not one rebuilt from today's data");
                Assert.That(read.AtNow["gapPerDay"].Value, Is.EqualTo(-0.1).Within(Tolerance), "Now must be computed afresh on every read");
            }
        }

        [Test]
        public void A_report_that_froze_nothing_for_arrivals_shows_no_Then_value_and_still_computes_Now()
        {
            var window = WindowOf(new DateOnly(2026, 10, 2), 30);
            var subject = new ArrivalsReportMetric(SeriesAnswering(Owner, (window, SpreadOver(30, 3), SpreadOver(30, 3))));

            var read = subject.Read(null, Owner, window);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen, Is.Empty, "an older report must read Not captured on Then, never a rebuilt value");
                Assert.That(read.AtNow.Keys, Is.EquivalentTo(Members), "Now must still be computed for a report that froze nothing");
            }
        }

        [Test]
        public void The_metric_is_named_arrivals_for_every_owner_kind()
        {
            var subject = new ArrivalsReportMetric(new Mock<IReportMetricSeries>(MockBehavior.Strict).Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Key, Is.EqualTo("arrivals"), "the wire key a client reads must be arrivals");
                Assert.That(subject.AppliesTo(ReportOwnerKind.Team), Is.True, "a Team report must be able to show Arrivals");
                Assert.That(subject.AppliesTo(ReportOwnerKind.Portfolio), Is.True, "a Portfolio report must be able to show Arrivals");
            }
        }

        private static readonly ReportOwner Owner = ReportOwner.Team(7);

        private static ReportWindow WindowOf(DateOnly end, int lengthDays) => ReportWindow.EndingOn(end, lengthDays)!;

        private static int[] SpreadOver(int lengthDays, int total)
            => [.. Enumerable.Range(0, lengthDays).Select(day => total / lengthDays + (day < total % lengthDays ? 1 : 0))];

        private static IReportMetricSeries SeriesAnswering(ReportOwner owner, params (ReportWindow Window, int[] Started, int[] Finished)[] answers)
        {
            var series = new Mock<IReportMetricSeries>(MockBehavior.Strict);
            foreach (var (window, started, finished) in answers)
            {
                series.Setup(s => s.DailyArrivals(owner, window)).Returns(started);
                series.Setup(s => s.DailyThroughput(owner, window)).Returns(finished);
            }

            return series.Object;
        }
    }
}
