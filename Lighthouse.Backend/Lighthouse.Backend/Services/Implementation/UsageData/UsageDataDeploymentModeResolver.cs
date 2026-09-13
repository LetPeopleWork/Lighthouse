using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.UsageData;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    /// <summary>
    /// Composes the platform service rather than changing it. What that service calls Docker is also
    /// true of every Kubernetes pod, and the release path reads the same value to decide whether an
    /// in-place update applies - so teaching it a new answer would change behaviour on instances that
    /// never turn usage data on.
    /// </summary>
    public sealed class UsageDataDeploymentModeResolver(IPlatformService platform) : IUsageDataDeploymentModeResolver
    {
        // Kubelet puts this into every pod it starts, which is why one variable is enough to tell a
        // pod from a plain container.
        private const string OnlySetInsideAPod = "KUBERNETES_SERVICE_HOST";

        public UsageDataDeploymentMode Resolve()
        {
            // Order matters twice over. A standalone install is a distribution shape rather than an
            // operating system, and it is the case where "which OS" is least worth knowing. Then
            // Kubernetes, because the container markers are true in a pod as well, so the narrower
            // answer has to be asked for before the broader one can claim it.
            if (platform.IsStandalone)
            {
                return UsageDataDeploymentMode.Standalone;
            }

            if (InsideAPod())
            {
                return UsageDataDeploymentMode.Kubernetes;
            }

            return platform.Platform switch
            {
                SupportedPlatform.Docker => UsageDataDeploymentMode.Docker,
                SupportedPlatform.Windows => UsageDataDeploymentMode.Windows,
                SupportedPlatform.Linux => UsageDataDeploymentMode.Linux,
                SupportedPlatform.MacOS => UsageDataDeploymentMode.MacOS,

                // Nothing is substituted here. Every value this product can run on has a name in the
                // published list, so a value without one means the list and the code have drifted
                // apart - and guessing would put a wrong answer into the numbers rather than none.
                _ => throw new PlatformNotSupportedException(
                    $"There is no published deployment mode for the platform {platform.Platform}."),
            };
        }

        private static bool InsideAPod()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OnlySetInsideAPod));
        }
    }
}
