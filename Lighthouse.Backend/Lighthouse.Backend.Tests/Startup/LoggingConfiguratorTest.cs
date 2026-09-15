using System.Text.Json;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Services.Implementation.Logging;
using Lighthouse.Backend.Startup;
using Microsoft.Extensions.Configuration;
using Serilog.Core;

namespace Lighthouse.Backend.Tests.Startup
{
    [Category("epic-5305-k8s-readiness")]
    public class LoggingConfiguratorTest
    {
        private const string Marker = "telemetry-json-marker";

        [Test]
        public void Logging_StructuredJsonToStdout_ContainsExpectedFields()
        {
            var configuration = ConfigurationWith("json");

            var output = CaptureConsole(() =>
            {
                using var logger = LoggingConfigurator.CreateLogger(configuration, new LoggingLevelSwitch(), ASinkNothingHereReads());
                logger.Information("Structured payload {Marker}", Marker);
            });

            var jsonLine = MarkerLine(output);
            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.TryGetProperty("Timestamp", out _), Is.True, "Timestamp field missing");
                Assert.That(root.TryGetProperty("Level", out _), Is.True, "Level field missing");
                Assert.That(root.GetProperty("Message").GetString(), Does.Contain(Marker), "Message field missing marker");
                Assert.That(root.GetProperty("Marker").GetString(), Is.EqualTo(Marker), "structured property not emitted");
            }
        }

        [Test]
        public void Logging_DefaultFormat_RemainsPlainTextNotJson()
        {
            var configuration = ConfigurationWith(format: string.Empty);

            var output = CaptureConsole(() =>
            {
                using var logger = LoggingConfigurator.CreateLogger(configuration, new LoggingLevelSwitch(), ASinkNothingHereReads());
                logger.Information("Structured payload {Marker}", Marker);
            });

            var markerLine = MarkerLine(output);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(markerLine, Does.Contain(Marker));
                Assert.That(() => JsonDocument.Parse(markerLine), Throws.InstanceOf<JsonException>(), "default console output must not be JSON");
            }
        }

        // The buffer is not optional decoration. A logger built without one writes the file and the console
        // exactly as before and records nothing an operator can read in the popover - a logger that looks
        // like it works, on an instance where noticing trouble has quietly gone back to opening the log
        // file. Refusing at the call is what stops that shipping.
        [Test]
        [Category("epic-5511-task-manager")]
        [Category("slice-06")]
        public void CreateLogger_WithNowhereToPutRecentProblems_IsRefusedAndNamesWhatWasMissing()
        {
            Assert.That(
                () => LoggingConfigurator.CreateLogger(ConfigurationWith(format: string.Empty), new LoggingLevelSwitch(), null!),
                Throws.ArgumentNullException
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("recentProblems"));
        }

        /// <summary>
        /// What these tests read is the console output. The recent-problems buffer is a second sink the
        /// logger now requires, and nothing here looks inside it.
        /// </summary>
        private static RecentProblemsSink ASinkNothingHereReads()
        {
            return new RecentProblemsSink(RecentProblemsSink.DefaultCapacity);
        }

        private static IConfigurationRoot ConfigurationWith(string format)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{TelemetryConfiguration.SectionName}:Logging:Format"] = format,
                })
                .Build();
        }

        private static string CaptureConsole(Action action)
        {
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                action();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            return writer.ToString();
        }

        private static string MarkerLine(string output)
        {
            return output
                .Split('\n')
                .Select(line => line.Trim())
                .First(line => line.Contains(Marker, StringComparison.Ordinal));
        }
    }
}
