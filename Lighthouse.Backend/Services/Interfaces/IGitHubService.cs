namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IGitHubService
    {
        Task<string> GetLatestReleaseVersion();
    }
}
