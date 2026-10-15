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

        /// <summary>
        /// An issue as a search returns it with its history attached: its own created and changed stamps,
        /// anything else the instance puts on it, its links, and the history entries the search carried.
        /// The connector does not trust a history the search reports as longer than thirty entries and
        /// re-reads it on the issue's own changelog endpoint, so <paramref name="historyEntriesInAll"/> may
        /// claim more entries than are handed over here.
        /// </summary>
        public static string AnIssueWithItsHistory(
            string key,
            string issueType,
            string created,
            string updated,
            string furtherFields,
            IEnumerable<string> links,
            IEnumerable<string> histories,
            int? historyEntriesInAll = null)
        {
            var entries = histories.ToList();
            var claimed = (historyEntriesInAll ?? entries.Count).ToString(CultureInfo.InvariantCulture);
            var carried = entries.Count.ToString(CultureInfo.InvariantCulture);

            var fields = "{\"summary\": \"" + key + " summary\""
                + ", \"issuetype\": {\"name\": \"" + issueType + "\"}"
                + ", \"status\": {\"name\": \"In Progress\"}"
                + ", \"created\": \"" + created + "\""
                + ", \"updated\": \"" + updated + "\""
                + ", \"labels\": []"
                + ", \"issuelinks\": [" + string.Join(",", links) + "]"
                + furtherFields + "}";

            var changelog = "{\"startAt\": 0, \"maxResults\": " + carried + ", \"total\": " + claimed
                + ", \"histories\": [" + string.Join(",", entries) + "]}";

            return "{\"key\": \"" + key + "\", \"fields\": " + fields + ", \"changelog\": " + changelog + "}";
        }

        /// <summary>How Jira writes an instant in an issue's own fields. The instant is taken to be UTC already.</summary>
        public static string AnInstant(DateTime at)
            => at.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'+0000'", CultureInfo.InvariantCulture);

        /// <summary>
        /// One history entry in which this issue gained a link to <paramref name="counterpartKey"/>, in the
        /// shape Jira Cloud was seen to write when a link was added, removed and added again: field "Link",
        /// fieldtype "jira", nothing on the from side, the counterpart's key in "to" and this issue's own
        /// sentence for the link in "toString". Both ends of a link get such an entry, each naming the other.
        /// </summary>
        public static string ALinkAddedAt(DateTime at, string counterpartKey, string sentence)
            => AHistoryEntry(at, ALinkItem(null, null, counterpartKey, sentence));

        /// <summary>The same addition under a history id of the caller's choosing, for entries stamped at one instant.</summary>
        public static string ALinkAddedAt(DateTime at, string counterpartKey, string sentence, long historyId)
            => AHistoryEntry(at, historyId, ALinkItem(null, null, counterpartKey, sentence));

        /// <summary>
        /// One history entry in which this issue lost its link to <paramref name="counterpartKey"/>, as Jira Cloud
        /// writes it: the mirror of an addition, the key and sentence on the from side and nothing on the to side.
        /// </summary>
        public static string ALinkRemovedAt(DateTime at, string counterpartKey, string sentence)
            => AHistoryEntry(at, ALinkItem(counterpartKey, sentence, null, null));

        /// <summary>The same removal under a history id of the caller's choosing, for entries stamped at one instant.</summary>
        public static string ALinkRemovedAt(DateTime at, string counterpartKey, string sentence, long historyId)
            => AHistoryEntry(at, historyId, ALinkItem(counterpartKey, sentence, null, null));

        /// <summary>
        /// An addition carrying a link id where the key would be, so only the sentence names the counterpart.
        /// Jira Cloud did not write this when it was watched; it has been reported for other Jira versions,
        /// and is kept so that a history in that shape still dates its links.
        /// </summary>
        public static string ALinkAddedNamingTheCounterpartOnlyInItsSentence(DateTime at, string linkId, string sentence)
            => AHistoryEntry(at, ALinkItem(null, null, linkId, sentence));

        /// <summary>One history entry that moved the issue between statuses, which most of a real history is.</summary>
        public static string AStatusChangeAt(DateTime at, string fromStatus, string toStatus)
            => AHistoryEntry(at, AStatusItem(fromStatus, toStatus));

        /// <summary>One history entry in which a single edit both gave the issue a link and moved it between statuses.</summary>
        public static string ALinkAddedBesideAStatusChangeAt(DateTime at, string counterpartKey, string sentence, string fromStatus, string toStatus)
            => AHistoryEntry(at, ALinkItem(null, null, counterpartKey, sentence) + ", " + AStatusItem(fromStatus, toStatus));

        private static string AStatusItem(string fromStatus, string toStatus)
            => "{\"field\": \"status\", \"fieldtype\": \"jira\", \"from\": \"1\", \"fromString\": \""
                + fromStatus + "\", \"to\": \"2\", \"toString\": \"" + toStatus + "\"}";

        /// <summary>One page of the changelog re-read, holding the whole history at once.</summary>
        public static string AChangelogPageHolding(IEnumerable<string> histories)
        {
            var entries = histories.ToList();

            return "{\"values\": [" + string.Join(",", entries) + "], \"total\": "
                + entries.Count.ToString(CultureInfo.InvariantCulture) + ", \"isLast\": true}";
        }

        /// <summary>
        /// Jira Cloud stamps a history entry in the zone of the account that reads it, not in UTC, so the
        /// entries here carry an offset too and a reader that drops it gets the wrong instant.
        /// </summary>
        private static string AHistoryEntry(DateTime at, string item) => AHistoryEntry(at, at.Ticks, item);

        private static string AHistoryEntry(DateTime at, long historyId, string item)
            => "{\"id\": \"" + historyId.ToString(CultureInfo.InvariantCulture) + "\", \"created\": \"" + AnInstantAsTheAccountSeesIt(at)
                + "\", \"items\": [" + item + "]}";

        private static readonly TimeSpan TheReadingAccountsOffset = TimeSpan.FromHours(2);

        private static string AnInstantAsTheAccountSeesIt(DateTime at)
        {
            var written = new DateTimeOffset(at, TimeSpan.Zero).ToOffset(TheReadingAccountsOffset)
                .ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

            return written.Remove(written.Length - 3, 1);
        }

        private static string ALinkItem(string? from, string? fromString, string? to, string? toString)
            => "{\"field\": \"Link\", \"fieldtype\": \"jira\""
                + ", \"from\": " + JsonOrNull(from)
                + ", \"fromString\": " + JsonOrNull(fromString)
                + ", \"to\": " + JsonOrNull(to)
                + ", \"toString\": " + JsonOrNull(toString) + "}";

        private static string JsonOrNull(string? value) => value is null ? "null" : "\"" + value + "\"";

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
