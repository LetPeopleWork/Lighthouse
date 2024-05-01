using Lighthouse.Services.Implementation.WorkItemServices;
using Lighthouse.WorkTracking.Jira;
using System.Text.Json;

namespace Lighthouse.Factories
{
    public class IssueFactory : IIssueFactory
    {
        private readonly ILexoRankService lexoRankService;
        private readonly ILogger<IssueFactory> logger;

        public IssueFactory(ILexoRankService lexoRankService, ILogger<IssueFactory> logger)
        {
            this.lexoRankService = lexoRankService;
            this.logger = logger;
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

            // First try to use the "default" custom field to get the rank
            // customfield_10019 is how Jira stores the rank. It's a string, not an int. It's using the LexoGraph algorithm for this.
            if (fields.TryGetProperty("customfield_10019", out var parsedRank))
            {
                rank = parsedRank.ToString();

                if (!string.IsNullOrEmpty(rank))
                {
                    return rank;
                }
            }

            logger.LogInformation("Could not find rank in default field, parsing all fields for LexoRank...");

            // It's possible that Jira is using a different custom field for the rank - try to find it via searching through the available properties.
            // Iterate through all fields
            foreach (var field in fields.EnumerateObject().Where(f => f.Value.ToString().Contains('|')))
            {
                rank = field.Value.ToString();

                logger.LogInformation("Found rank in field {Name}: {Rank}", field.Name, rank);
                break;
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
