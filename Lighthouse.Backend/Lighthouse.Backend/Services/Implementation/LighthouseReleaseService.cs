using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Updatum;

namespace Lighthouse.Backend.Services.Implementation
{
    public class LighthouseReleaseService : ILighthouseReleaseService
    {
        private readonly IHostEnvironment hostEnvironment;
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
            return $"v{version}";
        }

        public async Task<bool> UpdateAvailable()
        {
            var currentRelease = GetCurrentVersion();

            if (!currentRelease.StartsWith('v'))
            {
                return true;
            }

            var latestRelease = await GetLatestReleaseTag();
            if (!latestRelease.StartsWith('v'))
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

        public bool IsUpdateSupported()
        {
            // Don't support updates in development mode
            if (hostEnvironment.IsDevelopment())
            {
                return false;
            }

            // Don't support updates when running in Docker
            if (IsRunningInDocker())
            {
                return false;
            }

            return true;
        }

        public async Task<bool> InstallUpdateAsync()
        {
            if (!IsUpdateSupported())
            {
                return false;
            }

            try
            {
                // Create an instance of UpdatumManager for the Lighthouse repository
                var updater = new UpdatumManager("LetPeopleWork", "Lighthouse");

                // Check for updates
                var updateFound = await updater.CheckForUpdatesAsync();
                if (!updateFound)
                {
                    return false;
                }

                // Download the update
                var downloadedAsset = await updater.DownloadUpdateAsync();
                if (downloadedAsset == null)
                {
                    return false;
                }

                // Install the update (this will terminate the process and restart with the new version)
                await updater.InstallUpdateAsync(downloadedAsset);
                
                // This line should never be reached if the update was successful
                return true;
            }
            catch (Exception)
            {
                // Log the exception if needed
                return false;
            }
        }

        private static bool IsRunningInDocker()
        {
            // Check for common Docker environment indicators
            return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
                   File.Exists("/.dockerenv") ||
                   Environment.GetEnvironmentVariable("LIGHTHOUSE_DOCKER") == "true";
        }
    }
}
