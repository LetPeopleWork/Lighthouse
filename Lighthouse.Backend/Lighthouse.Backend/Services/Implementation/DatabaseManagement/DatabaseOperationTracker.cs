using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Services.Implementation.DatabaseManagement
{
    public class DatabaseOperationTracker
    {
        private readonly ConcurrentDictionary<string, DatabaseOperationStatus> operations = new();
        private string? latestOperationId;
        private readonly object latestLock = new();

        public DatabaseOperationStatus StartOperation(string operationId, DatabaseOperationType operationType)
        {
            var status = new DatabaseOperationStatus(operationId, operationType, DatabaseOperationState.Admitted);
            operations[operationId] = status;

            lock (latestLock)
            {
                latestOperationId = operationId;
            }

            return status;
        }

        public DatabaseOperationStatus? GetStatus(string operationId)
        {
            return operations.TryGetValue(operationId, out var status) ? status : null;
        }

        public DatabaseOperationStatus? GetLatestStatus()
        {
            lock (latestLock)
            {
                return latestOperationId != null ? GetStatus(latestOperationId) : null;
            }
        }

        public void TransitionTo(string operationId, DatabaseOperationState newState)
        {
            if (operations.TryGetValue(operationId, out var current))
            {
                operations[operationId] = current with { State = newState };
            }
        }

        public void TransitionToFailed(string operationId, string reason)
        {
            if (operations.TryGetValue(operationId, out var current))
            {
                operations[operationId] = current with { State = DatabaseOperationState.Failed, FailureReason = reason };
            }
        }
    }
}
