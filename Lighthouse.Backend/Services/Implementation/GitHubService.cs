using Lighthouse.Backend.Services.Interfaces;
using Octokit;

namespace Lighthouse.Backend.Services.Implementation
{
    public class GitHubService : IGitHubService
    {
        private readonly GitHubClient client;

        public GitHubService()
        {
            client = new GitHubClient(new ProductHeaderValue("let-people-work-ligthhouse"));
        }

        public async Task<string> GetLatestReleaseVersion()
        {
            var release = await client.Repository.Release.GetLatest("LetPeopleWork", "Lighthouse");
            return release.TagName;
        }
    }
}
