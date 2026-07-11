using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using System.Globalization;
using System.Text.Json;

namespace Lighthouse.Backend.Factories
{
    public class IssueFactory(ILogger<IssueFactory> logger) : IIssueFactory
    {
        private const string DefaultRank = "00000|";

        private const string DefaultRankField = "customfield_10019";

        // flaggedField is retained on the contract (callers still resolve the flagged custom-field key), but the
        // synthetic "Flagged" label injection is gone (ADR-071 slice-05): the flag now flows only through the
        // predefined additional field (generic id-keyed path), so the factory no longer consumes this value.
        public Issue CreateIssueFromJson(JsonElement json, IWorkItemQueryOwner workitemQueryOwner, string? rankFieldName = null, string? flaggedField = null)
        {
            // If the rank field is not set, use the default one and try our luck
            var rankField = !string.IsNullOrEmpty(rankFieldName) ? rankFieldName : DefaultRankField;

            var fields = json.GetProperty(JiraFieldNames.FieldsFieldName).Clone();
            var key = GetKeyFromJson(json);
            var title = GetTitleFromFields(fields);
            var createdDate = GetCreatedDateFromFields(fields);
            var parentKey = GetParentFromFields(fields);
            var labels = GetLabelsFromFields(fields);
            var rank = GetRankFromFields(fields, rankField);
            var issueType = GetIssueTypeFromFields(fields);
            var state = GetStateFromFields(fields);

            var (startedDate, closedDate) = GetStartedAndClosedDate(json, workitemQueryOwner, state);

            return new Issue
            {
                Key = key,
                Title = title,
                CreatedDate = createdDate,
                StartedDate = startedDate,
                ClosedDate = closedDate,
                ParentKey = parentKey,
                Rank = rank,
                IssueType = issueType,
                State = state,
                Labels = labels,
                Fields = fields,
                StateTransitions = GetAllStateTransitions(json),
            };
        }

        public IReadOnlyList<WorkItemStateTransition> GetAllStateTransitions(JsonElement json)
        {
            if (!json.TryGetProperty(JiraFieldNames.ChangelogFieldName, out var changelog))
            {
                return [];
            }

            var histories = changelog.GetProperty(JiraFieldNames.HistoriesFieldName);

            return histories.EnumerateArray()
                .SelectMany(ExtractStatusTransitionsFromHistory)
                .ToList();
        }

        private static IEnumerable<WorkItemStateTransition> ExtractStatusTransitionsFromHistory(JsonElement history)
        {
            var historyEntryCreationDateAsString = history.GetProperty(JiraFieldNames.CreatedDateFieldName).GetString() ?? string.Empty;
            var historyEntryCreationDate = DateTime.Parse(historyEntryCreationDateAsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            var transitionedAt = DateTime.SpecifyKind(historyEntryCreationDate, DateTimeKind.Utc);

            return history.GetProperty(JiraFieldNames.ItemsFieldName).EnumerateArray()
                .Where(item => item.GetProperty(JiraFieldNames.FieldFieldName).GetString() == JiraFieldNames.StatusFieldName)
                .Select(item => new WorkItemStateTransition
                {
                    FromState = item.GetProperty(JiraFieldNames.FromStringPropertyName).GetString() ?? string.Empty,
                    ToState = item.GetProperty(JiraFieldNames.ToStringPropertyName).GetString() ?? string.Empty,
                    TransitionedAt = transitionedAt,
                });
        }

        private static (DateTime? startedDate, DateTime? closedDate) GetStartedAndClosedDate(JsonElement json, IWorkItemQueryOwner workitemQueryOwner, string state)
        {
            var stateCategory = workitemQueryOwner.MapStateToStateCategory(state);

            var rawToDoStates = workitemQueryOwner.GetRawStatesForCategory(workitemQueryOwner.ToDoStates);
            var rawDoingStates = workitemQueryOwner.GetRawStatesForCategory(workitemQueryOwner.DoingStates);
            var rawDoneStates = workitemQueryOwner.GetRawStatesForCategory(workitemQueryOwner.DoneStates);

            DateTime? closedDate = null;
            DateTime? startedDate = null;

            if (stateCategory == StateCategories.Done)
            {
                closedDate = GetTransitionDate(json, rawDoneStates, []);
                startedDate = GetTransitionDate(json, rawDoingStates, rawDoneStates);

                var lastToDoEntryDate = GetTransitionDate(json, rawToDoStates, rawDoneStates);
                if (lastToDoEntryDate.HasValue && startedDate.HasValue && lastToDoEntryDate.Value > startedDate.Value)
                {
                    startedDate = null;
                }
            }
            else if (stateCategory == StateCategories.Doing)
            {
                startedDate = GetTransitionDate(json, rawDoingStates, rawDoneStates);
            }

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

        private string GetRankFromFields(JsonElement fields, string rankFieldName)
        {
            // Try getting the ranks using the default field or previously used fields. It's a string, not an int. It's using the LexoGraph algorithm for this.
            if (TryGetRankFromRankField(fields, rankFieldName, out var rank))
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
                logger.LogInformation("Found rank in field {Name}: {Rank}", field.Name, rank);

                return rank;
            }

            return DefaultRank;
        }

        private static bool TryGetRankFromRankField(JsonElement fields, string rankFieldName, out string rank)
        {
            rank = string.Empty;

            if (fields.TryGetProperty(rankFieldName, out var parsedRank))
            {
                rank = parsedRank.ToString();

                if (!string.IsNullOrEmpty(rank) && rank.Contains('|'))
                {
                    return true;
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

        private static List<string> GetLabelsFromFields(JsonElement fields)
        {
            var labels = new List<string>();

            if (fields.TryGetProperty(JiraFieldNames.LabelsFieldName, out var labelsElement))
            {
                foreach (var label in labelsElement.EnumerateArray())
                {
                    labels.Add(label.ToString());
                }
            }

            return labels;
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

            return DateTime.SpecifyKind(lastTransitionDate, DateTimeKind.Utc);
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

                if (changedField == JiraFieldNames.StatusFieldName && targetStates.IsItemInList(newStatus) && !targetStates.IsItemInList(oldStatus) && !statesToIgnoreTransitions.IsItemInList(oldStatus))
                {
                    transitionDate = historyEntryCreationDate;
                }
            }

            if (transitionDate == null)
            {
                return null;
            }

            return DateTime.SpecifyKind(transitionDate.Value, DateTimeKind.Utc);
        }

        private static DateTime? GetCreatedDateFromFields(JsonElement fields)
        {
            var createdDateAsString = fields.GetProperty(JiraFieldNames.CreatedDateFieldName).GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(createdDateAsString))
            {
                return DateTime.MinValue;
            }

            var createdDate = DateTime.Parse(createdDateAsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            return DateTime.SpecifyKind(createdDate, DateTimeKind.Utc);
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
