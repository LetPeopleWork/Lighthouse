using System.Text.Json;

namespace Lighthouse.Backend.WorkTracking.Jira
{
    public class Issue
    {
        public Issue(string key, string title, DateTime resolutionDate, string parentKey, string rank, string issueType, JsonElement fields)
        {   
            Key = key;
            Title = title;
            ResolutionDate = resolutionDate;
            ParentKey = parentKey;
            Rank = rank;
            IssueType = issueType;
            Fields = fields;
        }

        public string Key { get; }

        public DateTime ResolutionDate { get; }

        public string ParentKey { get; }

        public string Title { get; }

        public string Rank { get; }

        public string IssueType { get; }
        public JsonElement Fields { get; }
    }
}
