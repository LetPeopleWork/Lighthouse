using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira;
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

        public IssueFactory(ILexoRankService lexoRankService, ILogger<IssueFactory> logger, IConfiguration configuration)
        {
            this.lexoRankService = lexoRankService;
            this.logger = logger;

            var rankFieldOverride = configuration["WorkTrackingSystems:Jira:RankFieldOverride"];
            if (!string.IsNullOrEmpty(rankFieldOverride))
            {
                rankCustomFieldNames.Clear();
                rankCustomFieldNames.Add(rankFieldOverride);
            }
        }

        public Issue CreateIssueFromJson(JsonElement json, IWorkItemQueryOwner workitemQueryOwner, string? additionalRelatedField = null)
        {
            var fields = json.GetProperty(JiraFieldNames.FieldsFieldName);
            var key = GetKeyFromJson(json);
            var title = GetTitleFromFields(fields);
            var createdDate = GetCreatedDateFromFields(fields);
            var parentKey = GetParentFromFields(fields);
            var rank = GetRankFromFields(fields);
            var issueType = GetIssueTypeFromFields(fields);
            var state = GetStateFromFields(fields);

            if (!string.IsNullOrEmpty(additionalRelatedField))
            {
                parentKey = fields.GetFieldValue(additionalRelatedField);
            }

            (var startedDate, var closedDate) = GetStartedAndClosedDate(json, workitemQueryOwner, state);

            return new Issue(key, title, createdDate, closedDate, startedDate, parentKey, rank, issueType, state, fields);
        }

        private static (DateTime? startedDate, DateTime? closedDate) GetStartedAndClosedDate(JsonElement json, IWorkItemQueryOwner workitemQueryOwner, string state)
        {
            var stateCategory = workitemQueryOwner.MapStateToStateCategory(state);

            // If the StateCategory is ToDo or Unknown, we have neither started nor finished
            DateTime? closedDate = null;
            DateTime? startedDate = null;

            if (stateCategory == StateCategories.Done)
            {
                closedDate = GetTransitionDate(json, workitemQueryOwner.DoneStates, []);
                startedDate = GetTransitionDate(json, workitemQueryOwner.DoingStates, workitemQueryOwner.DoneStates);
            }
            else if (stateCategory == StateCategories.Doing)
            {
                startedDate = GetTransitionDate(json, workitemQueryOwner.DoingStates, workitemQueryOwner.DoneStates);
            }

            // It can happen that no started date is set if an item is created directly in closed state. Assume that the closed date is the started date in this case.
            if (startedDate == null && closedDate != null)
            {
                startedDate = closedDate;
            }

            return (startedDate, closedDate);
        }

        private static string GetIssueTypeFromFields(JsonElement fields)
        {
            return fields.GetProperty(JiraFieldNames.IssueTypeFieldName).GetProperty(JiraFieldNames.NamePropertyName).ToString();
        }

        private static string GetStateFromFields(JsonElement fields)
        {
            return fields.GetProperty(JiraFieldNames.StatusFieldName).GetProperty(JiraFieldNames.NamePropertyName).ToString();
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

                    if (!string.IsNullOrEmpty(rank) && rank.Contains('|'))
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

        private static DateTime? GetTransitionDate(JsonElement json, IEnumerable<string> targetStates, IEnumerable<string> statesToIgnoreTransition)
        {
            var movedToStateCategory = new List<DateTime>();

            if (json.TryGetProperty(JiraFieldNames.ChangelogFieldName, out JsonElement changelog))
            {
                var histories = changelog.GetProperty(JiraFieldNames.HistoriesFieldName);
                foreach (var history in histories.EnumerateArray())
                {
                    var extractedDate = ExtractDateOfStateTransitionFromHistory(targetStates, statesToIgnoreTransition, history);

                    if (extractedDate.HasValue)
                    {
                        movedToStateCategory.Add(extractedDate.Value);
                    }
                }
            }

            var lastTransitionDate = movedToStateCategory.OrderByDescending(date => date).FirstOrDefault();
            if (lastTransitionDate == default)
            {
                return null;
            }

            return lastTransitionDate.ToUniversalTime();
        }

        private static DateTime? ExtractDateOfStateTransitionFromHistory(IEnumerable<string> targetStates, IEnumerable<string> statesToIgnoreTransitions, JsonElement history)
        {
            var historyEntryCreationDateAsString = history.GetProperty(JiraFieldNames.CreatedDateFieldName).GetString() ?? string.Empty;
            var historyEntryCreationDate = DateTime.Parse(historyEntryCreationDateAsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            DateTime? transitionDate = null;

            foreach (var item in history.GetProperty(JiraFieldNames.ItemsFieldName).EnumerateArray())
            {
                var changedField = item.GetProperty(JiraFieldNames.FieldFieldName).GetString();
                var newStatus = item.GetProperty(JiraFieldNames.ToStringPropertyName).GetString();
                var oldStatus = item.GetProperty(JiraFieldNames.FromStringPropertyName).GetString();

                if (changedField == JiraFieldNames.StatusFieldName && targetStates.Contains(newStatus) && !targetStates.Contains(oldStatus) && !statesToIgnoreTransitions.Contains(oldStatus))
                {
                    transitionDate = historyEntryCreationDate;
                }
            }

            return transitionDate?.ToUniversalTime();
        }

        private static DateTime? GetCreatedDateFromFields(JsonElement fields)
        {
            var createdDateAsString = fields.GetProperty(JiraFieldNames.CreatedDateFieldName).GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(createdDateAsString))
            {
                return DateTime.MinValue;
            }

            var createdDate = DateTime.Parse(createdDateAsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            return createdDate.ToUniversalTime();
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
