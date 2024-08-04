using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Octokit;

namespace Lighthouse.Backend.Services.Implementation
{
    public class GitHubService : IGitHubService
    {
        private const int RepositoryId = 755695945;
        private const string RepositoryOwner = "LetPeopleWork";
        private const string RepositoryName = "Lighthouse";

        private readonly GitHubClient client;

        public GitHubService()
        {
            client = new GitHubClient(new ProductHeaderValue("let-people-work-ligthhouse"));
        }

        public async Task<string> GetLatestReleaseVersion()
        {
            var release = await client.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);
            return release.TagName;
        }

        public async Task<LighthouseRelease?> GetReleaseByTag(string releaseTagName)
        {
            try
            {
                var release = await client.Repository.Release.Get(RepositoryId, releaseTagName);
                var lighthouseRelease = CreateLighthouseRelease(release);

                return lighthouseRelease;
            }
            catch (NotFoundException)
            {
                return null;
            }
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
