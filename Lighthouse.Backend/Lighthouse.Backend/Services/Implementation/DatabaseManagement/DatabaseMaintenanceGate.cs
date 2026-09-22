using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.DatabaseManagement
{
    public record GateAcquisitionResult(bool Acquired, string? BlockedReason, bool PendingBehindBackup = false);

    public class DatabaseMaintenanceGate
    {
        private const string BackgroundUpdateIsRunning =
            "A background update is currently in progress. Database operations cannot start until background work completes.";

        private const string HistoryIsBeingFilledIn =
            "The chart is filling in days it was missing. Database operations cannot start until it has finished.";

        private readonly IUpdateStatusStore statusStore;
        private readonly IOverTimeHistoryFillActivity? historyFill;
        private readonly object gateLock = new();

        private string? activeOperationId;
        private DatabaseOperationType? activeOperationType;

        public DatabaseMaintenanceGate(IUpdateStatusStore statusStore, IOverTimeHistoryFillActivity? historyFill = null)
        {
            this.statusStore = statusStore;
            this.historyFill = historyFill;
        }

        public bool IsBlocked => WhatIsHoldingTheDatabase() != null;

        public string? BlockedReason => WhatIsHoldingTheDatabase();

        /// <summary>
        /// Whether a backup, restore or clear is under way right now. Anything that writes to the
        /// database off its own back has to stand down while one is: all three replace the file.
        /// </summary>
        public bool IsMaintenanceOperationActive => ActiveOperationId != null;

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
                var backgroundWork = WhatBackgroundWorkIsWriting();
                if (backgroundWork != null)
                {
                    return new GateAcquisitionResult(false, backgroundWork);
                }

                if (activeOperationId != null)
                {
                    var pendingBehindBackup = activeOperationType == DatabaseOperationType.Backup
                        && operationType != DatabaseOperationType.Backup;

                    return new GateAcquisitionResult(false, ActiveOperationIsRunning(), pendingBehindBackup);
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

        /// <summary>
        /// The one place a reason is worded, so an operator reads the same sentence whether they asked
        /// the gate what is holding the database or were turned away trying to start something.
        /// </summary>
        private string? WhatIsHoldingTheDatabase()
        {
            var backgroundWork = WhatBackgroundWorkIsWriting();
            if (backgroundWork != null)
            {
                return backgroundWork;
            }

            lock (gateLock)
            {
                return activeOperationId != null && activeOperationType != null
                    ? ActiveOperationIsRunning()
                    : null;
            }
        }

        private string? WhatBackgroundWorkIsWriting()
        {
            if (statusStore.HasActiveWork())
            {
                return BackgroundUpdateIsRunning;
            }

            // Filling in missing chart days writes to the database without going through the update
            // queue, so the status store above knows nothing about it and has to be asked separately.
            if (historyFill?.HasPassInFlight == true)
            {
                return HistoryIsBeingFilledIn;
            }

            return null;
        }

        private string ActiveOperationIsRunning() => $"A database {activeOperationType} operation is currently active.";
    }
}
