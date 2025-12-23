using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Octokit;

namespace Lighthouse.Backend.Services.Implementation
{
    public class GitHubService : IGitHubService
    {
        private const string RepositoryOwner = "LetPeopleWork";
        private const string RepositoryName = "Lighthouse";
        
        private const string LatestVersionCacheKey = "latest";
        private const string AllReleasesCacheKey = "allReleases";

        private readonly Cache<string, object> gitHubServiceCache = new Cache<string, object>();

        private readonly GitHubClient client;

        public GitHubService()
        {
            client = new GitHubClient(new ProductHeaderValue("let-people-work-lighthouse"));
        }

        public async Task<string> GetLatestReleaseVersion()
        {
            var latestVersion = gitHubServiceCache.Get(LatestVersionCacheKey)?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(latestVersion))
            {
                var release = await client.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);
                latestVersion = release.TagName;

                gitHubServiceCache.Store(LatestVersionCacheKey, latestVersion, TimeSpan.FromHours(1));
            }

            return latestVersion;
        }

        public async Task<IEnumerable<LighthouseRelease>> GetAllReleases()
        {
            var cachedReleases = gitHubServiceCache.Get(AllReleasesCacheKey) as IReadOnlyList<Release>;
            if (cachedReleases == null)
            {
                cachedReleases = await client.Repository.Release.GetAll(RepositoryOwner, RepositoryName);
                gitHubServiceCache.Store(AllReleasesCacheKey, cachedReleases, TimeSpan.FromHours(30));
            }

            return cachedReleases.Where(r => !r.Prerelease).Select(CreateLighthouseRelease);
        }

        private LighthouseRelease CreateLighthouseRelease(Release release)
        {
            var lighthouseRelease = new LighthouseRelease
            {
                Name = release.Name,
                Version = release.TagName,
                Link = release.HtmlUrl,
                Highlights = release.Body,
            };

            var releaseAssets = CreateLighthouseReleaseAssets(release);
            lighthouseRelease.Assets.AddRange(releaseAssets);
            return lighthouseRelease;
        }

        private IEnumerable<LighthouseReleaseAsset> CreateLighthouseReleaseAssets(Release release)
        {
            return release.Assets.Select(ra => new LighthouseReleaseAsset { Name = ra.Name, Link = ra.BrowserDownloadUrl });
        }
    }
}
