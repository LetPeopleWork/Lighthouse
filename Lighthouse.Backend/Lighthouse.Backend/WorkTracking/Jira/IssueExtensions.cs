using System.Text.Json;

namespace Lighthouse.Backend.WorkTracking.Jira
{
    public static class IssueExtensions
    {
        public static string GetFieldValue(this JsonElement fields, string fieldKey)
        {
            if (fields.TryGetProperty(fieldKey, out var field))
            {
                return field.ToString();
            }

            return string.Empty;
        }
    }
}
