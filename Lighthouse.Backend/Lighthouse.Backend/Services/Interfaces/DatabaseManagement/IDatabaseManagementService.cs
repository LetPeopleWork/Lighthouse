namespace Lighthouse.Backend.Services.Interfaces.DatabaseManagement
{
    public interface IDatabaseManagementService
    {
        DatabaseCapabilityStatus GetCapabilityStatus();

        DatabaseOperationStatus? GetOperationStatus(string operationId);

        Task<DatabaseOperationStatus> CreateBackup(string password);

        Task<DatabaseOperationStatus> RestoreBackup(Stream backupFile, string password);

        Task<DatabaseOperationStatus> ClearDatabase();

        Stream GetBackupArtifact(string operationId);
    }
}
