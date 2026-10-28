using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class ReportBulkCommandsTest
    {
        private const string SecretName = "Then & Now — the board's private name";

        private static readonly ReportOwner Zenith = ReportOwner.Team(7);

        private static readonly ReportOwner Gravity = ReportOwner.Team(3);

        private static readonly ReportOwner OceanExplorer = ReportOwner.Portfolio(5);

        private static readonly NewReport TheQuarter = new(ThenAndNowTemplate.TemplateKey, SecretName, ["cycleTime"], new NewThenAndNow(new DateOnly(2026, 7, 29), 90, 30));

        private static readonly string[] RunFields = ["Action", "TemplateKey", "ThenLengthDays", "NowLengthDays", "Asked", "Done", "NotDone", "RefusedByCode", "ElapsedMs"];

        private static readonly DateOnly Today = new(2026, 10, 27);

        private Mock<IReportCommands> commands = null!;
        private Mock<IReportOwnerLookup> owners = null!;
        private Mock<IReportTemplate> thenAndNow = null!;
        private Mock<ILighthouseClock> clock = null!;
        private Mock<IReportOwnerWriteAccess> writeAccess = null!;
        private FakeTimeProvider timeProvider = null!;
        private RecordingLogger<ReportBulkCommands> logger = null!;
        private TimeSpan eachCreateTakes;

        [SetUp]
        public void SetUp()
        {
            timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 27, 7, 0, 0, TimeSpan.Zero));
            logger = new RecordingLogger<ReportBulkCommands>();
            eachCreateTakes = TimeSpan.Zero;
            var nextId = 100;
            commands = new Mock<IReportCommands>();
            commands.Setup(port => port.Create(It.IsAny<ReportOwner>(), It.IsAny<NewReport>()))
                .ReturnsAsync((ReportOwner owner, NewReport request) =>
                {
                    timeProvider.Advance(eachCreateTakes);
                    return ReportOutcome<ReportRead>.Done(new ReportRead(nextId++, request.Name, request.TemplateKey, owner.Kind, owner.Id, new DateOnly(2026, 10, 27)));
                });
            owners = new Mock<IReportOwnerLookup>();
            thenAndNow = new Mock<IReportTemplate>();
            thenAndNow.Setup(template => template.Key).Returns(ThenAndNowTemplate.TemplateKey);
            clock = new Mock<ILighthouseClock>();
            clock.Setup(instance => instance.Today).Returns(Today);
            writeAccess = new Mock<IReportOwnerWriteAccess>();
            writeAccess.Setup(access => access.MayWriteAsync(It.IsAny<ReportOwner>())).ReturnsAsync(true);
        }

        [Test]
        public async Task An_owner_gone_by_the_time_the_run_reaches_it_is_answered_owner_not_found_and_the_run_goes_on()
        {
            commands.Setup(port => port.Create(Zenith, It.IsAny<NewReport>())).ReturnsAsync(ReportOutcome<ReportRead>.NotFound());

            var outcomes = await Subject().CreateForEach([Zenith, Gravity], TheQuarter, writeAccess.Object);

            Assert.That(outcomes, Is.EqualTo(new[]
            {
                ReportOwnerOutcome.NotCreated(Zenith, ReportRefusalCodes.OwnerNotFound),
                ReportOwnerOutcome.Created(Gravity, 100),
            }));
        }

        [TestCase(ReportRefusalCodes.ThenBeforeCutoff)]
        [TestCase(ReportRefusalCodes.NowBeforeCutoff)]
        public async Task A_window_reaching_before_the_owners_kept_history_names_the_day_that_history_starts(string code)
        {
            owners.Setup(lookup => lookup.Find(Gravity)).Returns(new ReportOwnerFacts(90));
            commands.Setup(port => port.Create(Gravity, It.IsAny<NewReport>())).ReturnsAsync(ReportOutcome<ReportRead>.Refused(code));

            var outcomes = await Subject().CreateForEach([Zenith, Gravity], TheQuarter, writeAccess.Object);

            Assert.That(outcomes[1], Is.EqualTo(ReportOwnerOutcome.NotCreated(Gravity, code, new DateOnly(2026, 7, 29))));
        }

        [Test]
        public async Task Another_refusal_names_no_day_even_when_the_owner_keeps_a_short_history()
        {
            owners.Setup(lookup => lookup.Find(Gravity)).Returns(new ReportOwnerFacts(90));
            commands.Setup(port => port.Create(Gravity, It.IsAny<NewReport>())).ReturnsAsync(ReportOutcome<ReportRead>.Refused(ReportRefusalCodes.ReportCapReached));

            var outcomes = await Subject().CreateForEach([Gravity], TheQuarter, writeAccess.Object);

            Assert.That(outcomes.Single(), Is.EqualTo(ReportOwnerOutcome.NotCreated(Gravity, ReportRefusalCodes.ReportCapReached)));
        }

        [Test]
        public async Task The_right_to_write_an_owner_is_asked_when_the_run_reaches_it_not_before_the_run_starts()
        {
            var zenithCreated = false;
            commands.Setup(port => port.Create(Zenith, It.IsAny<NewReport>()))
                .Callback(() => zenithCreated = true)
                .ReturnsAsync(ReportOutcome<ReportRead>.Done(new ReportRead(100, SecretName, ThenAndNowTemplate.TemplateKey, Zenith.Kind, Zenith.Id, Today)));
            writeAccess.Setup(access => access.MayWriteAsync(Gravity)).ReturnsAsync(() => !zenithCreated);

            var outcomes = await Subject().CreateForEach([Zenith, Gravity], TheQuarter, writeAccess.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Is.EqualTo(new[]
                {
                    ReportOwnerOutcome.Created(Zenith, 100),
                    ReportOwnerOutcome.NotCreated(Gravity, ReportRefusalCodes.NotAdmin),
                }));
                commands.Verify(port => port.Create(Gravity, It.IsAny<NewReport>()), Times.Never);
            }
        }

        [Test]
        public async Task A_template_key_the_instance_does_not_offer_is_logged_as_unknown()
        {
            var forged = TheQuarter with { TemplateKey = "then-and-now\nreport-bulk-run: forged line" };

            await Subject().CreateForEach([Zenith], forged, writeAccess.Object);

            Assert.That(logger.Everything.Single().Properties["TemplateKey"], Is.EqualTo("unknown"));
        }

        [Test]
        public async Task Every_owner_is_answered_once_in_the_order_asked_through_the_single_create()
        {
            var outcomes = await Subject().CreateForEach([Zenith, OceanExplorer, Gravity], TheQuarter, MayWriteEvery());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Is.EqualTo(new[]
                {
                    ReportOwnerOutcome.Created(Zenith, 100),
                    ReportOwnerOutcome.Created(OceanExplorer, 101),
                    ReportOwnerOutcome.Created(Gravity, 102),
                }));
                commands.Verify(port => port.Create(Zenith, TheQuarter), Times.Once);
                commands.Verify(port => port.Create(OceanExplorer, TheQuarter), Times.Once);
                commands.Verify(port => port.Create(Gravity, TheQuarter), Times.Once);
                commands.VerifyNoOtherCalls();
            }
        }

        [Test]
        public async Task An_owner_the_caller_may_not_change_is_not_created_on_and_the_run_goes_on()
        {
            var writeAccess = new Mock<IReportOwnerWriteAccess>();
            writeAccess.Setup(access => access.MayWriteAsync(It.IsAny<ReportOwner>())).ReturnsAsync((ReportOwner owner) => owner != Zenith);

            var outcomes = await Subject().CreateForEach([Zenith, Gravity], TheQuarter, writeAccess.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Is.EqualTo(new[]
                {
                    ReportOwnerOutcome.NotCreated(Zenith, ReportRefusalCodes.NotAdmin),
                    ReportOwnerOutcome.Created(Gravity, 100),
                }));
                commands.Verify(port => port.Create(Zenith, It.IsAny<NewReport>()), Times.Never);
            }
        }

        [Test]
        public async Task A_refused_owner_carries_the_single_creates_reason()
        {
            commands.Setup(port => port.Create(Gravity, It.IsAny<NewReport>())).ReturnsAsync(ReportOutcome<ReportRead>.Refused(ReportRefusalCodes.ThenBeforeCutoff));

            var outcomes = await Subject().CreateForEach([Zenith, Gravity], TheQuarter, MayWriteEvery());

            Assert.That(outcomes[1], Is.EqualTo(ReportOwnerOutcome.NotCreated(Gravity, ReportRefusalCodes.ThenBeforeCutoff)));
        }

        [Test]
        public async Task A_run_logs_one_report_bulk_run_line_with_its_counts_and_never_a_name()
        {
            eachCreateTakes = TimeSpan.FromSeconds(2);
            commands.Setup(port => port.Create(Gravity, It.IsAny<NewReport>())).ReturnsAsync(ReportOutcome<ReportRead>.Refused(ReportRefusalCodes.ThenBeforeCutoff));

            await Subject().CreateForEach([Zenith, Gravity, OceanExplorer], TheQuarter, MayWriteEvery());

            var line = logger.Everything.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(line.Message, Does.StartWith("report-bulk-run:"));
                Assert.That(line.Properties.Keys, Is.EquivalentTo(RunFields));
                Assert.That(line.Properties["Action"], Is.EqualTo("create"));
                Assert.That(line.Properties["TemplateKey"], Is.EqualTo(ThenAndNowTemplate.TemplateKey));
                Assert.That(line.Properties["ThenLengthDays"], Is.EqualTo(90));
                Assert.That(line.Properties["NowLengthDays"], Is.EqualTo(30));
                Assert.That(line.Properties["Asked"], Is.EqualTo(3));
                Assert.That(line.Properties["Done"], Is.EqualTo(2));
                Assert.That(line.Properties["NotDone"], Is.EqualTo(1));
                Assert.That(line.Properties["RefusedByCode"], Is.EqualTo(new Dictionary<string, int> { [ReportRefusalCodes.ThenBeforeCutoff] = 1 }));
                Assert.That(line.Properties["ElapsedMs"], Is.EqualTo(4000d));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
                Assert.That(line.Properties.Values, Has.None.EqualTo(SecretName));
            }
        }

        [Test]
        public async Task A_creation_run_above_twenty_seconds_is_a_report_bulk_run_slow_warning()
        {
            eachCreateTakes = TimeSpan.FromMilliseconds(10_001);

            await Subject().CreateForEach([Zenith, Gravity], TheQuarter, MayWriteEvery());

            var line = logger.Everything.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Warning));
                Assert.That(line.Message, Does.StartWith("report-bulk-run-slow:"));
                Assert.That(line.Properties.Keys, Is.EquivalentTo(RunFields));
                Assert.That(line.Properties["ElapsedMs"], Is.EqualTo(20_002d));
            }
        }

        [Test]
        public async Task A_creation_run_of_exactly_twenty_seconds_is_not_yet_slow()
        {
            eachCreateTakes = TimeSpan.FromSeconds(10);

            await Subject().CreateForEach([Zenith, Gravity], TheQuarter, MayWriteEvery());

            Assert.That(logger.Everything.Single().Level, Is.EqualTo(LogLevel.Information));
        }

        private ReportBulkCommands Subject() => new(commands.Object, owners.Object, [thenAndNow.Object], clock.Object, timeProvider, logger);

        private static IReportOwnerWriteAccess MayWriteEvery()
        {
            var writeAccess = new Mock<IReportOwnerWriteAccess>();
            writeAccess.Setup(access => access.MayWriteAsync(It.IsAny<ReportOwner>())).ReturnsAsync(true);
            return writeAccess.Object;
        }
    }
}
