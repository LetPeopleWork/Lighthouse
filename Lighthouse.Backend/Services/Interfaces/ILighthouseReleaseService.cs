using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ILighthouseReleaseService
    {
        string GetCurrentVersion();

        Task<LighthouseRelease?> GetLatestRelease();

        Task<bool> UpdateAvailable();
    }
}