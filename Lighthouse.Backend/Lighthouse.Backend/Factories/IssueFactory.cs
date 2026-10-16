using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Factories
{
    public partial class IssueFactory(ILogger<IssueFactory> logger) : IIssueFactory
    {
        private const string DefaultRank = "00000|";

        private const string DefaultRankField = "customfield_10019";

        // flaggedField stays on the signature because callers still resolve the flagged custom-field key, but the
        // flag now reaches a work item only through its additional field, so this factory no longer reads it.
        public Issue CreateIssueFromJson(JsonElement json, IWorkItemQueryOwner workitemQueryOwner, string? rankFieldName = null, string? flaggedField = null)
        {
            // If the rank field is not set, use the default one and try our luck
            var rankField = !string.IsNullOrEmpty(rankFieldName) ? rankFieldName : DefaultRankField;

            var fields = json.GetProperty(JiraFieldNames.FieldsFieldName).Clone();
            var key = GetKeyFromJson(json);
            var title = GetTitleFromFields(fields);
            var createdDate = GetCreatedDateFromFields(fields);
            var updated = GetUpdatedDateFromFields(fields);
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
                Updated = updated,
                StartedDate = startedDate,
                ClosedDate = closedDate,
                ParentKey = parentKey,
                Rank = rank,
                IssueType = issueType,
                State = state,
                Labels = labels,
                Fields = fields,
                StateTransitions = GetAllStateTransitions(json),
                HasItsWholeHistory = HoldsItsWholeHistory(json),
                StandingLinks = StandingLinksIn(json),
            };
        }

        public IReadOnlyList<WorkItemStateTransition> GetAllStateTransitions(JsonElement json)
        {
            return StatusTransitionsIn(json);
        }

        private static List<WorkItemStateTransition> StatusTransitionsIn(JsonElement json)
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

        private static bool HoldsItsWholeHistory(JsonElement json)
        {
            if (!json.TryGetProperty(JiraFieldNames.ChangelogFieldName, out var changelog))
            {
                return false;
            }

            var histories = changelog.GetProperty(JiraFieldNames.HistoriesFieldName);

            return !HoldsOnlyPartOfItsHistory(changelog, histories)
                && histories.EnumerateArray().All(history => JiraInstantOrNull(StringOrNull(history, JiraFieldNames.CreatedDateFieldName)) is not null);
        }

        private sealed record LinkEnd(string CounterpartKey, string Sentence);

        private sealed record LinkChange(DateTime At, long? HistoryId, LinkEnd? Removed, LinkEnd? Added);

        private const string LinkFieldName = "Link";

        private const string FromPropertyName = "from";

        private const string ToPropertyName = "to";

        private const string HistoryIdPropertyName = "id";

        private const string IssueKeyPattern = "[A-Z][A-Z0-9_]+-[0-9]+";

        [GeneratedRegex("^" + IssueKeyPattern + "$", RegexOptions.CultureInvariant)]
        private static partial Regex AnIssueKey();

        [GeneratedRegex(@"\b" + IssueKeyPattern + @"\b", RegexOptions.CultureInvariant)]
        private static partial Regex AnIssueKeyInASentence();

        // Neither Cloud nor Data Center promises an order for the histories, so they are replayed by when
        // they happened, and two entries stamped at one instant by their history ids, which only grow. An
        // entry with no numeric id goes after those that have one, in the order listed. What stands at the
        // end is each link that still exists, dated by its latest adding. A link is its counterpart together
        // with its sentence, because the sentence is the only place the history names the link's type: a
        // "relates to" to the same issue is another link with its own dates.
        private static List<StandingLink> StandingLinksIn(JsonElement json)
        {
            if (!json.TryGetProperty(JiraFieldNames.ChangelogFieldName, out var changelog))
            {
                return [];
            }

            var histories = changelog.GetProperty(JiraFieldNames.HistoriesFieldName);
            if (HoldsOnlyPartOfItsHistory(changelog, histories))
            {
                return [];
            }

            return [.. histories.EnumerateArray()
                .SelectMany(LinkChangesIn)
                .OrderBy(change => change.At)
                .ThenBy(change => change.HistoryId is null)
                .ThenBy(change => change.HistoryId)
                .Aggregate(new Dictionary<LinkEnd, DateTime>(), Replay)
                .Select(standing => new StandingLink(standing.Key.CounterpartKey, standing.Key.Sentence, standing.Value))];
        }

        // A search answer can carry fewer entries than the history holds, and says so in its total. Any of
        // the missing entries may have removed or re-added a link, so a partial history dates no link at all.
        private static bool HoldsOnlyPartOfItsHistory(JsonElement changelog, JsonElement histories)
            => changelog.TryGetProperty(JiraFieldNames.TotalFieldName, out var total)
                && total.ValueKind == JsonValueKind.Number
                && total.TryGetInt32(out var entriesInAll)
                && entriesInAll > histories.GetArrayLength();

        private static Dictionary<LinkEnd, DateTime> Replay(Dictionary<LinkEnd, DateTime> standing, LinkChange change)
        {
            if (change.Removed != null)
            {
                standing.Remove(change.Removed);
            }

            if (change.Added != null)
            {
                standing[change.Added] = change.At;
            }

            return standing;
        }

        private static IEnumerable<LinkChange> LinkChangesIn(JsonElement history)
        {
            if (JiraInstantOrNull(StringOrNull(history, JiraFieldNames.CreatedDateFieldName)) is not DateTime at)
            {
                return [];
            }

            long? historyId = long.TryParse(StringOrNull(history, HistoryIdPropertyName), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id
                : null;

            return history.GetProperty(JiraFieldNames.ItemsFieldName).EnumerateArray()
                .Where(item => StringOrNull(item, JiraFieldNames.FieldFieldName) == LinkFieldName)
                .Select(item => new LinkChange(
                    at,
                    historyId,
                    LinkEndOn(item, FromPropertyName, JiraFieldNames.FromStringPropertyName),
                    LinkEndOn(item, ToPropertyName, JiraFieldNames.ToStringPropertyName)));
        }

        // A removal writes on its from side the same sentence the adding wrote on its to side, which is what
        // lets a removal end exactly the link it names.
        private static LinkEnd? LinkEndOn(JsonElement item, string keyProperty, string sentenceProperty)
        {
            var counterpartKey = CounterpartKeyOn(item, keyProperty, sentenceProperty);

            return counterpartKey is null
                ? null
                : new LinkEnd(counterpartKey, StringOrNull(item, sentenceProperty) ?? string.Empty);
        }

        // Some Jira versions put a link id where the key would be; then the sentence is the only place the
        // counterpart is named. Its wording is renamable, so only an issue-key-shaped token in it is trusted.
        private static string? CounterpartKeyOn(JsonElement item, string keyProperty, string sentenceProperty)
        {
            var named = StringOrNull(item, keyProperty);
            if (named != null && AnIssueKey().IsMatch(named))
            {
                return named;
            }

            var sentence = StringOrNull(item, sentenceProperty);
            if (sentence == null)
            {
                return null;
            }

            var keysInSentence = AnIssueKeyInASentence().Matches(sentence);
            return keysInSentence.Count == 1 ? keysInSentence[0].Value : null;
        }

        private static string? StringOrNull(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static IEnumerable<WorkItemStateTransition> ExtractStatusTransitionsFromHistory(JsonElement history)
        {
            if (JiraInstantOrNull(StringOrNull(history, JiraFieldNames.CreatedDateFieldName)) is not DateTime transitionedAt)
            {
                return [];
            }

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
            return WorkItemCategoryCrossing.LastEntryInto(
                StatusTransitionsIn(json), targetStates, statesToIgnoreTransition);
        }

        private static DateTime? GetCreatedDateFromFields(JsonElement fields)
        {
            return JiraInstantOrNull(StringOrNull(fields, JiraFieldNames.CreatedDateFieldName)) ?? DateTime.MinValue;
        }

        // Missing or unreadable stays null rather than a sentinel, which would claim knowledge the tracker never
        // gave; a null makes the next update download everything again.
        private static DateTime? GetUpdatedDateFromFields(JsonElement fields)
        {
            return JiraInstantOrNull(fields.GetFieldValue(JiraFieldNames.UpdatedFieldName));
        }

        // A Jira stamp carries its own offset. Parsed as a DateTime it would come out in the server's local time,
        // so only reading it as a DateTimeOffset keeps the instant it names whatever zone the server runs in.
        private static DateTime? JiraInstantOrNull(string? stamp)
        {
            return DateTimeOffset.TryParse(stamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var at)
                ? at.UtcDateTime
                : null;
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
