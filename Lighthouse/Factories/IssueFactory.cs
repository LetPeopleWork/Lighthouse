using Lighthouse.Services.Implementation.WorkItemServices;
using Lighthouse.WorkTracking.Jira;
using System.Text.Json;

namespace Lighthouse.Factories
{
    public class IssueFactory : IIssueFactory
    {
        private readonly string rankField = "customfield_10019";
        private readonly ILexoRankService lexoRankService;

        public IssueFactory(IConfiguration configuration, ILexoRankService lexoRankService)
        {
            var rankFiledOverride = configuration.GetValue<string>($"JiraConfiguration:CustomRankField");

            if (!string.IsNullOrEmpty(rankFiledOverride))
            {
                rankField = rankFiledOverride;
            }

            this.lexoRankService = lexoRankService;
        }

        public Issue CreateIssueFromJson(JsonElement json)
        {
            var fields = json.GetProperty(JiraFieldNames.FieldsFieldName);
            var key = GetKeyFromJson(json);
            var title = GetTitleFromFields(fields);
            var resolutionDate = GetResolutionDateFromFields(fields);
            var parentKey = GetParentFromFields(fields);
            var rank = GetRankFromFields(fields);
            var issueType = GetIssueTypeFromFields(fields);

            return new Issue(key, title, resolutionDate, parentKey, rank, issueType, fields);
        }

        private string GetIssueTypeFromFields(JsonElement fields)
        {
            return fields.GetProperty("issuetype").GetProperty("name").ToString();
        }

        private string GetRankFromFields(JsonElement fields)
        {
            var rank = lexoRankService.Default;
            // customfield_10019 is how Jira stores the rank. It's a string, not an int. It's using the LexoGraph algorithm for this.
            if (fields.TryGetProperty(rankField, out var parsedRank))
            {
                rank = parsedRank.ToString();
            }

            return rank;
        }

        private string GetParentFromFields(JsonElement fields)
        {
            var parentKey = string.Empty;
            if (fields.TryGetProperty("parent", out var parent))
            {
                parentKey = parent.GetProperty("key").ToString();
            }

            return parentKey;
        }

        private DateTime GetResolutionDateFromFields(JsonElement fields)
        {
            var resolutionDate = DateTime.MinValue;
            var resolutionDateString = fields.GetProperty(JiraFieldNames.ResolutionDateFieldName).GetString();
            if (!string.IsNullOrEmpty(resolutionDateString))
            {
                resolutionDate = DateTime.Parse(resolutionDateString);
            }

            return resolutionDate;
        }

        private string GetTitleFromFields(JsonElement fields)
        {
            return fields.GetFieldValue("summary");
        }

        private string GetKeyFromJson(JsonElement json)
        {
            return json.GetProperty("key").ToString();
        }
    }
}
