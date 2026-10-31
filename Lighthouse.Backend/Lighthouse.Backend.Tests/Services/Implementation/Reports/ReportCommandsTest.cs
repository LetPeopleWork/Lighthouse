using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    [TestFixture]
    public class ReportCommandsTest
    {
        private const string TemplateKey = "then-and-now";

        private const string StoredName = "Engagement start";

        private const string StoredPayload = "{\"stored\":true}";

        private static readonly ReportOwner Lightspeed = ReportOwner.Team(42);

        private static readonly ReportOutcome<string> DoneWithoutAPayload = new(ReportOutcomeStatus.Done, null, null);

        private Mock<IReportWriter> writer = null!;
        private Mock<IReportTemplate> template = null!;

        [SetUp]
        public void SetUp()
        {
            writer = new Mock<IReportWriter>();
            template = new Mock<IReportTemplate>();
            template.Setup(t => t.Key).Returns(TemplateKey);
        }

        [Test]
        public async Task A_template_that_freezes_no_payload_is_refused_and_nothing_is_stored()
        {
            template.Setup(t => t.Freeze(Lightspeed, It.IsAny<NewReport>(), It.IsAny<ReportCalendar>())).Returns(DoneWithoutAPayload);

            var outcome = await Subject().Create(Lightspeed, new NewReport(TemplateKey, StoredName, ["cycle-time"], null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(outcome.Code, Is.EqualTo(ReportRefusalCodes.TemplateNotSupported));
                writer.Verify(w => w.Add(It.IsAny<Report>()), Times.Never);
                writer.Verify(w => w.Save(), Times.Never);
            }
        }

        [Test]
        public async Task A_template_that_edits_to_no_payload_is_refused_and_the_report_is_left_as_it_was()
        {
            var report = Report.For(Lightspeed, TemplateKey, StoredName, new DateTime(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc));
            report.TemplatePayloadJson = StoredPayload;
            writer.Setup(w => w.FindForEdit(Lightspeed, 17)).Returns(report);
            template.Setup(t => t.Edit(report, It.IsAny<ReportEdit>(), It.IsAny<ReportCalendar>())).Returns(DoneWithoutAPayload);

            var outcome = await Subject().Edit(Lightspeed, 17, new ReportEdit("Renamed", ["cycle-time"], report.ConcurrencyToken, 30));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(outcome.Code, Is.EqualTo(ReportRefusalCodes.TemplateNotSupported));
                Assert.That(report.Name, Is.EqualTo(StoredName));
                Assert.That(report.TemplatePayloadJson, Is.EqualTo(StoredPayload));
                writer.Verify(w => w.Save(), Times.Never);
            }
        }

        private ReportCommands Subject()
        {
            var owners = new Mock<IReportOwnerLookup>();
            owners.Setup(o => o.Find(Lightspeed)).Returns(new ReportOwnerFacts("Lightspeed", 365));
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 2, 7, 0, 0, TimeSpan.Zero));
            var clock = new FakeLighthouseClock(timeProvider.GetUtcNow());

            return new ReportCommands(writer.Object, owners.Object, [template.Object], Mock.Of<ILicenseService>(), clock, timeProvider, NullLogger<ReportCommands>.Instance);
        }
    }
}
