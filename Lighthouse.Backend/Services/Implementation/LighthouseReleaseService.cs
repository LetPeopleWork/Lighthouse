

using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class LighthouseReleaseService : ILighthouseReleaseService
    {
        private IConfiguration configuration;
        private readonly IGitHubService gitHubService;

        public LighthouseReleaseService(IConfiguration configuration, IGitHubService gitHubService)
        {
            this.configuration = configuration;
            this.gitHubService = gitHubService;
        }

        public string GetCurrentVersion()
        {
            var version = configuration.GetValue<string>("LighthouseVersion") ?? string.Empty;

            return version;
        }

        public async Task<bool> UpdateAvailable()
        {
            var currentRelease = GetCurrentVersion();

            if (!currentRelease.StartsWith("v"))
            {
                return true;
            }

            var latestRelease = await gitHubService.GetLatestReleaseVersion();
            if (!latestRelease.StartsWith("v"))
            {
                return false;
            }

            var currentReleaseVersion = new Version(currentRelease.Substring(1));
            var latestReleaseVersion = new Version(latestRelease.Substring(1));

            return latestReleaseVersion > currentReleaseVersion;
        }
    }
}
