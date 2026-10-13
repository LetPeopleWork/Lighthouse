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

        private static readonly TimeZoneInfo InstanceZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        private FakeTimeProvider timeProvider = null!;
        private RecordingLogger logger = null!;

        [SetUp]
        public void SetUp()
        {
            timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 2, 7, 0, 0, TimeSpan.Zero));
            logger = new RecordingLogger();
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

            var series = new Mock<IReportMetricSeries>();
            series.Setup(s => s.CycleTimePercentiles(Lightspeed, It.IsAny<ReportWindow>())).Returns(new CycleTimePercentiles(6, 11, 21, 20));

            var owners = new Mock<IReportOwnerLookup>();
            owners.Setup(o => o.Find(Lightspeed)).Returns(new ReportOwnerFacts(365));

            var writer = new Mock<IReportWriter>();
            writer.Setup(w => w.Add(It.IsAny<Report>())).Callback<Report>(report => report.Id = 17);
            writer.Setup(w => w.Save()).Callback(() => timeProvider.Advance(TimeSpan.FromSeconds(seconds))).Returns(Task.CompletedTask);

            var template = new ThenAndNowTemplate(new ReportMetricCatalog([new CycleTimeReportMetric(series.Object)]), clock);
            return new ReportCommands(writer.Object, owners.Object, [template], clock, timeProvider, logger);
        }

        private sealed record LogLine(LogLevel Level, string Message, Dictionary<string, object?> Fields);

        private sealed class RecordingLogger : ILogger<ReportCommands>
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
