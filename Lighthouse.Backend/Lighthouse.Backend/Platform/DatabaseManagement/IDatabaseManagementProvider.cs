using System.Data.Common;

namespace Lighthouse.Backend.Platform.DatabaseManagement
{
    public interface IDatabaseManagementProvider
    {
        string ProviderName { get; }

        bool IsToolingAvailable();

        string? GetToolingGuidanceMessage();

        string? GetToolingGuidanceUrl();

        void RecycleConnection();

        DbConnection CreateConnection();

        Task<string> CreateBackup(string destinationPath);

        Task<IStagedBackup> StageBackup(string backupContentPath);

        Task RestoreBackup(string backupContentPath);

        Task ClearDatabase();

        string? GetServerVersion();
    }
}
