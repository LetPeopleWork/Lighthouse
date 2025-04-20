using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public static class IssueExtensions
    {
        public static string GetFieldValue(this JsonElement fields, string fieldKey)
        {
            if (fields.TryGetProperty(fieldKey, out var field))
            {
                return field.ToString();
            }

            return TryExtractingCustomField(fields, fieldKey);
        }

        private static string TryExtractingCustomField(JsonElement fields, string fieldKey)
        {
            /* This is done because of Jira's custom fields. In the JQL, we have to refer to them via "cf[<id>]".
             * However, when we get it from the API, it's "customfield_<id>". This method tries to convert the key from cf[<id>] to customfield_<id>.
             * This is stupid and I hate it. Do better Atlassian. */
            var regex = new Regex(@"^cf\[(\d+)\]$", RegexOptions.None, TimeSpan.FromMilliseconds(300));
            var match = regex.Match(fieldKey);
            if (match.Success)
            {
                var customFieldKey = "customfield_" + match.Groups[1].Value;
                if (fields.TryGetProperty(customFieldKey, out var customField))
                {
                    return customField.ToString();
                }
            }

            return string.Empty;
        }
    }
}
