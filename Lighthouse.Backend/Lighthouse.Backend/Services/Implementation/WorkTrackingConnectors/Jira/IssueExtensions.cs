using Lighthouse.Backend.Models;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public static class IssueExtensions
    {
        /// <summary>
        /// Jira's default name for the waiting end of a Blocks link. An administrator can rename it,
        /// which is why the name is public: a Portfolio where nothing matched reports what it looked for
        /// beside what it found.
        /// </summary>
        public const string BlockedByLinkName = "is blocked by";

        private const string IssueLinkType = "type";

        private const string IssueLinkInwardName = "inward";

        private const string IssueLinkInwardIssue = "inwardIssue";

        private const string IssueLinkOutwardName = "outward";

        private const string IssueLinkOutwardIssue = "outwardIssue";


        public static string GetFieldValue(this JsonElement fields, string fieldKey)
        {
            if (!fields.TryGetProperty(fieldKey, out var field))
            {
                return string.Empty;
            }

            switch (field.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    var values = field.EnumerateArray()
                        .Select(item => item.ValueKind switch
                        {
                            JsonValueKind.String => item.GetString() ?? string.Empty,
                            JsonValueKind.Object => GetObjectDisplayValue(item),
                            _ => item.ToString()
                        });

                    return string.Join(",", values);
                }
                case JsonValueKind.Object:
                    return GetObjectDisplayValue(field);
                default:
                    return field.ToString();
            }
        }

        /// <summary>
        /// The other issues this one is waiting on, as the keys Lighthouse stores references by.
        ///
        /// Jira writes a link once and offers it from both ends: the waiting issue is handed an
        /// inwardIssue, the issue being waited on is handed an outwardIssue. Only the waiting end is
        /// read, because taking both would record every dependency in the instance a second time.
        ///
        /// The inward name is what tells waiting apart from the several other link types that arrive in
        /// exactly this shape - "relates to", "duplicates" and the rest - so it cannot simply be
        /// skipped. An administrator can rename it, which is why the caller reports the names it did see
        /// when nothing matched, rather than presenting an instance as having no dependencies.
        /// </summary>
        public static List<string> ExtractDependencyReferences(this JsonElement fields)
        {
            var references = new List<string>();

            foreach (var link in IssueLinksOf(fields))
            {
                if (!InwardNameOf(link).Equals(BlockedByLinkName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var key = KeyOf(link, IssueLinkInwardIssue);
                if (!string.IsNullOrEmpty(key))
                {
                    references.Add(key);
                }
            }

            return references;
        }

        /// <summary>
        /// The issue this one hangs under, read from its links of one configured type.
        ///
        /// Jira writes a link once and offers it from both ends, handing each issue a pointer to the
        /// other one - so an entry naming an outwardIssue sits on the issue holding the inward end, and
        /// the other way round. Read both ways, the parent is whichever issue the entry names, so an
        /// instance where a Work Item names its Feature and one where a Feature names its Work Items read
        /// alike without anybody configuring a direction.
        ///
        /// One link type drawn at every level breaks that: an item in the middle holds the link up to its
        /// parent and the links down to its children. Typing the phrase one end reads as narrows the
        /// reading to the entries handed to that end - Inward keeps those naming an inwardIssue, Outward
        /// those naming an outwardIssue.
        ///
        /// Two links to the same issue are one parent; two links to different issues are not a parent to
        /// choose between, so both are reported and neither is taken.
        /// </summary>
        public static ParentResolution ResolveParentFromLinks(this JsonElement fields, string linkTypeReference, LinkDirection direction)
        {
            var counterparts = IssueLinksOf(fields)
                .Where(link => LinkTypeAnswersTo(link, linkTypeReference))
                .Select(link => CounterpartKeyOf(link, direction))
                .Where(key => key.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return ParentResolution.From(counterparts);
        }

        /// <summary>
        /// What this issue's inward links are called on this instance. Only the inward ones: an outward
        /// link is the far end of somebody else's dependency, so naming those would send an
        /// administrator looking at the wrong half of their configuration.
        /// </summary>
        public static List<string> InwardLinkNames(this JsonElement fields)
        {
            var names = new List<string>();

            foreach (var link in IssueLinksOf(fields))
            {
                if (!link.TryGetProperty(IssueLinkInwardIssue, out _))
                {
                    continue;
                }

                var inwardName = InwardNameOf(link);
                if (!string.IsNullOrEmpty(inwardName))
                {
                    names.Add(inwardName);
                }
            }

            return names;
        }

        private static IEnumerable<JsonElement> IssueLinksOf(JsonElement fields)
        {
            if (!fields.TryGetProperty(JiraFieldNames.IssueLinksFieldName, out var links) || links.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return links.EnumerateArray().Where(link => link.ValueKind == JsonValueKind.Object);
        }

        private static string InwardNameOf(JsonElement link) => LabelOf(link, IssueLinkInwardName);

        /// <summary>
        /// A link type shows an administrator three phrases - its name, and what a link reads as from
        /// each end - and any of the three is what someone might have typed where a field name goes. A
        /// type whose two ends read alike is still one type, so a link is judged once rather than once
        /// per phrase.
        /// </summary>
        private static bool LinkTypeAnswersTo(JsonElement link, string reference)
            => Reads(LabelOf(link, JiraFieldNames.NamePropertyName), reference)
                || Reads(LabelOf(link, IssueLinkInwardName), reference)
                || Reads(LabelOf(link, IssueLinkOutwardName), reference);

        private static bool Reads(string label, string reference)
            => label.Length > 0 && label.Equals(reference, StringComparison.OrdinalIgnoreCase);

        private static string CounterpartKeyOf(JsonElement link, LinkDirection direction)
        {
            var inwardEnd = KeyOf(link, IssueLinkInwardIssue);
            var outwardEnd = KeyOf(link, IssueLinkOutwardIssue);

            return direction switch
            {
                LinkDirection.Inward => inwardEnd,
                LinkDirection.Outward => outwardEnd,
                _ => outwardEnd.Length > 0 ? outwardEnd : inwardEnd,
            };
        }

        private static string LabelOf(JsonElement link, string labelProperty)
        {
            if (!link.TryGetProperty(IssueLinkType, out var type) || type.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            if (!type.TryGetProperty(labelProperty, out var label) || label.ValueKind != JsonValueKind.String)
            {
                return string.Empty;
            }

            return label.GetString() ?? string.Empty;
        }

        private static string KeyOf(JsonElement link, string end)
        {
            if (!link.TryGetProperty(end, out var issue) || issue.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            if (!issue.TryGetProperty(JiraFieldNames.KeyPropertyName, out var key) || key.ValueKind != JsonValueKind.String)
            {
                return string.Empty;
            }

            return key.GetString() ?? string.Empty;
        }

        private static string GetObjectDisplayValue(JsonElement obj)
        {
            if (obj.TryGetProperty("value", out var valueProp))
            {
                return valueProp.ToString();
            }

            if (obj.TryGetProperty("name", out var nameProp))
            {
                return nameProp.ToString();
            }

            return obj.ToString();
        }
    }
}
