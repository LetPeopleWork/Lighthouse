using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Maps Jira status and issue type ids to the names an English account sees, whatever language the
    /// token's own account reads Jira in. A name the lists cannot supply is reported as the payload gave it.
    /// </summary>
    public sealed class JiraOriginalNames
    {
        private readonly Dictionary<string, string> statusNames;

        private readonly Dictionary<string, string> issueTypeNames;

        private readonly bool readsNamesAsReported;

        private readonly Dictionary<string, (string? Key, string? Name)> statusCategories;

        public static JiraOriginalNames AsReported { get; } = new([], [], [], true);

        private JiraOriginalNames(
            Dictionary<string, string> statusNames,
            Dictionary<string, (string? Key, string? Name)> statusCategories,
            Dictionary<string, string> issueTypeNames,
            bool readsNamesAsReported)
        {
            this.statusNames = statusNames;
            this.statusCategories = statusCategories;
            this.issueTypeNames = issueTypeNames;
            this.readsNamesAsReported = readsNamesAsReported;
        }

        public static JiraOriginalNames FromLists(JsonElement statusList, JsonElement issueTypeList)
            => new(NamesById(statusList), CategoriesById(statusList), NamesById(issueTypeList), false);

        /// <summary>The names and categories of a status list read on its own, as Data Center gives them.</summary>
        public static JiraOriginalNames FromStatusList(JsonElement statusList)
            => new(NamesById(statusList), CategoriesById(statusList), [], false);

        /// <summary>The language-independent key and the display name of the status category the id belongs to.</summary>
        public (string? Key, string? Name) StatusCategoryOf(string statusId)
            => statusCategories.GetValueOrDefault(statusId);

        private static Dictionary<string, (string? Key, string? Name)> CategoriesById(JsonElement list)
        {
            var categories = new Dictionary<string, (string? Key, string? Name)>();
            if (list.ValueKind != JsonValueKind.Array)
            {
                return categories;
            }

            foreach (var entry in list.EnumerateArray())
            {
                var id = TextOf(entry, "id");
                if (id is not null && entry.TryGetProperty("statusCategory", out var category))
                {
                    categories[id] = (TextOf(category, "key"), TextOf(category, "name"));
                }
            }

            return categories;
        }

        /// <summary>
        /// The original name for the id; the payload name when there is no id to look up; null for a non-null id the
        /// list does not hold, which is never answered with the payload name.
        /// </summary>
        public string? StatusNameFor(string? statusId, string nameInThePayload)
            => NameFor(statusNames, statusId, nameInThePayload);

        public string? IssueTypeNameFor(string? issueTypeId, string nameInThePayload)
            => NameFor(issueTypeNames, issueTypeId, nameInThePayload);

        /// <summary>The status ids among <paramref name="statusIds"/> that the list does not hold.</summary>
        public IReadOnlyList<string> StatusIdsItDoesNotKnow(IEnumerable<string?> statusIds)
            => IdsMissingFrom(statusNames, statusIds);

        /// <summary>The issue type ids among <paramref name="issueTypeIds"/> that the list does not hold.</summary>
        public IReadOnlyList<string> IssueTypeIdsItDoesNotKnow(IEnumerable<string?> issueTypeIds)
            => IdsMissingFrom(issueTypeNames, issueTypeIds);

        private string? NameFor(Dictionary<string, string> names, string? id, string nameInThePayload)
        {
            if (id is null || readsNamesAsReported)
            {
                return nameInThePayload;
            }

            return names.GetValueOrDefault(id);
        }

        private List<string> IdsMissingFrom(Dictionary<string, string> names, IEnumerable<string?> ids)
            => readsNamesAsReported ? [] : [.. ids.OfType<string>().Where(id => !names.ContainsKey(id)).Distinct()];

        private static Dictionary<string, string> NamesById(JsonElement list)
        {
            var names = new Dictionary<string, string>();
            if (list.ValueKind != JsonValueKind.Array)
            {
                return names;
            }

            foreach (var entry in list.EnumerateArray())
            {
                var id = TextOf(entry, "id");
                var name = TextOf(entry, "untranslatedName") ?? TextOf(entry, "name");
                if (id is not null && name is not null)
                {
                    names[id] = name;
                }
            }

            return names;
        }

        private static string? TextOf(JsonElement entry, string property)
            => entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null;
    }
}
