using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IGitHubService
    {
        Task<string> GetLatestReleaseVersion();

        Task<IEnumerable<LighthouseRelease>> GetAllReleases();

        Task<LighthouseRelease?> GetReleaseByTag(string releaseTagName);
    }
}
