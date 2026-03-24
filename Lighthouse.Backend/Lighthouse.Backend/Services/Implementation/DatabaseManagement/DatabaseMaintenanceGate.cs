using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Services.Implementation.DatabaseManagement
{
    public record GateAcquisitionResult(bool Acquired, string? BlockedReason, bool PendingBehindBackup = false);

    public class DatabaseMaintenanceGate
    {
        private readonly ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;
        private readonly object gateLock = new();

        private string? activeOperationId;
        private DatabaseOperationType? activeOperationType;

        public DatabaseMaintenanceGate(ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses)
        {
            this.updateStatuses = updateStatuses;
        }

        public bool IsBlocked => HasActiveBackgroundWork() || activeOperationId != null;

        public string? BlockedReason
        {
            get
            {
                if (HasActiveBackgroundWork())
                {
                    return "A background update is currently in progress. Database operations cannot start until background work completes.";
                }

                if (activeOperationId != null && activeOperationType != null)
                {
                    return $"A database {activeOperationType} operation is currently active.";
                }

                return null;
            }
        }

        public string? ActiveOperationId
        {
            get
            {
                lock (gateLock)
                {
                    return activeOperationId;
                }
            }
        }

        public DatabaseOperationType? ActiveOperationType
        {
            get
            {
                lock (gateLock)
                {
                    return activeOperationType;
                }
            }
        }

        public GateAcquisitionResult TryAcquire(DatabaseOperationType operationType, string operationId)
        {
            lock (gateLock)
            {
                if (HasActiveBackgroundWork())
                {
                    return new GateAcquisitionResult(false, "A background update is currently in progress. Database operations cannot start until background work completes.");
                }

                if (activeOperationId != null)
                {
                    var pendingBehindBackup = activeOperationType == DatabaseOperationType.Backup
                        && operationType != DatabaseOperationType.Backup;

                    return new GateAcquisitionResult(
                        false,
                        $"A database {activeOperationType} operation is currently active.",
                        pendingBehindBackup);
                }

                activeOperationId = operationId;
                activeOperationType = operationType;

                return new GateAcquisitionResult(true, null);
            }
        }

        public void Release(string operationId)
        {
            lock (gateLock)
            {
                if (activeOperationId == operationId)
                {
                    activeOperationId = null;
                    activeOperationType = null;
                }
            }
        }

        private bool HasActiveBackgroundWork()
        {
            return updateStatuses.Values.Any(status =>
                status.Status is UpdateProgress.Queued or UpdateProgress.InProgress);
        }
    }
}
