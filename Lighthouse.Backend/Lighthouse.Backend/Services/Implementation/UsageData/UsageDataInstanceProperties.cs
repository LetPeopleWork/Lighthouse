using System.Text.RegularExpressions;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    /// <summary>
    /// Asks the parts of the application that already know, rather than adding anywhere new for these
    /// answers to live. A scope is opened each time because three of the four are per-request
    /// services and this runs on a background loop that has none.
    /// </summary>
    public sealed partial class UsageDataInstanceProperties(
        IServiceScopeFactory scopeFactory,
        IUsageDataDeploymentModeResolver deployment) : IUsageDataInstanceProperties
    {
        /// <summary>
        /// What a version is replaced by when it is not one anybody published. Everyone on a release
        /// shares its number, so it says nothing about any of them; a number built from somebody's
        /// working tree is close to unique, and the handful of people running one are the people most
        /// easily recognised from it. Nobody plans a support cut-off against a working tree, so
        /// dropping it costs nothing anyone wanted.
        /// </summary>
        private const string NotAPublishedRelease = "unreleased";

        private const string Premium = "Premium";

        private const string Community = "Community";

        public UsageDataInstanceFacts Describe()
        {
            using var scope = scopeFactory.CreateScope();

            var releases = scope.ServiceProvider.GetRequiredService<ILighthouseReleaseService>();
            var licences = scope.ServiceProvider.GetRequiredService<ILicenseService>();
            var authentication = scope.ServiceProvider.GetRequiredService<IAuthModeResolver>();

            return new UsageDataInstanceFacts(
                VersionIfSomebodyPublishedIt(releases.GetCurrentVersion()),
                deployment.Resolve(),
                licences.CanUsePremiumFeatures() ? Premium : Community,

                // Only a working sign-in makes "one browser is roughly one person" plausible. With
                // authentication off every caller is the same subject and a shared screen is a live
                // possibility, and an instance that cannot let anybody in is not one people have
                // their own logins on either.
                authentication.Resolve().Mode == AuthMode.Enabled);
        }

        private static string VersionIfSomebodyPublishedIt(string version)
        {
            return AShapeOnlyAReleaseHas().IsMatch(version) ? version : NotAPublishedRelease;
        }

        /// <summary>
        /// Every release this product has ever published is stamped with the date it was built and
        /// which build of that day it was, so anything that is not a date is not a release - a
        /// development build, a local one, or the version a test host reports.
        /// </summary>
        [GeneratedRegex(@"^v[0-9]{2}\.(?:1[0-2]|[1-9])\.(?:3[01]|[12][0-9]|[1-9])(?:\.[0-9]+)?$")]
        private static partial Regex AShapeOnlyAReleaseHas();
    }
}
