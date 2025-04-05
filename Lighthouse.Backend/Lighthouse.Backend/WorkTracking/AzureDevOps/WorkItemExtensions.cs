using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Mono.TextTemplating;

namespace Lighthouse.Backend.WorkTracking.AzureDevOps
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

        private static string ExtractFieldFromWorkItem(WorkItem workItem, string fieldName)
        {
            return workItem.Fields[fieldName]?.ToString() ?? string.Empty;
        }
    }
}
