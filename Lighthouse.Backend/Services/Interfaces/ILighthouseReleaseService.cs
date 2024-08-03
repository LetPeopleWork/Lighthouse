namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ILighthouseReleaseService
    {
        string GetCurrentVersion();

        Task<bool> UpdateAvailable();
    }
}