using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// For tests that have to hand something an instance clock but never read a moment back off it. A test
    /// that asserts on elapsed time wants <c>FakeLighthouseClock</c> instead, so that it can move time
    /// rather than wait for it.
    /// </summary>
    public static class Clocks
    {
        public static ILighthouseClock SystemUtc => new LighthouseClock(TimeZoneInfo.Utc, TimeProvider.System);
    }
}
