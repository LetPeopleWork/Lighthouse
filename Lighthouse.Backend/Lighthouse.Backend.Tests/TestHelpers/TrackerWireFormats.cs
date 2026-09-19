namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What a Jira response looks like on the wire, in the one place that says so. Both the mapper's own
    /// tests and the cross-connector parity scenarios need to hand the connector a payload, and two
    /// descriptions of the same shape are two chances to test against a Jira that does not exist.
    /// </summary>
    public static class JiraWireFormat
    {
        private const string EpicIssueType = "Epic";

        /// <summary>
        /// The routing a Cloud connector walks before it ever reaches a search: it asks which deployment
        /// it is talking to, then for the field definitions, and only then for the issues.
        /// </summary>
        public static string ACloudResponseTo(HttpRequestMessage request, IEnumerable<string> issues)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal))
            {
                return "{\"deploymentType\":\"Cloud\"}";
            }

            if (path.EndsWith("rest/api/latest/field", StringComparison.Ordinal))
            {
                return "[]";
            }

            if (path.Contains("/search", StringComparison.Ordinal))
            {
                return "{\"issues\":[" + string.Join(",", issues) + "],\"isLast\":true}";
            }

            return "{}";
        }

        public static string AnEpic(string key, params string[] links) => AnEpicNamed(key, $"{key} summary", links);

        public static string AnEpicNamed(string key, string summary, params string[] links)
            => AnIssue(key, TheFieldsOf(summary, EpicIssueType, links), changelogEntries: null);

        /// <summary>
        /// An issue at whatever grain the caller needs. Only a Portfolio is made of Epics; a Team's items
        /// carry whichever type the instance calls them, and a fixture fixed at Epic cannot say so.
        /// </summary>
        public static string AnIssueOfType(string key, string issueType, params string[] links)
            => AnIssue(key, TheFieldsOf($"{key} summary", issueType, links), changelogEntries: null);

        /// <summary>
        /// An issue whose history is long enough that the connector will not trust the copy that came with
        /// the search result, and re-reads it on the issue's own changelog endpoint. The threshold is thirty.
        /// </summary>
        public static string AnIssueWithAChangelogOf(string key, int entries, params string[] links)
            => AnIssue(key, TheFieldsOf($"{key} summary", EpicIssueType, links), entries);

        private static string AnIssue(string key, string fields, int? changelogEntries)
        {
            var changelog = changelogEntries is null
                ? string.Empty
                : ", \"changelog\": {\"total\": " + changelogEntries + "}";

            return "{\"key\": \"" + key + "\", \"fields\": " + fields + changelog + "}";
        }

        private static string TheFieldsOf(string summary, string issueType, string[] links)
            => "{\"summary\": \"" + summary + "\""
                + ", \"issuetype\": {\"name\": \"" + issueType + "\"}"
                + ", \"status\": {\"name\": \"In Progress\"}"
                + ", \"created\": \"2026-01-01T00:00:00.000+0000\""
                + ", \"updated\": \"2026-01-02T00:00:00.000+0000\""
                + ", \"labels\": []"
                + ", \"issuelinks\": [" + string.Join(",", links) + "]}";

        /// <summary>One page of that re-read. The connector keeps asking until a page says it is the last.</summary>
        public static string AChangelogPage(int entries, bool isLast)
            => "{\"values\": [], \"total\": " + entries + ", \"isLast\": " + (isLast ? "true" : "false") + "}";

        /// <summary>This issue is waiting on <paramref name="key"/>.</summary>
        public static string BlockedByLink(string key) => InwardLink("is blocked by", key);

        /// <summary>This issue is waiting on <paramref name="key"/>, under a link named something else.</summary>
        public static string InwardLink(string inwardName, string key)
            => JiraLinkType.Blocks(inwardName).LinkWhoseInwardIssueIs(key);

        /// <summary>The far end of somebody else's dependency: this issue blocks <paramref name="key"/>.</summary>
        public static string BlocksLink(string key)
            => JiraLinkType.Blocks("is blocked by").LinkWhoseOutwardIssueIs(key);
    }

    /// <summary>
    /// A link type as Jira defines it: the name an administrator sees, plus the label the link wears from
    /// each end. Jira writes a link once and serves it from both ends, handing each issue a pointer to the
    /// other one - so an entry naming an inwardIssue sits on the issue holding the outward end, and a
    /// fixture that only ever builds one of the two shapes tests half of what an instance can send.
    /// </summary>
    public sealed record JiraLinkType(string Name, string Inward, string Outward)
    {
        /// <summary>The Blocks type as Jira ships it, save for an inward label an administrator may have renamed.</summary>
        public static JiraLinkType Blocks(string inwardName) => new("Blocks", inwardName, "blocks");

        /// <summary>An entry on the issue holding the outward end, pointing at <paramref name="counterpartKey"/>.</summary>
        public string LinkWhoseInwardIssueIs(string counterpartKey) => LinkTo("inwardIssue", counterpartKey);

        /// <summary>An entry on the issue holding the inward end, pointing at <paramref name="counterpartKey"/>.</summary>
        public string LinkWhoseOutwardIssueIs(string counterpartKey) => LinkTo("outwardIssue", counterpartKey);

        private string LinkTo(string end, string counterpartKey)
        {
            var type = "{\"name\": \"" + Name + "\", \"inward\": \"" + Inward + "\", \"outward\": \"" + Outward + "\"}";
            var issue = "{\"key\": \"" + counterpartKey + "\", \"fields\": {\"summary\": \"Something\"}}";

            return "{\"type\": " + type + ", \"" + end + "\": " + issue + "}";
        }
    }

    /// <summary>
    /// What a Linear projects query answers with. The relation type is <c>dependency</c> because that is
    /// the only value the real API accepts - it rejects <c>blocks</c> outright, whatever the published
    /// schema's example says - and a fixture carrying a value no workspace can produce is a fixture
    /// testing a tracker nobody has.
    /// </summary>
    public static class LinearWireFormat
    {
        public static string ProjectsResponse(params string[] projects)
            => "{\"data\": {\"projects\": {\"nodes\": [" + string.Join(",", projects) + "]"
                + ", \"pageInfo\": {\"hasNextPage\": false, \"endCursor\": null}}}}";

        public static string AProject(string id, string name, string inverseRelations)
            => "{\"id\": \"" + id + "\""
                + ", \"name\": \"" + name + "\""
                + ", \"status\": {\"id\": \"s1\", \"name\": \"Active\"}"
                + ", \"url\": \"https://linear.app/" + id + "\""
                + ", \"sortOrder\": 1.0"
                + ", \"createdAt\": \"2026-01-01T00:00:00.000Z\""
                + ", \"inverseRelations\": " + inverseRelations + "}";

        /// <summary>
        /// The relations where this Project is the target, so their source is what it waits on. Linear
        /// hands the same relation to the other end as one of its own <c>relations</c>, which this
        /// deliberately never builds: a payload written that way round would pass a mapper reading the
        /// wrong side.
        /// </summary>
        public static string BlockedBy(params string[] blockerIds)
        {
            var nodes = Array.ConvertAll(
                blockerIds,
                id => "{\"type\": \"dependency\", \"project\": {\"id\": \"" + id + "\"}}");

            return "{\"nodes\": [" + string.Join(",", nodes) + "]}";
        }

        public static string BlockedByNothing() => "{\"nodes\": []}";
    }
}
