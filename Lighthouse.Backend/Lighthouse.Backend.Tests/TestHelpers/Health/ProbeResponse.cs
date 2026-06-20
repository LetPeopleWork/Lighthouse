using System.Net;

namespace Lighthouse.Backend.Tests.TestHelpers.Health
{
    public sealed record ProbeResponse(HttpStatusCode StatusCode, string Body)
    {
        public bool IsHealthyProbe => StatusCode == HttpStatusCode.OK && Body == "Healthy";

        public bool IsUnhealthyProbe => StatusCode == HttpStatusCode.ServiceUnavailable && Body == "Unhealthy";
    }
}
