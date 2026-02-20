using System.Text.Json;

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
