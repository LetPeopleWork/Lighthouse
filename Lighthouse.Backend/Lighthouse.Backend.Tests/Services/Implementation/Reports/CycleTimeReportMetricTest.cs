using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class CycleTimeReportMetricTest
    {
        private const string PendingSlice05b = "Epic #5878 slice 05b (#6163) - pending DELIVER";

        private static readonly ReportOwner Owner = ReportOwner.Team(7);

        private static readonly ReportWindow ThenWindow = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

        private static readonly ReportWindow NowWindow = new(new DateOnly(2026, 8, 2), new DateOnly(2026, 10, 30), 90);

        private static readonly string[] Percentiles = ["p85", "p70", "p50"];

        private static readonly string[] AllFourPercentiles = ["p95", "p85", "p70", "p50"];

        private static readonly string[] OnlyThe85th = ["p85"];

        private const string ReportStoredBeforeThenKeptThe70thAnd50th =
            """{"thenWindow":{"start":"2026-07-05","end":"2026-10-02","lengthDays":90},"nowLengthDays":90,"captures":{"cycle-time":{"p85":{"value":21,"sampleSize":20,"absentReason":null}}}}""";

        private const string ReportStoredBeforeThenKeptThe95th =
            """{"thenWindow":{"start":"2026-07-05","end":"2026-10-02","lengthDays":90},"nowLengthDays":90,"captures":{"cycle-time":{"p85":{"value":21,"sampleSize":20,"absentReason":null},"p70":{"value":11,"sampleSize":20,"absentReason":null},"p50":{"value":6,"sampleSize":20,"absentReason":null}}}}""";

        [TestCase(20, 21, 11, 6)]
        [TestCase(3, 18, 18, 9)]
        [TestCase(1, 4, 4, 4)]
        public void Then_freezes_and_Now_shows_the_85th_70th_and_50th_each_with_its_sample_size(int finishedItems, int p85, int p70, int p50)
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering(
                (ThenWindow, new Answer(p85 + 5, p85, p70, p50, finishedItems)),
                (NowWindow, new Answer(p85 + 9, p85 + 3, p70 + 2, p50 + 1, finishedItems + 4))));

            var read = subject.Read(subject.CaptureThen(Owner, ThenWindow), Owner, NowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Keys, Is.EquivalentTo(Percentiles));
                Assert.That(read.AtThen["p85"], Is.EqualTo(CapturedValue.Shown(p85, finishedItems)));
                Assert.That(read.AtThen["p70"], Is.EqualTo(CapturedValue.Shown(p70, finishedItems)));
                Assert.That(read.AtThen["p50"], Is.EqualTo(CapturedValue.Shown(p50, finishedItems)));
                Assert.That(read.AtNow.Keys, Is.EquivalentTo(Percentiles));
                Assert.That(read.AtNow["p85"], Is.EqualTo(CapturedValue.Shown(p85 + 3, finishedItems + 4)));
                Assert.That(read.AtNow["p70"], Is.EqualTo(CapturedValue.Shown(p70 + 2, finishedItems + 4)));
                Assert.That(read.AtNow["p50"], Is.EqualTo(CapturedValue.Shown(p50 + 1, finishedItems + 4)));
            }
        }

        [TestCase(20, 30, 21, 11, 6)]
        [TestCase(3, 25, 18, 18, 9)]
        [TestCase(1, 4, 4, 4, 4)]
        [Ignore(PendingSlice05b)]
        public void Then_freezes_and_Now_shows_the_95th_beside_the_others_on_the_same_sample_size(int finishedItems, int p95, int p85, int p70, int p50)
        {
            var then = new Answer(p95, p85, p70, p50, finishedItems);
            var now = new Answer(p95 + 2, p85 + 1, p70, p50, finishedItems + 4);
            var subject = new CycleTimeReportMetric(SeriesAnswering((ThenWindow, then), (NowWindow, now)));

            var read = subject.Read(subject.CaptureThen(Owner, ThenWindow), Owner, NowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Keys, Is.EquivalentTo(AllFourPercentiles));
                Assert.That(read.AtThen["p95"], Is.EqualTo(CapturedValue.Shown(then.P95, then.FinishedItems)));
                Assert.That(read.AtThen["p85"], Is.EqualTo(CapturedValue.Shown(then.P85, then.FinishedItems)));
                Assert.That(read.AtNow.Keys, Is.EquivalentTo(AllFourPercentiles));
                Assert.That(read.AtNow["p95"], Is.EqualTo(CapturedValue.Shown(now.P95, now.FinishedItems)));
                Assert.That(read.AtNow["p85"], Is.EqualTo(CapturedValue.Shown(now.P85, now.FinishedItems)));
            }
        }

        [Test]
        public void A_window_without_finished_items_is_a_dash_with_its_reason_never_a_zero()
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering(
                (ThenWindow, new Answer(0, 0, 0, 0, 0)),
                (NowWindow, new Answer(0, 0, 0, 0, 0))));

            var read = subject.Read(subject.CaptureThen(Owner, ThenWindow), Owner, NowWindow);

            var nothingToComputeFrom = CapturedValue.Absent(CycleTimeReportMetric.NoFinishedItems);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Keys, Is.EquivalentTo(Percentiles));
                Assert.That(read.AtThen.Values, Is.All.EqualTo(nothingToComputeFrom));
                Assert.That(read.AtNow.Values, Is.All.EqualTo(nothingToComputeFrom));
            }
        }

        [Test]
        [Ignore(PendingSlice05b)]
        public void A_window_without_finished_items_shows_the_95th_as_a_dash_with_its_reason_too()
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering(
                (ThenWindow, new Answer(0, 0, 0, 0, 0)),
                (NowWindow, new Answer(0, 0, 0, 0, 0))));

            var read = subject.Read(subject.CaptureThen(Owner, ThenWindow), Owner, NowWindow);

            var nothingToComputeFrom = CapturedValue.Absent(CycleTimeReportMetric.NoFinishedItems);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen["p95"], Is.EqualTo(nothingToComputeFrom));
                Assert.That(read.AtNow["p95"], Is.EqualTo(nothingToComputeFrom));
            }
        }

        [Test]
        public void A_report_that_froze_nothing_for_the_metric_shows_no_Then_value_and_still_computes_Now()
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering((NowWindow, new Answer(14, 12, 9, 6, 10))));

            var read = subject.Read(null, Owner, NowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen, Is.Empty);
                Assert.That(read.AtNow["p85"], Is.EqualTo(CapturedValue.Shown(12, 10)));
            }
        }

        [Test]
        public void A_report_stored_when_Then_kept_only_the_85th_still_opens_with_the_70th_and_50th_not_captured()
        {
            var subject = new CycleTimeReportMetric(SeriesAnswering((NowWindow, new Answer(14, 12, 9, 6, 10))));
            var stored = ThenAndNowPayload.FromJson(ReportStoredBeforeThenKeptThe70thAnd50th)!;

            var read = subject.Read(stored.Captures![CycleTimeReportMetric.MetricKey], Owner, NowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Keys, Is.EquivalentTo(OnlyThe85th));
                Assert.That(read.AtThen["p85"], Is.EqualTo(CapturedValue.Shown(21, 20)));
                Assert.That(read.AtNow.Keys, Is.EquivalentTo(Percentiles));
            }
        }

        [Test]
        [Ignore(PendingSlice05b)]
        public void A_report_stored_before_Then_kept_the_95th_opens_with_Thens_95th_not_captured_and_Nows_shown()
        {
            var now = new Answer(14, 12, 9, 6, 10);
            var subject = new CycleTimeReportMetric(SeriesAnswering((NowWindow, now)));
            var stored = ThenAndNowPayload.FromJson(ReportStoredBeforeThenKeptThe95th)!;

            var read = subject.Read(stored.Captures![CycleTimeReportMetric.MetricKey], Owner, NowWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.AtThen.Keys, Is.EquivalentTo(Percentiles), "nothing is added to what Then froze, not even a dash");
                Assert.That(read.AtThen["p85"], Is.EqualTo(CapturedValue.Shown(21, 20)));
                Assert.That(read.AtNow.Keys, Is.EquivalentTo(AllFourPercentiles));
                Assert.That(read.AtNow["p95"], Is.EqualTo(CapturedValue.Shown(now.P95, now.FinishedItems)));
            }
        }

        private static IReportMetricSeries SeriesAnswering(params (ReportWindow Window, Answer Answer)[] answers)
        {
            var series = new Mock<IReportMetricSeries>(MockBehavior.Strict);
            foreach (var (window, answer) in answers)
            {
                series.Setup(s => s.CycleTimePercentiles(Owner, window)).Returns(answer.AsTheSeriesGivesIt());
            }

            return series.Object;
        }

        /// <summary>What the series answers for one window, from the 95th down, and how many finished items it rests on.</summary>
        private sealed record Answer(int P95, int P85, int P70, int P50, int FinishedItems)
        {
            // The series does not carry the 95th yet; once it does, it is handed on here like the other three.
            public CycleTimePercentiles AsTheSeriesGivesIt() => new(P50, P70, P85, FinishedItems);
        }
    }
}
