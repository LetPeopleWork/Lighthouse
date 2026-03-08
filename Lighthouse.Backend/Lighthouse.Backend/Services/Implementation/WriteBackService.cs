using System.Diagnostics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WriteBackService(
        IWorkTrackingConnectorFactory connectorFactory,
        ILogger<WriteBackService> logger,
        IWorkItemRepository workItemRepository,
        IRepository<Feature> featureRepository)
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
                var result = await WriteUpdates(connection, updates);

                stopwatch.Stop();

                logger.LogInformation(
                    "Completed write-back for connection {ConnectionId} ({ConnectionName}) in {ElapsedMs}ms — {SuccessCount} succeeded, {FailureCount} failed",
                    connection.Id, connection.Name, stopwatch.ElapsedMilliseconds, result.SuccessCount, result.FailureCount);

                LogFailedUpdates(connection, result);

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

        private void LogFailedUpdates(WorkTrackingSystemConnection connection, WriteBackResult result)
        {
            foreach (var failure in result.ItemResults.Where(r => !r.Success))
            {
                logger.LogDebug(
                    "Write-back failed for work item {WorkItemId}, field {TargetFieldReference} on connection {ConnectionId}: {ErrorMessage}",
                    failure.WorkItemId, failure.TargetFieldReference, connection.Id, failure.ErrorMessage);
            }
        }

        private async Task<WriteBackResult> WriteUpdates(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            var connector = connectorFactory.GetWorkTrackingConnector(connection.WorkTrackingSystem);
            var changedFields = GetChangedFields(updates, connection);
            var result = await connector.WriteFieldsToWorkItems(connection, changedFields);
            return result;
        }

        private List<WriteBackFieldUpdate> GetChangedFields(IReadOnlyList<WriteBackFieldUpdate> updates, WorkTrackingSystemConnection connection)
        {
            var allFeatures = featureRepository.GetAll();
            var allWorkItems = workItemRepository.GetAll();
            
            var allItems = allFeatures.OfType<WorkItemBase>().Union(allWorkItems).Distinct();

            var actualUpdates = new List<WriteBackFieldUpdate>();
            var additionalFieldMap = connection.AdditionalFieldDefinitions.ToDictionary(a => a.Reference, a => a.Id);

            foreach (var update in updates)
            {
                var changedItem = allItems.SingleOrDefault(x => x.ReferenceId == update.WorkItemId);
                if (changedItem == null)
                {
                    continue;
                }

                if (!additionalFieldMap.TryGetValue(update.TargetFieldReference, out var additionalFieldId))
                {
                    continue;
                }

                if (!changedItem.AdditionalFieldValues.TryGetValue(additionalFieldId, out var currentAdditionalFieldValue))
                {
                    continue;
                }

                if (currentAdditionalFieldValue != update.Value)
                {
                    actualUpdates.Add(update);
                }
            }
            
            return actualUpdates;
        }
    }
}
