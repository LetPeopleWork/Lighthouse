using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class CycleTimeReportMetricTest
    {
        private static readonly ReportOwner Owner = ReportOwner.Team(7);

        private static readonly ReportWindow ThenWindow = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

        private static readonly ReportWindow NowWindow = new(new DateOnly(2026, 8, 2), new DateOnly(2026, 10, 30), 90);

        private static readonly string[] NowMembers = ["p85", "p70", "p50"];

        private static readonly string[] ThenMembers = ["p85"];

        [TestCase(20, 21, 11, 6)]
        [TestCase(3, 18, 18, 9)]
        [TestCase(1, 4, 4, 4)]
        public void Then_freezes_the_85th_and_Now_shows_the_85th_70th_and_50th_each_with_its_sample_size(int finishedItems, int p85, int p70, int p50)
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering(
                (ThenWindow, new CycleTimePercentiles(p50, p70, p85, finishedItems)),
                (NowWindow, new CycleTimePercentiles(p50 + 1, p70 + 2, p85 + 3, finishedItems + 4))));

            var read = subject.Read(subject.CaptureThen(Owner, ThenWindow), Owner, NowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Keys, Is.EquivalentTo(ThenMembers));
                Assert.That(read.AtThen["p85"], Is.EqualTo(CapturedValue.Shown(p85, finishedItems)));
                Assert.That(read.AtNow.Keys, Is.EquivalentTo(NowMembers));
                Assert.That(read.AtNow["p85"], Is.EqualTo(CapturedValue.Shown(p85 + 3, finishedItems + 4)));
                Assert.That(read.AtNow["p70"], Is.EqualTo(CapturedValue.Shown(p70 + 2, finishedItems + 4)));
                Assert.That(read.AtNow["p50"], Is.EqualTo(CapturedValue.Shown(p50 + 1, finishedItems + 4)));
            }
        }

        [Test]
        public void A_window_without_finished_items_is_a_dash_with_its_reason_never_a_zero()
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering(
                (ThenWindow, new CycleTimePercentiles(0, 0, 0, 0)),
                (NowWindow, new CycleTimePercentiles(0, 0, 0, 0))));

            var read = subject.Read(subject.CaptureThen(Owner, ThenWindow), Owner, NowWindow);

            var nothingToComputeFrom = CapturedValue.Absent(CycleTimeReportMetric.NoFinishedItems);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen["p85"], Is.EqualTo(nothingToComputeFrom));
                Assert.That(read.AtNow.Values, Is.All.EqualTo(nothingToComputeFrom));
            }
        }

        [Test]
        public void A_report_that_froze_nothing_for_the_metric_shows_no_Then_value_and_still_computes_Now()
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering((NowWindow, new CycleTimePercentiles(6, 9, 12, 10))));

            var read = subject.Read(null, Owner, NowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen, Is.Empty);
                Assert.That(read.AtNow["p85"], Is.EqualTo(CapturedValue.Shown(12, 10)));
            }
        }

        private static IReportMetricSeries SeriesAnswering(params (ReportWindow Window, CycleTimePercentiles Answer)[] answers)
        {
            var series = new Mock<IReportMetricSeries>(MockBehavior.Strict);
            foreach (var (window, answer) in answers)
            {
                series.Setup(s => s.CycleTimePercentiles(Owner, window)).Returns(answer);
            }

            return series.Object;
        }
    }
}
