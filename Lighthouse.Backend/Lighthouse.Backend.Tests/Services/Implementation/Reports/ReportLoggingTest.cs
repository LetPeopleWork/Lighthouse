using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class ReportLoggingTest
    {
        private const string SecretName = "Then & Now — the board's private name";

        private static readonly ReportOwner Lightspeed = ReportOwner.Team(42);

        private static readonly string[] LoggedRefusalFields = ["Reason", "OwnerKind", "OwnerId", "TemplateKey"];

        private static readonly string[] LoggedDeletionFields = ["ReportId", "OwnerKind", "OwnerId", "TemplateKey"];

        private const string NotInTheCatalog = "velocity";

        private static readonly string[] OnlyCycleTime = [CycleTimeReportMetric.MetricKey];

        private static readonly TimeZoneInfo InstanceZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        private FakeTimeProvider timeProvider = null!;
        private RecordingLogger logger = null!;
        private Report? stored;

        [SetUp]
        public void SetUp()
        {
            timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 2, 7, 0, 0, TimeSpan.Zero));
            logger = new RecordingLogger();
            stored = null;
        }

        [Test]
        public async Task A_create_logs_its_elapsed_time_and_never_the_name()
        {
            var created = await CreateTakingSeconds(3);

            var line = logger.Lines.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(line.Fields["ReportId"], Is.EqualTo(created.Id));
                Assert.That(line.Fields["OwnerKind"], Is.EqualTo(ReportOwnerKind.Team));
                Assert.That(line.Fields["OwnerId"], Is.EqualTo(42));
                Assert.That(line.Fields["TemplateKey"], Is.EqualTo(ThenAndNowTemplate.TemplateKey));
                Assert.That(line.Fields["ThenLengthDays"], Is.EqualTo(90));
                Assert.That(line.Fields["NowLengthDays"], Is.EqualTo(30));
                Assert.That(line.Fields["ElapsedMs"], Is.EqualTo(3000d));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
                Assert.That(line.Fields.Values, Has.None.EqualTo(SecretName));
            }
        }

        [Test]
        public async Task A_create_above_ten_seconds_is_a_report_creation_slow_warning()
        {
            await CreateTakingSeconds(11);

            var line = logger.Lines.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Warning));
                Assert.That(line.Message, Does.Contain("report-creation-slow"));
                Assert.That(line.Fields["ElapsedMs"], Is.EqualTo(11000d));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
            }
        }

        [Test]
        public async Task A_create_taking_exactly_ten_seconds_is_not_yet_slow()
        {
            await CreateTakingSeconds(10);

            Assert.That(logger.Lines.Single().Level, Is.EqualTo(LogLevel.Information));
        }

        [Test]
        public async Task A_create_without_a_Now_length_logs_Now_as_long_as_Then()
        {
            var outcome = await Subject(seconds: 0).Create(Lightspeed, new NewReport(
                ThenAndNowTemplate.TemplateKey, SecretName, ["cycle-time"], new NewThenAndNow(new DateOnly(2026, 10, 2), 90, null)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Done));
                Assert.That(logger.Lines.Single().Fields["NowLengthDays"], Is.EqualTo(90));
            }
        }

        [Test]
        public async Task A_refused_create_logs_its_reason_and_never_the_name_or_dates()
        {
            var thenEnd = new DateOnly(2026, 9, 17);

            var outcome = await Subject(seconds: 0).Create(Lightspeed, new NewReport(
                ThenAndNowTemplate.TemplateKey, SecretName, ["cycle-time"], new NewThenAndNow(thenEnd, 0, 30)));

            var line = logger.Lines.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(line.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(line.Fields.Keys.Where(key => key != "{OriginalFormat}"), Is.EquivalentTo(LoggedRefusalFields));
                Assert.That(line.Fields["Reason"], Is.EqualTo("then-too-short"));
                Assert.That(line.Fields["OwnerKind"], Is.EqualTo(ReportOwnerKind.Team));
                Assert.That(line.Fields["OwnerId"], Is.EqualTo(42));
                Assert.That(line.Fields["TemplateKey"], Is.EqualTo(ThenAndNowTemplate.TemplateKey));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
                Assert.That(line.Message, Does.Not.Contain(thenEnd.ToString()));
                Assert.That(line.Message, Does.Not.Contain("2026"));
            }
        }

        [Test]
        public async Task A_name_too_long_is_refused_with_its_reason_and_the_name_is_never_logged()
        {
            var tooLong = SecretName + new string('x', Report.LongestName);

            var outcome = await Subject(seconds: 0).Create(Lightspeed, new NewReport(
                ThenAndNowTemplate.TemplateKey, tooLong, ["cycle-time"], new NewThenAndNow(new DateOnly(2026, 10, 2), 90, 30)));

            var line = logger.Lines.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(stored, Is.Null);
                Assert.That(line.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(line.Fields["Reason"], Is.EqualTo("name-too-long"));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
            }
        }

        [Test]
        public async Task A_read_above_two_seconds_is_a_report_read_slow_warning()
        {
            await ReadTakingMilliseconds(2_001);

            var line = logger.Lines.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Warning));
                Assert.That(line.Message, Does.StartWith("report-read-slow"));
                Assert.That(line.Fields["ReportId"], Is.EqualTo(17));
                Assert.That(line.Fields["OwnerKind"], Is.EqualTo(ReportOwnerKind.Team));
                Assert.That(line.Fields["OwnerId"], Is.EqualTo(42));
                Assert.That(line.Fields["TemplateKey"], Is.EqualTo(ThenAndNowTemplate.TemplateKey));
                Assert.That(line.Fields["ElapsedMs"], Is.EqualTo(2001d));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
            }
        }

        [Test]
        public async Task A_read_below_two_seconds_logs_nothing()
        {
            await ReadTakingMilliseconds(2_000);

            Assert.That(logger.Lines, Is.Empty);
        }

        [Test]
        public async Task A_stored_metric_this_version_does_not_know_is_skipped_with_one_warning_naming_it()
        {
            await CreateTakingSeconds(0);
            stored!.ShownMetricKeysJson = $"[\"{CycleTimeReportMetric.MetricKey}\",\"{NotInTheCatalog}\"]";
            var payload = JsonNode.Parse(stored.TemplatePayloadJson)!.AsObject();
            payload["captures"]!.AsObject()[NotInTheCatalog] = new JsonObject();
            stored.TemplatePayloadJson = payload.ToJsonString();
            logger.Lines.Clear();

            var reader = new Mock<IReportReader>();
            reader.Setup(r => r.GetForOwner(Lightspeed, 17)).Returns(stored);
            var read = Queries(reader.Object).Get(Lightspeed, 17).Value!;

            var line = logger.Lines.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.ShownMetricKeys, Is.EqualTo(OnlyCycleTime));
                Assert.That(read.ThenAndNow!.Metrics.Keys, Is.EqualTo(OnlyCycleTime));
                Assert.That(line.Level, Is.EqualTo(LogLevel.Warning));
                Assert.That(line.Fields["MetricKey"], Is.EqualTo(NotInTheCatalog));
                Assert.That(line.Fields["ReportId"], Is.EqualTo(17));
                Assert.That(line.Fields["OwnerKind"], Is.EqualTo(ReportOwnerKind.Team));
                Assert.That(line.Fields["OwnerId"], Is.EqualTo(42));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
            }
        }

        [Test]
        public async Task A_report_deleted_logs_its_id_and_never_the_name()
        {
            await CreateTakingSeconds(0);
            logger.Lines.Clear();

            var outcome = await Subject(seconds: 0).Delete(Lightspeed, 17);

            var line = logger.Lines.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(ReportOutcomeStatus.Done));
                Assert.That(line.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(line.Fields.Keys.Where(key => key != "{OriginalFormat}"), Is.EquivalentTo(LoggedDeletionFields));
                Assert.That(line.Fields["ReportId"], Is.EqualTo(17));
                Assert.That(line.Fields["OwnerKind"], Is.EqualTo(ReportOwnerKind.Team));
                Assert.That(line.Fields["OwnerId"], Is.EqualTo(42));
                Assert.That(line.Fields["TemplateKey"], Is.EqualTo(ThenAndNowTemplate.TemplateKey));
                Assert.That(line.Message, Does.Not.Contain(SecretName));
            }
        }

        private async Task ReadTakingMilliseconds(int milliseconds)
        {
            await CreateTakingSeconds(0);
            logger.Lines.Clear();

            var reader = new Mock<IReportReader>();
            reader.Setup(r => r.GetForOwner(Lightspeed, 17))
                .Callback(() => timeProvider.Advance(TimeSpan.FromMilliseconds(milliseconds)))
                .Returns(stored);

            var outcome = Queries(reader.Object).Get(Lightspeed, 17);

            Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Done));
        }

        private async Task<ReportRead> CreateTakingSeconds(int seconds)
        {
            var outcome = await Subject(seconds).Create(Lightspeed, new NewReport(
                ThenAndNowTemplate.TemplateKey, SecretName, ["cycle-time"], new NewThenAndNow(new DateOnly(2026, 10, 2), 90, 30)));

            Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Done));
            return outcome.Value!;
        }

        private ReportCommands Subject(int seconds)
        {
            var clock = new FakeLighthouseClock(timeProvider.GetUtcNow(), InstanceZone);

            var writer = new Mock<IReportWriter>();
            writer.Setup(w => w.Add(It.IsAny<Report>())).Callback<Report>(report =>
            {
                report.Id = 17;
                stored = report;
            });
            writer.Setup(w => w.Remove(Lightspeed, 17)).Returns(() => stored);
            writer.Setup(w => w.Save()).Callback(() => timeProvider.Advance(TimeSpan.FromSeconds(seconds))).Returns(Task.CompletedTask);

            return new ReportCommands(writer.Object, Owners(), [Template(clock)], clock, timeProvider, logger);
        }

        private ReportQueries Queries(IReportReader reader)
        {
            var clock = new FakeLighthouseClock(timeProvider.GetUtcNow(), InstanceZone);
            return new ReportQueries(reader, Owners(), [Template(clock)], clock, timeProvider, logger);
        }

        private static IReportOwnerLookup Owners()
        {
            var owners = new Mock<IReportOwnerLookup>();
            owners.Setup(o => o.Find(Lightspeed)).Returns(new ReportOwnerFacts(365));
            return owners.Object;
        }

        private ThenAndNowTemplate Template(FakeLighthouseClock clock)
        {
            var series = new Mock<IReportMetricSeries>();
            series.Setup(s => s.CycleTimePercentiles(Lightspeed, It.IsAny<ReportWindow>())).Returns(new CycleTimePercentiles(6, 11, 21, 30, 20));
            return new ThenAndNowTemplate(new ReportMetricCatalog([new CycleTimeReportMetric(series.Object)]), clock, logger);
        }

        private sealed record LogLine(LogLevel Level, string Message, Dictionary<string, object?> Fields);

        private sealed class RecordingLogger : ILogger<ReportCommands>, ILogger<ReportQueries>, ILogger<ThenAndNowTemplate>
        {
            public List<LogLine> Lines { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var fields = state is IEnumerable<KeyValuePair<string, object?>> pairs
                    ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value)
                    : [];
                Lines.Add(new LogLine(logLevel, formatter(state, exception), fields));
            }
        }
    }
}
