namespace Lighthouse.Backend.Platform.DatabaseManagement
{
    public enum DatabaseOperationState
    {
        Requested,
        PendingBehindBackup,
        Admitted,
        Executing,
        RestartPending,
        RestartComplete,
        Completed,
        Failed,
    }
}
