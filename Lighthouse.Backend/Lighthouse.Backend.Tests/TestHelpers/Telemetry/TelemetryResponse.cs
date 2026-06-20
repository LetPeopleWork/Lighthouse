using System.Net;

namespace Lighthouse.Backend.Tests.TestHelpers.Telemetry
{
    public sealed record TelemetryResponse(HttpStatusCode StatusCode, string Body)
    {
        public bool IsOk => StatusCode == HttpStatusCode.OK;
    }
}
