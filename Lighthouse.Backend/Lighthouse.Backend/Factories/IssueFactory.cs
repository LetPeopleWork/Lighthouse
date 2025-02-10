using Lighthouse.Backend.Services.Implementation.WorkItemServices;
using Lighthouse.Backend.WorkTracking.Jira;
using System.Text.Json;

namespace Lighthouse.Backend.Factories
{
    public class IssueFactory : IIssueFactory
    {
        private readonly ILexoRankService lexoRankService;
        private readonly ILogger<IssueFactory> logger;

        // customfield_10019 is how Jira stores the rank.
        private static readonly List<string> rankCustomFieldNames = new List<string> { "customfield_10019" };

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
            var state = GetStateFromFields(fields);
            var statusCategory = GetStatusCategoryFromFields(fields);

            logger.LogDebug("Creating Issue with Key {Key}, Title {Title}, Resoultion Date {ResolutionDate}, Parent Key {ParentKey}, Rank {Rank}, Issue Type {issueType}, Status {Status}, Status Category {StatusCategory}", key, title, resolutionDate, parentKey, rank, issueType, state, statusCategory);

            return new Issue(key, title, resolutionDate, parentKey, rank, issueType, state, statusCategory, fields);
        }

        private static string GetIssueTypeFromFields(JsonElement fields)
        {
            return fields.GetProperty("issuetype").GetProperty("name").ToString();
        }

        private static string GetStateFromFields(JsonElement fields)
        {
            return fields.GetProperty("status").GetProperty("name").ToString();
        }

        private static string GetStatusCategoryFromFields(JsonElement fields)
        {
            return fields.GetProperty("status").GetProperty("statusCategory").GetProperty("name").ToString();
        }

        private string GetRankFromFields(JsonElement fields)
        {
            // Try getting the ranks using the default field or previously used fields. It's a string, not an int. It's using the LexoGraph algorithm for this.
            var rank = string.Empty;

            if (TryGetRankFromKnownFields(fields, out rank))
            {
                return rank;
            }

            logger.LogInformation("Could not find rank in default field, parsing all fields for LexoRank...");
            // It's possible that Jira is using a different custom field for the rank - try to find it via searching through the available properties.
            var rankFields = fields.EnumerateObject().Where(f => f.Value.ToString().Contains('|'));
            if (rankFields.Any())
            {
                var field = rankFields.First();
                rank = field.Value.ToString();
                logger.LogInformation("Found rank in field {Name}: {Rank}. Storing for next use", field.Name, rank);

                rankCustomFieldNames.Add(field.Name);

                return rank;
            }

            return lexoRankService.Default;
        }

        private static bool TryGetRankFromKnownFields(JsonElement fields, out string rank)
        {
            rank = string.Empty;

            foreach (var customField in rankCustomFieldNames)
            {
                if (fields.TryGetProperty(customField, out var parsedRank))
                {
                    rank = parsedRank.ToString();

                    if (!string.IsNullOrEmpty(rank))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        private static string GetParentFromFields(JsonElement fields)
        {
            var parentKey = string.Empty;
            if (fields.TryGetProperty("parent", out var parent))
            {
                parentKey = parent.GetProperty("key").ToString();
            }

            return parentKey;
        }

        private static DateTime GetResolutionDateFromFields(JsonElement fields)
        {
            var resolutionDate = DateTime.MinValue;
            var resolutionDateString = fields.GetProperty(JiraFieldNames.ResolutionDateFieldName).GetString();
            if (!string.IsNullOrEmpty(resolutionDateString))
            {
                resolutionDate = DateTime.Parse(resolutionDateString);
            }

            return resolutionDate;
        }

        private static string GetTitleFromFields(JsonElement fields)
        {
            return fields.GetFieldValue("summary");
        }

        private static string GetKeyFromJson(JsonElement json)
        {
            return json.GetProperty("key").ToString();
        }
    }
}
