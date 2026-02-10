using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public static class IssueExtensions
    {
        public static string GetFieldValue(this JsonElement fields, string fieldKey)
        {
            if (!fields.TryGetProperty(fieldKey, out var field))
            {
                return string.Empty;
            }
            
            // It may be that we get back a JSON Object, e.g. for option fields. They need special treatment. If not, we just return the value.
            if (field.ValueKind != JsonValueKind.Object)
            {
                return field.ToString();
            }

            if (field.TryGetProperty("value", out var valueProperty))
            {
                return valueProperty.ToString();
            }
            
            // If no "value" property, return the full object as string
            return field.ToString();
        }
    }
}
