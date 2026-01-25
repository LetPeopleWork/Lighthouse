using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class Issue
    {
        public string Key { get; set; }

        public DateTime? CreatedDate { get; set;}

        public DateTime? ClosedDate { get; set;}

        public DateTime? StartedDate { get; set;}

        public string ParentKey { get;set; }

        public string Title { get; set;}

        public string Rank { get; set;}

        public string IssueType { get; set;}

        public JsonElement Fields { get; set; }

        public string State { get; set;}

        public List<string> Labels { get; set; }
    }
}
