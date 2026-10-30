using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class ReportBulkRemoverTest
    {
        private static readonly ReportOwner Zenith = ReportOwner.Team(7);

        private static readonly ReportOwner Gravity = ReportOwner.Team(3);

        private static readonly ReportOwner OceanExplorer = ReportOwner.Portfolio(5);

        private static readonly string[] RemovalRunFields = ["Action", "Asked", "Done", "NotDone", "RefusedByCode", "ElapsedMs"];

        private const int OnZenith = 40;

        private const int OnGravity = 41;

        private const int OnOceanExplorer = 42;

        private Mock<IReportCommands> commands = null!;
        private Mock<IReportReader> reader = null!;
        private Mock<IReportWriter> writer = null!;
        private Mock<IReportOwnerWriteAccess> writeAccess = null!;
        private FakeTimeProvider timeProvider = null!;
        private RecordingLogger<ReportBulkRemover> logger = null!;
        private TimeSpan eachDeleteTakes;

        [SetUp]
        public void SetUp()
        {
            timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 27, 7, 0, 0, TimeSpan.Zero));
            logger = new RecordingLogger<ReportBulkRemover>();
            eachDeleteTakes = TimeSpan.Zero;
            commands = new Mock<IReportCommands>();
            commands.Setup(port => port.Delete(It.IsAny<ReportOwner>(), It.IsAny<int>()))
                .ReturnsAsync(() =>
                {
                    timeProvider.Advance(eachDeleteTakes);
                    return ReportOutcomeStatus.Done;
                });
            reader = new Mock<IReportReader>();
            reader.Setup(port => port.OwnerOf(OnZenith)).Returns(Zenith);
            reader.Setup(port => port.OwnerOf(OnGravity)).Returns(Gravity);
            reader.Setup(port => port.OwnerOf(OnOceanExplorer)).Returns(OceanExplorer);
            writer = new Mock<IReportWriter>();
            writeAccess = new Mock<IReportOwnerWriteAccess>();
            writeAccess.Setup(access => access.MayWriteAsync(It.IsAny<ReportOwner>())).ReturnsAsync(true);
        }

        [Test]
        public async Task Every_report_is_answered_once_in_the_order_asked_and_removed_from_the_owner_the_server_found_for_it()
        {
            var outcomes = await Subject().RemoveEach([OnOceanExplorer, OnZenith, OnGravity], MayWriteEvery());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Is.EqualTo(new[]
                {
                    ReportRemovalOutcome.Removed(OnOceanExplorer),
                    ReportRemovalOutcome.Removed(OnZenith),
                    ReportRemovalOutcome.Removed(OnGravity),
                }));
                commands.Verify(port => port.Delete(OceanExplorer, OnOceanExplorer), Times.Once);
                commands.Verify(port => port.Delete(Zenith, OnZenith), Times.Once);
                commands.Verify(port => port.Delete(Gravity, OnGravity), Times.Once);
            }
        }

        [Test]
        public async Task A_report_named_twice_is_answered_once_where_it_was_first_named_and_removed_once()
        {
            var outcomes = await Subject().RemoveEach([OnZenith, OnGravity, OnZenith], MayWriteEvery());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Is.EqualTo(new[]
                {
                    ReportRemovalOutcome.Removed(OnZenith),
                    ReportRemovalOutcome.Removed(OnGravity),
                }));
                commands.Verify(port => port.Delete(Zenith, OnZenith), Times.Once);
            }
        }

        [Test]
        public async Task A_report_on_an_owner_the_caller_may_not_change_is_not_removed_and_the_run_goes_on()
        {
            writeAccess.Setup(access => access.MayWriteAsync(Gravity)).ReturnsAsync(false);

            var outcomes = await Subject().RemoveEach([OnGravity, OnZenith], writeAccess.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Is.EqualTo(new[]
                {
                    ReportRemovalOutcome.NotRemoved(OnGravity, ReportRefusalCodes.NotAdmin),
                    ReportRemovalOutcome.Removed(OnZenith),
                }));
                commands.Verify(port => port.Delete(Gravity, It.IsAny<int>()), Times.Never);
            }
        }

        [Test]
        public async Task A_report_already_gone_is_answered_removed_as_already_gone_without_asking_for_Admin_on_anyone()
        {
            const int Unknown = 99;

            var outcomes = await Subject().RemoveEach([Unknown], writeAccess.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Single(), Is.EqualTo(ReportRemovalOutcome.Removed(Unknown, ReportRefusalCodes.AlreadyGone)));
                writeAccess.Verify(access => access.MayWriteAsync(It.IsAny<ReportOwner>()), Times.Never);
                commands.Verify(port => port.Delete(It.IsAny<ReportOwner>(), It.IsAny<int>()), Times.Never);
            }
        }

        [Test]
        public async Task A_report_removed_by_somebody_else_after_its_owner_was_found_is_answered_removed_as_already_gone_and_the_run_goes_on()
        {
            commands.Setup(port => port.Delete(Zenith, OnZenith)).ReturnsAsync(ReportOutcomeStatus.NotFound);

            var outcomes = await Subject().RemoveEach([OnZenith, OnGravity], writeAccess.Object);

            Assert.That(outcomes, Is.EqualTo(new[]
            {
                ReportRemovalOutcome.Removed(OnZenith, ReportRefusalCodes.AlreadyGone),
                ReportRemovalOutcome.Removed(OnGravity),
            }));
        }

        [Test]
        public async Task A_report_whose_removal_fails_to_save_is_answered_not_saved_its_pending_change_is_discarded_and_the_run_goes_on()
        {
            const string DatabaseMessage = "constraint failed on host db.internal";
            commands.Setup(port => port.Delete(Zenith, OnZenith)).ThrowsAsync(new DbUpdateException(DatabaseMessage, new InvalidOperationException(DatabaseMessage)));
            writer.Setup(port => port.DiscardPendingChanges())
                .Callback(() => commands.Verify(port => port.Delete(Gravity, It.IsAny<int>()), Times.Never));

            var outcomes = await Subject().RemoveEach([OnZenith, OnGravity], writeAccess.Object);

            var warning = logger.Everything.Single(line => line.Level == LogLevel.Warning);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes, Is.EqualTo(new[]
                {
                    ReportRemovalOutcome.NotRemoved(OnZenith, ReportRefusalCodes.NotSaved),
                    ReportRemovalOutcome.Removed(OnGravity),
                }));
                writer.Verify(port => port.DiscardPendingChanges(), Times.Once);
                Assert.That(warning.Properties["ReportId"], Is.EqualTo(OnZenith));
                Assert.That(warning.Properties["ExceptionType"], Is.EqualTo(nameof(DbUpdateException)));
                Assert.That(warning.Properties["InnerExceptionType"], Is.EqualTo(nameof(InvalidOperationException)));
                Assert.That(warning.Message, Does.Not.Contain(DatabaseMessage));
            }
        }

        [Test]
        public async Task A_removal_run_logs_one_report_bulk_run_line_with_its_counts()
        {
            eachDeleteTakes = TimeSpan.FromSeconds(2);
            writeAccess.Setup(access => access.MayWriteAsync(Gravity)).ReturnsAsync(false);

            await Subject().RemoveEach([OnZenith, OnGravity, OnOceanExplorer], writeAccess.Object);

            var line = logger.Everything.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(line.Message, Does.StartWith("report-bulk-run:"));
                Assert.That(line.Properties.Keys, Is.EquivalentTo(RemovalRunFields));
                Assert.That(line.Properties["Action"], Is.EqualTo("remove"));
                Assert.That(line.Properties["Asked"], Is.EqualTo(3));
                Assert.That(line.Properties["Done"], Is.EqualTo(2));
                Assert.That(line.Properties["NotDone"], Is.EqualTo(1));
                Assert.That(line.Properties["RefusedByCode"], Is.EqualTo(new Dictionary<string, int> { [ReportRefusalCodes.NotAdmin] = 1 }));
                Assert.That(line.Properties["ElapsedMs"], Is.EqualTo(4000d));
            }
        }

        [Test]
        public async Task A_removal_run_above_ten_seconds_is_a_report_bulk_run_slow_warning()
        {
            eachDeleteTakes = TimeSpan.FromMilliseconds(5_001);

            await Subject().RemoveEach([OnZenith, OnGravity], MayWriteEvery());

            var line = logger.Everything.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Warning));
                Assert.That(line.Message, Does.StartWith("report-bulk-run-slow:"));
                Assert.That(line.Properties.Keys, Is.EquivalentTo(RemovalRunFields));
                Assert.That(line.Properties["Action"], Is.EqualTo("remove"));
                Assert.That(line.Properties["ElapsedMs"], Is.EqualTo(10_002d));
            }
        }

        [Test]
        public async Task A_removal_run_of_exactly_ten_seconds_is_not_yet_slow()
        {
            eachDeleteTakes = TimeSpan.FromSeconds(5);

            await Subject().RemoveEach([OnZenith, OnGravity], MayWriteEvery());

            Assert.That(logger.Everything.Single().Level, Is.EqualTo(LogLevel.Information));
        }

        private ReportBulkRemover Subject() => new(commands.Object, reader.Object, writer.Object, timeProvider, logger);

        private static IReportOwnerWriteAccess MayWriteEvery()
        {
            var writeAccess = new Mock<IReportOwnerWriteAccess>();
            writeAccess.Setup(access => access.MayWriteAsync(It.IsAny<ReportOwner>())).ReturnsAsync(true);
            return writeAccess.Object;
        }
    }
}
