using System.Globalization;

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
        /// The endpoints a Jira connector reaches for. A fixture naming them for itself is what lets it
        /// notice one of them changing; reading them off the connector would make every such test agree
        /// with whatever the connector now asks for.
        /// </summary>
        public const string ServerInfoEndpoint = "rest/api/2/serverInfo";

        /// <summary>The one call that actually establishes who Lighthouse is signed in to Jira as.</summary>
        public const string CredentialCheckEndpoint = "rest/api/2/myself";

        public const string FieldListEndpoint = "rest/api/latest/field";

        public const string IssueLinkTypeEndpoint = "rest/api/latest/issueLinkType";

        public const string SearchEndpoint = "/search";

        /// <summary>What the deployment probe answers on a Cloud instance.</summary>
        public const string ACloudDeployment = "{\"deploymentType\":\"Cloud\"}";

        private const string EpicIssueType = "Epic";

        /// <summary>The id this gives the first custom field an instance is described as defining.</summary>
        public const int TheFirstCustomFieldId = 10100;

        /// <summary>
        /// The routing a Cloud connector walks before it ever reaches a search: it asks which deployment
        /// it is talking to, then for the field definitions, and only then for the issues.
        /// </summary>
        public static string ACloudResponseTo(HttpRequestMessage request, IEnumerable<string> issues)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith(ServerInfoEndpoint, StringComparison.Ordinal))
            {
                return ACloudDeployment;
            }

            if (path.EndsWith(FieldListEndpoint, StringComparison.Ordinal))
            {
                return TheFieldListDefining([]);
            }

            if (path.Contains(SearchEndpoint, StringComparison.Ordinal))
            {
                return OnePageOf(issues);
            }

            return "{}";
        }

        /// <summary>
        /// A search answered in full, with nothing left to page for. Every fixture describing an instance
        /// hands its issues back this way, so the envelope the connector reads is written once.
        /// </summary>
        public static string OnePageOf(IEnumerable<string> issues)
            => "{\"issues\":[" + string.Join(",", issues) + "],\"isLast\":true}";

        /// <summary>
        /// The custom fields an instance defines, as Jira Cloud writes them - every field carrying a "key"
        /// as well as an id. Ids are handed out in order from <see cref="TheFirstCustomFieldId"/>, so a
        /// fixture that has to name one can work out which it will be.
        /// </summary>
        public static string TheFieldListDefining(IEnumerable<string> fieldNames)
        {
            var fields = fieldNames.Select((name, index) =>
            {
                var id = TheCustomFieldIdAt(index);

                return "{\"id\":\"" + id + "\",\"key\":\"" + id + "\",\"name\":\"" + name
                    + "\",\"custom\":true,\"schema\":{\"type\":\"string\"}}";
            });

            return "[" + string.Join(",", fields) + "]";
        }

        public static string TheCustomFieldIdAt(int index)
            => "customfield_" + (TheFirstCustomFieldId + index).ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The link-type list as a live Cloud instance answered it: an object keyed issueLinkTypes, not a
        /// bare array and not the values-plus-isLast envelope the field and search endpoints use, and with
        /// no paging to carry.
        /// </summary>
        public static string TheLinkTypeListDefining(IEnumerable<string> linkTypeDefinitions)
            => "{\"issueLinkTypes\":[" + string.Join(",", linkTypeDefinitions) + "]}";

        /// <summary>
        /// One entry of that list. Each of a link type's three labels is renamed on its own, and Jira
        /// writes a label nobody filled in by leaving the property out rather than by sending an empty
        /// one - which is why a label may be handed over as null here.
        /// </summary>
        public static string ALinkTypeDefinition(int index, string? name, string? inward, string? outward)
        {
            var id = (10000 + index).ToString(CultureInfo.InvariantCulture);

            return "{\"id\":\"" + id + "\""
                + Written("name", name)
                + Written("inward", inward)
                + Written("outward", outward)
                + ",\"self\":\"https://jira.example.invalid/rest/api/2/issueLinkType/" + id + "\"}";
        }

        public static string ALinkTypeDefinition(int index, JiraLinkType linkType)
            => ALinkTypeDefinition(index, linkType.Name, linkType.Inward, linkType.Outward);

        private static string Written(string property, string? value)
            => value is null ? string.Empty : ",\"" + property + "\":\"" + value + "\"";

        public static string AnEpic(string key, params string[] links) => AnEpicNamed(key, $"{key} summary", links);

        public static string AnEpicNamed(string key, string summary, params string[] links)
            => AnIssue(key, TheFieldsOf(summary, EpicIssueType, string.Empty, links), changelogEntries: null);

        /// <summary>
        /// An issue at whatever grain the caller needs. Only a Portfolio is made of Epics; a Team's items
        /// carry whichever type the instance calls them, and a fixture fixed at Epic cannot say so.
        /// </summary>
        public static string AnIssueOfType(string key, string issueType, params string[] links)
            => AnIssue(key, TheFieldsOf($"{key} summary", issueType, string.Empty, links), changelogEntries: null);

        /// <summary>
        /// An issue carrying whatever else the instance puts on it - a custom field value, a parent Jira
        /// names for itself - written as the leading-comma JSON fragment it appears as inside "fields".
        /// </summary>
        public static string AnIssueCarrying(string key, string issueType, string furtherFields, params string[] links)
            => AnIssue(key, TheFieldsOf($"{key} summary", issueType, furtherFields, links), changelogEntries: null);

        /// <summary>
        /// An issue whose history is long enough that the connector will not trust the copy that came with
        /// the search result, and re-reads it on the issue's own changelog endpoint. The threshold is thirty.
        /// </summary>
        public static string AnIssueWithAChangelogOf(string key, int entries, params string[] links)
            => AnIssue(key, TheFieldsOf($"{key} summary", EpicIssueType, string.Empty, links), entries);

        private static string AnIssue(string key, string fields, int? changelogEntries)
        {
            var changelog = changelogEntries is null
                ? string.Empty
                : ", \"changelog\": {\"total\": " + changelogEntries + "}";

            return "{\"key\": \"" + key + "\", \"fields\": " + fields + changelog + "}";
        }

        private static string TheFieldsOf(string summary, string issueType, string furtherFields, string[] links)
            => "{\"summary\": \"" + summary + "\""
                + ", \"issuetype\": {\"name\": \"" + issueType + "\"}"
                + ", \"status\": {\"name\": \"In Progress\"}"
                + ", \"created\": \"2026-01-01T00:00:00.000+0000\""
                + ", \"updated\": \"2026-01-02T00:00:00.000+0000\""
                + ", \"labels\": []"
                + ", \"issuelinks\": [" + string.Join(",", links) + "]"
                + furtherFields + "}";

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
