using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// The stand-ins a service port fixture registers for what lies outside the port. Each is defined here once,
    /// so every fixture stands in for the same thing the same way, and none writes its own.
    /// </summary>
    public static class PortDoubles
    {
        /// <summary>The licence the host's scenarios run under: premium features are on.</summary>
        public static ILicenseService PremiumLicence()
        {
            var licence = new Mock<ILicenseService>();
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);
            return licence.Object;
        }

        /// <summary>
        /// The settings a newly seeded database holds. The port seeds nothing, so this answers what the seeded
        /// rows say rather than reading them. A setting nobody gave it fails the read, naming the setting, rather
        /// than answering a default no seeded database holds.
        /// </summary>
        public static IAppSettingService SettingsAsSeeded()
        {
            var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
            settings
                .Setup(service => service.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 });
            return settings.Object;
        }

        /// <summary>Sign-in is off, as it is on every instance a port scenario stands for.</summary>
        public static IAuthModeResolver SignInOff()
        {
            var authMode = new Mock<IAuthModeResolver>();
            authMode.Setup(resolver => resolver.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Disabled });
            return authMode.Object;
        }

        /// <summary>
        /// A port scenario serves no request, so there is none to read. The product's own accessor with nothing in
        /// it says exactly that; a service reads the request only when sign-in is on, and at the port it is off.
        /// </summary>
        public static IHttpContextAccessor NoRequest() => new HttpContextAccessor();
    }
}
