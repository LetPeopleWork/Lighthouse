using System.Net;

namespace Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders
{
    public sealed record ForwardedHeadersTestOptions
    {
        public IReadOnlyList<string> TrustedProxies { get; init; } = [];

        public IReadOnlyList<string> TrustedNetworks { get; init; } = [];

        public IPAddress? SimulatedRemoteIp { get; init; }

        public static ForwardedHeadersTestOptions Standalone()
            => new();

        public static ForwardedHeadersTestOptions TrustingProxy(string proxyIp, string simulatedRemoteIp)
            => new()
            {
                TrustedProxies = [proxyIp],
                SimulatedRemoteIp = IPAddress.Parse(simulatedRemoteIp),
            };

        public static ForwardedHeadersTestOptions UntrustedSource(string declaredProxyIp, string simulatedRemoteIp)
            => new()
            {
                TrustedProxies = [declaredProxyIp],
                SimulatedRemoteIp = IPAddress.Parse(simulatedRemoteIp),
            };

        public static ForwardedHeadersTestOptions TrustingNetwork(string cidr, string simulatedRemoteIp)
            => new()
            {
                TrustedNetworks = [cidr],
                SimulatedRemoteIp = IPAddress.Parse(simulatedRemoteIp),
            };
    }
}
