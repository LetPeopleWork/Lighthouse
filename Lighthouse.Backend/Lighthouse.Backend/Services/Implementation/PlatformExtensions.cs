using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public static class PlatformExtensions
    {
        public static string GetIdentifier(this SupportedPlatform platform)
        {
            return platform switch
            {
                SupportedPlatform.Windows => "win",
                SupportedPlatform.Linux => "linux",
                SupportedPlatform.MacOS => "osx",
                SupportedPlatform.Docker => "docker",
                _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
            };
        }
    }
}