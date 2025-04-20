using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class Issue
    {
        public Issue(string key, string title, DateTime? createdDate, DateTime? closedDate, DateTime? startedDate, string parentKey, string rank, string issueType, string status, string statusCategory, JsonElement fields)
        {   
            Key = key;
            Title = title;
            ClosedDate = closedDate;
            StartedDate = startedDate;
            CreatedDate = createdDate;
            ParentKey = parentKey;
            Rank = rank;
            IssueType = issueType;
            State = status;
            StatusCategory = statusCategory;
            Fields = fields;
        }

        public string Key { get; }

        public DateTime? CreatedDate { get; }

        public DateTime? ClosedDate { get; }

        public DateTime? StartedDate { get; }

        public string ParentKey { get; }

        public string Title { get; }

        public string Rank { get; }

        public string IssueType { get; }

        public JsonElement Fields { get; }

        public string State { get; }
        
        public string StatusCategory { get; }
    }
}
