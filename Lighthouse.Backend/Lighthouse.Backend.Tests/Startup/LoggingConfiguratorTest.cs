using System.Text.Json;
using Lighthouse.Backend.Configuration;
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
                using var logger = LoggingConfigurator.CreateLogger(configuration, new LoggingLevelSwitch());
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
                using var logger = LoggingConfigurator.CreateLogger(configuration, new LoggingLevelSwitch());
                logger.Information("Structured payload {Marker}", Marker);
            });

            var markerLine = MarkerLine(output);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(markerLine, Does.Contain(Marker));
                Assert.That(() => JsonDocument.Parse(markerLine), Throws.InstanceOf<JsonException>(), "default console output must not be JSON");
            }
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
