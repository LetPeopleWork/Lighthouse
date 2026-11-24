namespace Lighthouse.Backend.Platform.DatabaseManagement
{
    public interface IDatabaseManagementService
    {
        DatabaseCapabilityStatus GetCapabilityStatus();

        DatabaseOperationStatus? GetOperationStatus(string operationId);

        Task<DatabaseOperationStatus> CreateBackup(string password);

        Task<DatabaseOperationStatus> RestoreBackup(Stream backupFile, string password, UnreadableCredentials onUnreadable = UnreadableCredentials.StopRestore);

        Task<DatabaseOperationStatus> ClearDatabase();

        Stream GetBackupArtifact(string operationId);
    }
}
