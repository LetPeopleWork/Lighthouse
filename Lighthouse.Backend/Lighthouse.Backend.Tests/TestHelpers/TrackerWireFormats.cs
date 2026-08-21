namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What a Jira response looks like on the wire, in the one place that says so. Both the mapper's own
    /// tests and the cross-connector parity scenarios need to hand the connector a payload, and two
    /// descriptions of the same shape are two chances to test against a Jira that does not exist.
    /// </summary>
    public static class JiraWireFormat
    {
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
        {
            var fields = "{\"summary\": \"" + summary + "\""
                + ", \"issuetype\": {\"name\": \"Epic\"}"
                + ", \"status\": {\"name\": \"In Progress\"}"
                + ", \"created\": \"2026-01-01T00:00:00.000+0000\""
                + ", \"updated\": \"2026-01-02T00:00:00.000+0000\""
                + ", \"labels\": []"
                + ", \"issuelinks\": [" + string.Join(",", links) + "]}";

            return "{\"key\": \"" + key + "\", \"fields\": " + fields + "}";
        }

        /// <summary>This issue is waiting on <paramref name="key"/>.</summary>
        public static string BlockedByLink(string key) => InwardLink("is blocked by", key);

        /// <summary>This issue is waiting on <paramref name="key"/>, under a link named something else.</summary>
        public static string InwardLink(string inwardName, string key) => Link("inwardIssue", inwardName, key);

        /// <summary>The far end of somebody else's dependency: this issue blocks <paramref name="key"/>.</summary>
        public static string BlocksLink(string key) => Link("outwardIssue", "is blocked by", key);

        private static string Link(string end, string inwardName, string key)
        {
            var type = "{\"name\": \"Blocks\", \"inward\": \"" + inwardName + "\", \"outward\": \"blocks\"}";
            var issue = "{\"key\": \"" + key + "\", \"fields\": {\"summary\": \"Something\"}}";

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
