namespace Lighthouse.Backend.Services.Interfaces.DatabaseManagement
{
    public interface IDatabaseManagementProvider
    {
        string ProviderName { get; }

        bool IsToolingAvailable();

        string? GetToolingGuidanceMessage();

        string? GetToolingGuidanceUrl();

        void RecycleConnection();

        Task<string> CreateBackup(string destinationPath);

        Task RestoreBackup(string backupContentPath);

        Task ClearDatabase();

        string? GetServerVersion();
    }
}
