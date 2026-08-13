using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    public static class WorkItemExtensions
    {
        public static string ExtractStateFromWorkItem(this WorkItem workItem)
        {
            return ExtractFieldFromWorkItem(workItem, AzureDevOpsFieldNames.State);
        }

        public static string ExtractTitleFromWorkItem(this WorkItem workItem)
        {
            return ExtractFieldFromWorkItem(workItem, AzureDevOpsFieldNames.Title);
        }

        public static string ExtractTypeFromWorkItem(this WorkItem workItem)
        {
            return ExtractFieldFromWorkItem(workItem, AzureDevOpsFieldNames.WorkItemType);
        }

        public static string ExtractParentFromWorkItem(this WorkItem workItem)
        {
            if (workItem.Relations != null)
            {
                foreach (var relation in workItem.Relations)
                {
                    if (relation.Attributes.TryGetValue("name", out var attributeValue) && attributeValue.ToString() == "Parent")
                    {
                        var splittedUrl = relation.Url.Split("/");
                        var parentId = splittedUrl[splittedUrl.Length - 1];

                        return parentId ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        public static string ExtractStackRankFromWorkItem(this WorkItem workItem)
        {
            var workItemOrder = string.Empty;
            if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.StackRank, out var stackRank))
            {
                workItemOrder = stackRank?.ToString() ?? string.Empty;
            }
            else if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.BacklogPriority, out var backlogPriority))
            {
                workItemOrder = backlogPriority?.ToString() ?? string.Empty;
            }

            return workItemOrder;
        }

        public static string ExtractUrlFromWorkItem(this WorkItem workItem)
        {
            return ((ReferenceLink)workItem.Links.Links[AzureDevOpsFieldNames.UrlPropertyName])?.Href ?? string.Empty;
        }

        public static DateTime ExtractCreatedDateFromWorkItem(this WorkItem workItem)
        {
            return (DateTime?)workItem.Fields[AzureDevOpsFieldNames.CreatedDate] ?? DateTime.MinValue;
        }

        /// <summary>
        /// When Azure DevOps says this item last changed. Always returned in UTC: the field arrives without a
        /// dependable kind, and this value is both compared against a stored one and written to a column
        /// Postgres rejects a non-UTC instant for.
        /// </summary>
        public static DateTime? ExtractChangedDateFromWorkItem(this WorkItem workItem)
        {
            if (!workItem.Fields.TryGetValue(AzureDevOpsFieldNames.ChangedDate, out var changedDate) || changedDate is not DateTime changed)
            {
                return null;
            }

            return changed.Kind == DateTimeKind.Local
                ? changed.ToUniversalTime()
                : DateTime.SpecifyKind(changed, DateTimeKind.Utc);
        }

        public static List<string> ExtractTagsFromWorkItem(this WorkItem workItem)
        {
            if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.Tags, out var tagsField) && tagsField is string tags)
            {
                return tags.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim()).ToList();
            }

            return new List<string>();
        }

        private static string ExtractFieldFromWorkItem(WorkItem workItem, string fieldName)
        {
            return workItem.Fields[fieldName]?.ToString() ?? string.Empty;
        }
    }
}
