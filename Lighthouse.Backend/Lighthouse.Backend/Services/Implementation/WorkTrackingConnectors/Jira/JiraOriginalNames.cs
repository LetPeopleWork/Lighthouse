using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Maps Jira status and issue type ids to the names an English account sees, whatever language the
    /// token's own account reads Jira in. A name the lists cannot supply is reported as the payload gave it.
    /// </summary>
    public sealed class JiraOriginalNames
    {
        private readonly Dictionary<string, string> statusNames = [];

        private readonly Dictionary<string, string> issueTypeNames = [];

        public static JiraOriginalNames AsReported { get; } = new();

        private JiraOriginalNames()
        {
        }

        public static JiraOriginalNames FromLists(JsonElement statusList, JsonElement issueTypeList)
            => AsReported;

        /// <summary>
        /// The original name for the id; the payload name when there is no id to look up; null for a non-null id the
        /// list does not hold, which is never answered with the payload name.
        /// </summary>
        public string? StatusNameFor(string? statusId, string nameInThePayload)
            => statusId is not null && statusNames.TryGetValue(statusId, out var original) ? original : nameInThePayload;

        public string? IssueTypeNameFor(string? issueTypeId, string nameInThePayload)
            => issueTypeId is not null && issueTypeNames.TryGetValue(issueTypeId, out var original) ? original : nameInThePayload;

        /// <summary>The status ids among <paramref name="statusIds"/> that a list which was read fine does not hold.</summary>
        public IReadOnlyList<string> StatusIdsItDoesNotKnow(IEnumerable<string?> statusIds)
            => statusNames.Count == 0 ? [] : [.. statusIds.OfType<string>().Where(id => !statusNames.ContainsKey(id)).Distinct()];

        /// <summary>The issue type ids among <paramref name="issueTypeIds"/> that a list which was read fine does not hold.</summary>
        public IReadOnlyList<string> IssueTypeIdsItDoesNotKnow(IEnumerable<string?> issueTypeIds)
            => issueTypeNames.Count == 0 ? [] : [.. issueTypeIds.OfType<string>().Where(id => !issueTypeNames.ContainsKey(id)).Distinct()];
    }
}
