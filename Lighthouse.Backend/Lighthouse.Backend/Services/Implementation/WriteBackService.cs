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
            var changes = GetChangedFields(updates, connection);

            if (changes.Count == 0)
            {
                logger.LogInformation(
                    "No mapped value changed for connection {ConnectionId} ({ConnectionName}) — not calling the work tracking system",
                    connection.Id, connection.Name);
                return new WriteBackResult();
            }

            var connector = connectorFactory.GetWorkTrackingConnector(connection.WorkTrackingSystem);
            var result = await connector.WriteFieldsToWorkItems(connection, [.. changes.Select(change => change.Update)]);

            await PersistWrittenValues(changes, result);

            return result;
        }

        /// <summary>
        /// Every update that would genuinely change something, paired with the item and field it
        /// resolved to. Resolving once is what lets the write and the write-back of the value into the
        /// local copy agree on which item they are talking about.
        /// </summary>
        private List<PendingWrite> GetChangedFields(IReadOnlyList<WriteBackFieldUpdate> updates, WorkTrackingSystemConnection connection)
        {
            var itemsByReference = AllItems().ToLookup(item => item.ReferenceId);
            var fieldIdsByReference = connection.AdditionalFieldDefinitions.ToDictionary(a => a.Reference, a => a.Id);

            var changes = new List<PendingWrite>();

            foreach (var update in updates)
            {
                var changedItem = ResolveItem(update, connection, itemsByReference);
                if (changedItem == null)
                {
                    continue;
                }

                if (!fieldIdsByReference.TryGetValue(update.TargetFieldReference, out var additionalFieldId))
                {
                    continue;
                }

                if (!changedItem.AdditionalFieldValues.TryGetValue(additionalFieldId, out var currentAdditionalFieldValue))
                {
                    continue;
                }

                if (currentAdditionalFieldValue != update.Value)
                {
                    changes.Add(new PendingWrite(update, changedItem, additionalFieldId));
                }
            }

            return changes;
        }

        private IEnumerable<WorkItemBase> AllItems()
        {
            return featureRepository.GetAll().OfType<WorkItemBase>().Union(workItemRepository.GetAll());
        }

        /// <summary>
        /// Indexed lookup rather than a dictionary: a duplicate reference is legal here and logs a
        /// warning, where ToDictionary would throw (ADR-143 §5).
        /// </summary>
        private WorkItemBase? ResolveItem(
            WriteBackFieldUpdate update,
            WorkTrackingSystemConnection connection,
            ILookup<string, WorkItemBase> itemsByReference)
        {
            var changedItems = itemsByReference[update.WorkItemId].ToList();

            if (changedItems.Count == 0)
            {
                return null;
            }

            if (changedItems.Count > 1)
            {
                logger.LogWarning(
                    "Multiple items found with reference {WorkItemReference} for update to field {TargetFieldReference} on connection {ConnectionId} — taking first match.",
                    update.WorkItemId, update.TargetFieldReference, connection.Id);
            }

            return changedItems[0];
        }

        /// <summary>
        /// ADR-144 D-A7-R: a value the tracker accepted becomes the stored copy, so the guard above sees
        /// the truth on the next pass and the duplicate write disappears by construction. Bounded to
        /// successes only and to the value that actually went over the wire; the next inbound sync still
        /// overwrites it.
        /// </summary>
        private async Task PersistWrittenValues(List<PendingWrite> changes, WriteBackResult result)
        {
            var succeeded = result.ItemResults
                .Where(r => r.Success)
                .Select(r => (r.WorkItemId, r.TargetFieldReference))
                .ToHashSet();

            var accepted = changes
                .Where(change => succeeded.Contains((change.Update.WorkItemId, change.Update.TargetFieldReference)))
                .ToList();

            if (accepted.Count == 0)
            {
                return;
            }

            foreach (var change in accepted)
            {
                change.Item.AdditionalFieldValues[change.AdditionalFieldId] = change.Update.Value;
            }

            // Features and Work Items are different tables behind one scoped context, so a single save
            // persists both.
            await featureRepository.Save();
        }

        private sealed record PendingWrite(WriteBackFieldUpdate Update, WorkItemBase Item, int AdditionalFieldId);
    }
}
