using System.Net;
using Lighthouse.Backend.Tests.TestHelpers.Health;
using Lighthouse.Backend.Tests.TestHelpers.Telemetry;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("epic-5305-k8s-readiness")]
    public class MetricsEndpointIntegrationTest
    {
        [Test]
        public async Task Metrics_Endpoint_ReturnsPrometheusFormatWithHttpServerMetrics()
        {
            using var host = new TelemetryTestHost(telemetryEnabled: true);
            await host.GetAsync(HealthCheckTestHost.LivePath);

            var metrics = await host.GetMetricsAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host.HasMeterProvider, Is.True, "OpenTelemetry metrics must be registered when telemetry is on");
                Assert.That(host.HasMetricsEndpoint, Is.True, "/metrics route must be mapped when telemetry is on");
                Assert.That(metrics.IsOk, Is.True, $"/metrics returned {metrics.StatusCode}");
                Assert.That(metrics.Body, Does.Contain("# TYPE"), "Prometheus exposition format marker missing");
                Assert.That(metrics.Body, Does.Contain("http_server_request_duration_seconds"), "HTTP server metric missing");
            }
        }

        [Test]
        public async Task Metrics_AfterHttpRequests_ExposesRequestCountAndLatency()
        {
            using var host = new TelemetryTestHost(telemetryEnabled: true);
            await host.GetAsync(HealthCheckTestHost.LivePath);
            await host.GetAsync(HealthCheckTestHost.ReadyPath);
            await host.GetAsync(HealthCheckTestHost.StartupPath);

            var metrics = await host.GetMetricsAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.Body, Does.Contain("http_server_request_duration_seconds_count"), "request count metric missing");
                Assert.That(metrics.Body, Does.Contain("http_server_request_duration_seconds_sum"), "request latency metric missing");
            }
        }

        [Test]
        public void Metrics_Exposure_OffUnlessConsciouslyConfigured()
        {
            using var host = new TelemetryTestHost(telemetryEnabled: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host.HasMeterProvider, Is.False, "no OpenTelemetry metrics services must be registered when telemetry is off");
                Assert.That(host.HasMetricsEndpoint, Is.False, "/metrics route must not be mapped when telemetry is off");
            }
        }

        [Test]
        public async Task Telemetry_DisabledByDefault_NoExporterNoBehaviourChange()
        {
            using var host = new TelemetryTestHost();

            var liveStatus = await host.GetAsync(HealthCheckTestHost.LivePath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host.HasMeterProvider, Is.False, "no OpenTelemetry metrics services must be registered by default");
                Assert.That(host.HasMetricsEndpoint, Is.False, "/metrics route must not be mapped by default");
                Assert.That(liveStatus, Is.EqualTo(HttpStatusCode.OK), $"existing endpoints must be unaffected but /health/live was {liveStatus}");
            }
        }
    }
}
