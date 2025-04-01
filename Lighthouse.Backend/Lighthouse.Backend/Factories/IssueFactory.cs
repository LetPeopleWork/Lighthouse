using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItemServices;
using Lighthouse.Backend.WorkTracking.Jira;
using System.Globalization;
using System.Text.Json;

namespace Lighthouse.Backend.Factories
{
    public class IssueFactory : IIssueFactory
    {
        private readonly ILexoRankService lexoRankService;
        private readonly ILogger<IssueFactory> logger;

        // customfield_10019 is how Jira stores the rank.
        private static readonly List<string> rankCustomFieldNames = new List<string> { "customfield_10019" };

        public IssueFactory(ILexoRankService lexoRankService, ILogger<IssueFactory> logger)
        {
            this.lexoRankService = lexoRankService;
            this.logger = logger;
        }

        public Issue CreateIssueFromJson(JsonElement json, IWorkItemQueryOwner workitemQueryOwner, string? additionalRelatedField = null)
        {
            var fields = json.GetProperty(JiraFieldNames.FieldsFieldName);
            var key = GetKeyFromJson(json);
            var title = GetTitleFromFields(fields);
            var closedDate = GetTransitionDate(json, workitemQueryOwner.DoneStates, JiraFieldNames.ResolutionDateFieldName);
            var startedDate = GetTransitionDate(json, workitemQueryOwner.ToDoStates, JiraFieldNames.CreatedDateFieldName);
            var parentKey = GetParentFromFields(fields);
            var rank = GetRankFromFields(fields);
            var issueType = GetIssueTypeFromFields(fields);
            var state = GetStateFromFields(fields);
            var statusCategory = GetStatusCategoryFromFields(fields);

            if (string.IsNullOrEmpty(parentKey) && !string.IsNullOrEmpty(additionalRelatedField))
            {
                parentKey = fields.GetFieldValue(additionalRelatedField);
            }

            logger.LogDebug("Creating Issue with Key {Key}, Title {Title}, Closed Date {ClosedDate}, Parent Key {ParentKey}, Rank {Rank}, Issue Type {IssueType}, Status {Status}, Status Category {StatusCategory}", key, title, closedDate, parentKey, rank, issueType, state, statusCategory);

            return new Issue(key, title, closedDate, startedDate, parentKey, rank, issueType, state, statusCategory, fields);
        }

        private static string GetIssueTypeFromFields(JsonElement fields)
        {
            return fields.GetProperty(JiraFieldNames.IssueTypeFieldName).GetProperty(JiraFieldNames.NamePropertyName).ToString();
        }

        private static string GetStateFromFields(JsonElement fields)
        {
            return fields.GetProperty(JiraFieldNames.StatusFieldName).GetProperty(JiraFieldNames.NamePropertyName).ToString();
        }

        private static string GetStatusCategoryFromFields(JsonElement fields)
        {
            return fields.GetProperty(JiraFieldNames.StatusFieldName).GetProperty(JiraFieldNames.StatusCategoryFieldName).GetProperty(JiraFieldNames.NamePropertyName).ToString();
        }

        private string GetRankFromFields(JsonElement fields)
        {
            // Try getting the ranks using the default field or previously used fields. It's a string, not an int. It's using the LexoGraph algorithm for this.
            var rank = string.Empty;

            if (TryGetRankFromKnownFields(fields, out rank))
            {
                return rank;
            }

            logger.LogInformation("Could not find rank in default field, parsing all fields for LexoRank...");
            // It's possible that Jira is using a different custom field for the rank - try to find it via searching through the available properties.
            var rankFields = fields.EnumerateObject().Where(f => f.Value.ToString().Contains('|'));
            if (rankFields.Any())
            {
                var field = rankFields.First();
                rank = field.Value.ToString();
                logger.LogInformation("Found rank in field {Name}: {Rank}. Storing for next use", field.Name, rank);

                rankCustomFieldNames.Add(field.Name);

                return rank;
            }

            return lexoRankService.Default;
        }

        private static bool TryGetRankFromKnownFields(JsonElement fields, out string rank)
        {
            rank = string.Empty;

            foreach (var customField in rankCustomFieldNames)
            {
                if (fields.TryGetProperty(customField, out var parsedRank))
                {
                    rank = parsedRank.ToString();

                    if (!string.IsNullOrEmpty(rank))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        private static string GetParentFromFields(JsonElement fields)
        {
            var parentKey = string.Empty;
            if (fields.TryGetProperty(JiraFieldNames.ParentFieldName, out var parent))
            {
                parentKey = parent.GetProperty(JiraFieldNames.KeyPropertyName).ToString();
            }

            return parentKey;
        }

        private static DateTime GetTransitionDate(JsonElement json, IEnumerable<string> targetStates, string defaultField)
        {
            var transitionDate = DateTime.MinValue;

            if (json.TryGetProperty(JiraFieldNames.ChangelogFieldName, out JsonElement changelog))
            {
                var histories = changelog.GetProperty(JiraFieldNames.HistoriesFieldName);
                foreach (var history in histories.EnumerateArray())
                {
                    var extractedDate = ExtractDateFromHistory(targetStates, history);

                    if (extractedDate.HasValue)
                    {
                        transitionDate = extractedDate.Value;
                    }
                }
            }

            if (transitionDate == DateTime.MinValue)
            {
                transitionDate = GetDateByFieldName(defaultField, json);
            }

            return transitionDate;
        }

        private static DateTime? ExtractDateFromHistory(IEnumerable<string> targetStates, JsonElement history)
        {
            var historyEntryCreationDateAsString = history.GetProperty(JiraFieldNames.CreatedDateFieldName).GetString() ?? string.Empty;
            var historyEntryCreationDate = DateTime.Parse(historyEntryCreationDateAsString, CultureInfo.InvariantCulture);

            DateTime? transitionDate = null;

            foreach (var item in history.GetProperty(JiraFieldNames.ItemsFieldName).EnumerateArray())
            {
                var changedField = item.GetProperty(JiraFieldNames.FieldFieldName).GetString();
                var newStatus = item.GetProperty(JiraFieldNames.ToStringPropertyName).GetString();

                if (changedField == JiraFieldNames.StatusFieldName && targetStates.Contains(newStatus))
                {
                    transitionDate = historyEntryCreationDate;
                }
            }

            return transitionDate;
        }

        private static DateTime GetDateByFieldName(string fieldName, JsonElement json)
        {
            var fields = json.GetProperty(JiraFieldNames.FieldsFieldName);
            var defaultFieldDateString = fields.GetProperty(fieldName).GetString();
            if (!string.IsNullOrEmpty(defaultFieldDateString))
            {
                return DateTime.Parse(defaultFieldDateString, CultureInfo.InvariantCulture);
            }

            return DateTime.MinValue;
        }

        private static string GetTitleFromFields(JsonElement fields)
        {
            return fields.GetFieldValue(JiraFieldNames.SummaryFieldName);
        }

        private static string GetKeyFromJson(JsonElement json)
        {
            return json.GetProperty(JiraFieldNames.KeyPropertyName).ToString();
        }
    }
}
