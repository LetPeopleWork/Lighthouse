using System.Net;

namespace Lighthouse.Backend.Tests.TestHelpers.Telemetry
{
    public sealed record TelemetryResponse(HttpStatusCode StatusCode, string Body)
    {
        public bool IsOk => StatusCode == HttpStatusCode.OK;

        public bool IsPrometheusExposition =>
            Body.Contains("http_server_request_duration_seconds", StringComparison.Ordinal)
            || Body.Contains("# TYPE ", StringComparison.Ordinal);
    }
}
