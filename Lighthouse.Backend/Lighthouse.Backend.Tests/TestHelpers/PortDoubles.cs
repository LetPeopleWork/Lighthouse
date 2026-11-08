using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
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
        /// rows say rather than reading them.
        /// </summary>
        public static IAppSettingService SettingsAsSeeded()
        {
            var settings = new Mock<IAppSettingService>();
            settings
                .Setup(service => service.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 });
            return settings.Object;
        }
    }
}
