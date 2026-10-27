using System.Text.Json;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    [TestFixture]
    public class ReportQueriesTest
    {
        private const int KeepsAYear = 365;

        private static readonly ReportOwner Lightspeed = ReportOwner.Team(42);

        private static readonly ReportOwner Apollo = ReportOwner.Portfolio(42);

        private static readonly DateTimeOffset Now = new(2026, 10, 2, 7, 0, 0, TimeSpan.Zero);

        private static readonly ReportWindow ThenInsideTheKeptYear = new(new DateOnly(2026, 7, 5), new DateOnly(2026, 10, 2), 90);

        private static readonly ReportWindow ThenBeforeTheKeptYear = new(new DateOnly(2025, 7, 5), new DateOnly(2025, 10, 2), 90);

        private static readonly string[] CycleTimeShown = [CycleTimeReportMetric.MetricKey];

        private readonly List<Report> stored = [];

        private Mock<ILogger<ReportQueries>> logger = null!;

        [SetUp]
        public void SetUp()
        {
            stored.Clear();
            logger = new Mock<ILogger<ReportQueries>>();
        }

        [Test]
        public void A_listed_report_says_whether_its_Then_could_still_be_rebuilt_from_what_the_Team_keeps()
        {
            var rebuildable = Stored(PayloadOf(ThenInsideTheKeptYear));
            var beyondTheKeptYear = Stored(PayloadOf(ThenBeforeTheKeptYear));

            var summaries = Subject().List(Lightspeed).Value!.Reports.ToDictionary(summary => summary.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summaries[rebuildable.Id].ThenAndNow, Is.EqualTo(new ThenAndNowSummary(true)));
                Assert.That(summaries[beyondTheKeptYear.Id].ThenAndNow, Is.EqualTo(new ThenAndNowSummary(false)));
            }
        }

        [Test]
        public void A_listed_report_holding_no_Then_window_says_nothing_about_Then()
        {
            var withoutThen = Stored("{}");

            var summary = Subject().List(Lightspeed).Value!.Reports.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.Id, Is.EqualTo(withoutThen.Id));
                Assert.That(summary.ThenAndNow, Is.Null);
            }
        }

        [Test]
        public void A_listed_report_this_version_cannot_read_is_still_listed_and_logged()
        {
            var unreadable = Stored("this is not a payload");

            var summary = Subject().List(Lightspeed).Value!.Reports.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary, Is.EqualTo(new ReportSummary(unreadable.Id, unreadable.Name, ThenAndNowTemplate.TemplateKey, ReportOwnerKind.Team, 42, new DateOnly(2026, 10, 2))));
                logger.Verify(log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<JsonException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
            }
        }

        [Test]
        public void An_opened_report_shows_the_metrics_its_row_names()
        {
            var report = Stored(PayloadOf(ThenInsideTheKeptYear));
            report.ShownMetricKeysJson = JsonSerializer.Serialize(CycleTimeShown);

            var read = Subject().Get(Lightspeed, report.Id).Value!;

            Assert.That(read.ShownMetricKeys, Is.EqualTo(CycleTimeShown));
        }

        [Test]
        public void An_opened_report_whose_row_names_no_metrics_shows_none()
        {
            var report = Stored(PayloadOf(ThenInsideTheKeptYear));
            report.ShownMetricKeysJson = "null";

            var read = Subject().Get(Lightspeed, report.Id).Value!;

            Assert.That(read.ShownMetricKeys, Is.Empty);
        }

        [Test]
        public void Reports_across_owners_name_their_owner_and_come_Teams_before_Portfolios_then_by_owner_name()
        {
            var ofApollo = Stored(PayloadOf(ThenInsideTheKeptYear), Apollo);
            var ofLightspeed = Stored(PayloadOf(ThenInsideTheKeptYear));
            var unreadable = Stored(PayloadOf(ThenInsideTheKeptYear), ReportOwner.Team(99));

            var listed = Subject().ListAcross([Apollo, Lightspeed]).Value!.Reports;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed.Select(summary => (summary.Id, summary.OwnerKind, summary.OwnerName)),
                    Is.EqualTo(new[] { (ofLightspeed.Id, ReportOwnerKind.Team, "Lightspeed"), (ofApollo.Id, ReportOwnerKind.Portfolio, "Apollo") }));
                Assert.That(listed.Select(summary => summary.Id), Does.Not.Contain(unreadable.Id));
            }
        }

        [Test]
        public void Reports_across_no_readable_owner_are_an_empty_list()
        {
            Stored(PayloadOf(ThenInsideTheKeptYear));

            Assert.That(Subject().ListAcross([]).Value!.Reports, Is.Empty);
        }

        private Report Stored(string payloadJson, ReportOwner? owner = null)
        {
            var report = Report.For(owner ?? Lightspeed, ThenAndNowTemplate.TemplateKey, $"Report {stored.Count + 1}", Now.UtcDateTime);
            report.Id = stored.Count + 1;
            report.TemplatePayloadJson = payloadJson;
            stored.Add(report);
            return report;
        }

        private static string PayloadOf(ReportWindow thenWindow)
            => new ThenAndNowPayload { ThenWindow = thenWindow, NowLengthDays = 30 }.ToJson();

        private ReportQueries Subject()
        {
            var clock = new FakeLighthouseClock(Now);

            var reader = new Mock<IReportReader>();
            reader.Setup(r => r.ListForOwner(Lightspeed)).Returns(() => stored);
            reader.Setup(r => r.GetForOwner(Lightspeed, It.IsAny<int>())).Returns((ReportOwner _, int id) => stored.Find(report => report.Id == id));

            var owners = new Mock<IReportOwnerLookup>();
            owners.Setup(o => o.Find(Lightspeed)).Returns(new ReportOwnerFacts(KeepsAYear));
            owners.Setup(o => o.All()).Returns([new ReportOwnerRow(Apollo, "Apollo", KeepsAYear), new ReportOwnerRow(Lightspeed, "Lightspeed", KeepsAYear)]);
            reader.Setup(r => r.ListForOwners(It.IsAny<IReadOnlyCollection<ReportOwner>>()))
                .Returns((IReadOnlyCollection<ReportOwner> asked) => [.. stored.Where(report => asked.Contains(report.OwnedBy()))]);

            var series = new Mock<IReportMetricSeries>();
            series.Setup(s => s.CycleTimePercentiles(Lightspeed, It.IsAny<ReportWindow>())).Returns(new CycleTimePercentiles(6, 11, 21, 30, 20));
            var template = new ThenAndNowTemplate(new ReportMetricCatalog([new CycleTimeReportMetric(series.Object)]), clock, NullLogger<ThenAndNowTemplate>.Instance);

            return new ReportQueries(reader.Object, owners.Object, [template], Mock.Of<ILicenseService>(), clock, new FakeTimeProvider(Now), logger.Object);
        }
    }
}
