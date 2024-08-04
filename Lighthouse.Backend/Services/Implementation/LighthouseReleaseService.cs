using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class LighthouseReleaseService : ILighthouseReleaseService
    {
        private IHostEnvironment hostEnvironment;
        private readonly IGitHubService gitHubService;
        private readonly IAssemblyService assemblyService;

        public LighthouseReleaseService(IHostEnvironment hostEnvironment, IGitHubService gitHubService, IAssemblyService assemblyService)
        {
            this.hostEnvironment = hostEnvironment;
            this.gitHubService = gitHubService;
            this.assemblyService = assemblyService;
        }

        public string GetCurrentVersion()
        {
            if (hostEnvironment.IsDevelopment())
            {
                return "DEV";
            }

            var version = assemblyService.GetAssemblyVersion();
            return version;
        }

        public async Task<bool> UpdateAvailable()
        {
            var currentRelease = GetCurrentVersion();

            if (!currentRelease.StartsWith("v"))
            {
                return true;
            }

            var latestRelease = await GetLatestReleaseTag();
            if (!latestRelease.StartsWith("v"))
            {
                return false;
            }

            var currentReleaseVersion = new Version(currentRelease.Substring(1));
            var latestReleaseVersion = new Version(latestRelease.Substring(1));

            return latestReleaseVersion > currentReleaseVersion;
        }

        public async Task<IEnumerable<LighthouseRelease>> GetNewReleases()
        {
            var newReleases = new List<LighthouseRelease>();

            var allReleases = await gitHubService.GetAllReleases();
            var currentVersion = GetCurrentVersion();

            foreach (var release in allReleases)
            {
                if (release.Version == currentVersion)
                {
                    break;
                }

                newReleases.Add(release);
            }

            return newReleases;
        }

        private async Task<string> GetLatestReleaseTag()
        {
            return await gitHubService.GetLatestReleaseVersion();
        }
    }
}
