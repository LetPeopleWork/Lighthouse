using System.Text.Json;

namespace Lighthouse.WorkTracking.Jira
{
    public class Issue
    {
        public Issue(JsonElement issueAsJson)
        {
            fields = issueAsJson.GetProperty(JiraFieldNames.FieldsFieldName);

            Key = issueAsJson.GetProperty("key").ToString();

            Title = GetFieldValue("summary");

            var resolutionDateString = fields.GetProperty(JiraFieldNames.ResolutionDateFieldName).GetString();
            if (!string.IsNullOrEmpty(resolutionDateString))
            {
                ResolutionDate = DateTime.Parse(resolutionDateString);
            }

            if (fields.TryGetProperty("parent", out var parent))
            {
                ParentKey = parent.GetProperty("key").ToString();
            }

            IssueType = fields.GetProperty("issuetype").GetProperty("name").ToString();
        }

        public string Key { get; }

        private JsonElement fields;

        public DateTime ResolutionDate { get; } = DateTime.MinValue;

        public string ParentKey { get; }

        public string Title { get; }

        public int Rank { get; set; }

        public string IssueType { get; }

        public string GetFieldValue(string fieldKey)
        {
            if (fields.TryGetProperty(fieldKey, out var field))
            {
                return field.ToString();
            }

            return string.Empty;
        }
    }
}
