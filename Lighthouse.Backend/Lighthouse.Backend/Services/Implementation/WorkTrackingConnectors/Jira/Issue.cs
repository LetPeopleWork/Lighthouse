using Lighthouse.Backend.Models;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class Issue
    {
        public string Key { get; set; }

        public DateTime? CreatedDate { get; set;}

        /// <summary>When Jira says the issue last changed — the stamp an incremental sweep compares to decide whether the issue needs downloading. Null when the tracker did not report it.</summary>
        public DateTime? Updated { get; set; }

        public DateTime? ClosedDate { get; set;}

        public DateTime? StartedDate { get; set;}

        public string ParentKey { get;set; }

        public string Title { get; set;}

        public string Rank { get; set;}

        public string IssueType { get; set;}

        public JsonElement Fields { get; set; }

        public string State { get; set;}

        public List<string> Labels { get; set; }

        public IReadOnlyList<WorkItemStateTransition> StateTransitions { get; set; } = [];

        /// <summary>
        /// When the link to each counterpart key that still stands was made, read from the issue's history.
        /// A key with no entry is a link the history never recorded.
        /// </summary>
        // SCAFFOLD: nothing fills this in yet, so every link reads as undated.
        public IReadOnlyDictionary<string, DateTime> LinkAddedAt { get; init; } = new Dictionary<string, DateTime>();
    }
}
