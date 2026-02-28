using System.Diagnostics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WriteBackService(
        IWorkTrackingConnectorFactory connectorFactory,
        ILogger<WriteBackService> logger)
        : IWriteBackService
    {
        public async Task<WriteBackResult> WriteFieldsToWorkItems(
            WorkTrackingSystemConnection connection,
            IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            if (updates.Count == 0)
            {
                logger.LogInformation("Starting write-back for connection {ConnectionId} ({ConnectionName}) with 0 updates — skipping", connection.Id, connection.Name);
                return new WriteBackResult();
            }

            logger.LogInformation(
                "Starting write-back for connection {ConnectionId} ({ConnectionName}), {UpdateCount} update(s), provider {WorkTrackingSystem}",
                connection.Id, connection.Name, updates.Count, connection.WorkTrackingSystem);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var connector = connectorFactory.GetWorkTrackingConnector(connection.WorkTrackingSystem);
                var result = await connector.WriteFieldsToWorkItems(connection, updates);

                stopwatch.Stop();

                logger.LogInformation(
                    "Completed write-back for connection {ConnectionId} ({ConnectionName}) in {ElapsedMs}ms — {SuccessCount} succeeded, {FailureCount} failed",
                    connection.Id, connection.Name, stopwatch.ElapsedMilliseconds, result.SuccessCount, result.FailureCount);

                foreach (var failure in result.ItemResults.Where(r => !r.Success))
                {
                    logger.LogDebug(
                        "Write-back failed for work item {WorkItemId}, field {TargetFieldReference} on connection {ConnectionId}: {ErrorMessage}",
                        failure.WorkItemId, failure.TargetFieldReference, connection.Id, failure.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                logger.LogError(ex,
                    "Write-back failed for connection {ConnectionId} ({ConnectionName}) after {ElapsedMs}ms with unhandled exception",
                    connection.Id, connection.Name, stopwatch.ElapsedMilliseconds);

                return new WriteBackResult
                {
                    ItemResults = updates.Select(u => new WriteBackItemResult
                    {
                        WorkItemId = u.WorkItemId,
                        TargetFieldReference = u.TargetFieldReference,
                        Success = false,
                        ErrorMessage = ex.Message
                    }).ToList()
                };
            }
        }
    }
}
