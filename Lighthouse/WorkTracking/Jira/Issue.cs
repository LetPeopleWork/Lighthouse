using System.Text.Json;

namespace Lighthouse.WorkTracking.Jira
{
    public class Issue
    {
        private readonly JsonElement fields;

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

            ParentKey = string.Empty;
            if (fields.TryGetProperty("parent", out var parent))
            {
                ParentKey = parent.GetProperty("key").ToString();
            }

            Rank = "00000|";
            // customfield_10019 is how Jira stores the rank. It's a string, not an int. It's using the LexoGraph algorithm for this.
            if (fields.TryGetProperty("customfield_10019", out var rank))
            {
                Rank = rank.ToString();
            }

            IssueType = fields.GetProperty("issuetype").GetProperty("name").ToString();
        }

        public string Key { get; }

        public DateTime ResolutionDate { get; } = DateTime.MinValue;

        public string ParentKey { get; }

        public string Title { get; }

        public string Rank { get; private set; }

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
