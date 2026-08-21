using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public static class IssueExtensions
    {
        private const string BlockedByLinkName = "is blocked by";

        private const string IssueLinkType = "type";

        private const string IssueLinkInwardName = "inward";

        private const string IssueLinkInwardIssue = "inwardIssue";


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

        private static string InwardNameOf(JsonElement link)
        {
            if (!link.TryGetProperty(IssueLinkType, out var type) || type.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            if (!type.TryGetProperty(IssueLinkInwardName, out var inward) || inward.ValueKind != JsonValueKind.String)
            {
                return string.Empty;
            }

            return inward.GetString() ?? string.Empty;
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
            if (obj.TryGetProperty("value", out var valueProp)){
                return valueProp.ToString();
            }

            if (obj.TryGetProperty("name", out var nameProp)){
                return nameProp.ToString();
                
            }

            return obj.ToString();
        }
    }
}
