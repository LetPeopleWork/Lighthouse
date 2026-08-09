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
                WarnAboutWatchersWeCouldNotSpare(connection, result);

                return result;
            }
            catch (Exception ex)
            {
                // Stryker disable all: the elapsed time and the wording are diagnostics; the result below is the behaviour
                stopwatch.Stop();

                logger.LogError(ex,
                    "Write-back failed for connection {ConnectionId} ({ConnectionName}) after {ElapsedMs}ms with unhandled exception",
                    connection.Id, connection.Name, stopwatch.ElapsedMilliseconds);
                // Stryker restore all

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

        /// <summary>
        /// One line per connection per flush, naming the projects and the remedy (ADR-142 §6). Deliberately
        /// louder than the <see cref="LogFailedUpdates"/> beside it: until the connection surface ships this
        /// is the only way an administrator learns their write-backs are mailing the team, and a Debug line
        /// is invisible at production log levels.
        ///
        /// Only <c>NotSuppressed</c> counts. A write that failed outright says nothing about permissions,
        /// and naming its project would send the administrator to grant one that was never at fault.
        /// </summary>
        private void WarnAboutWatchersWeCouldNotSpare(WorkTrackingSystemConnection connection, WriteBackResult result)
        {
            var affectedProjects = result.ItemResults
                .Where(itemResult => itemResult.NotificationSuppression == NotificationSuppression.NotSuppressed)
                .Select(itemResult => ProjectOf(itemResult.WorkItemId))
                .Distinct()
                // Stryker disable once Linq: descending is an equally canonical order and names the same projects
                .Order()
                .ToList();

            if (affectedProjects.Count == 0)
            {
                return;
            }

            logger.LogWarning(
                "Write-back on connection {ConnectionId} ({ConnectionName}) emailed the watchers of every item it touched in {Projects} — Lighthouse's credential is not allowed to discard notifications there. Grant it \"Administer Jira\" globally, or \"Administer Projects\" on those projects.",
                connection.Id, connection.Name, string.Join(", ", affectedProjects));
        }

        /// <summary>
        /// The permission is granted per project, so the project is what the warning has to name. A Jira
        /// reference is its issue key; anything else is reported as it stands rather than dropped or folded
        /// into a neighbour, because an item nobody can name is still an item somebody was emailed about.
        /// </summary>
        private static string ProjectOf(string workItemId)
        {
            var separator = workItemId.LastIndexOf('-');

            if (separator > 0 && IsIssueNumber(workItemId.AsSpan(separator + 1)))
            {
                return workItemId[..separator];
            }

            return $"unknown project (item {workItemId})";
        }

        private static bool IsIssueNumber(ReadOnlySpan<char> candidate)
        {
            if (candidate.IsEmpty)
            {
                return false;
            }

            foreach (var character in candidate)
            {
                if (!char.IsAsciiDigit(character))
                {
                    return false;
                }
            }

            return true;
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
