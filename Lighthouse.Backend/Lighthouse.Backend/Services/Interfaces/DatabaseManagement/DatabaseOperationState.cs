namespace Lighthouse.Backend.Services.Interfaces.DatabaseManagement
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
