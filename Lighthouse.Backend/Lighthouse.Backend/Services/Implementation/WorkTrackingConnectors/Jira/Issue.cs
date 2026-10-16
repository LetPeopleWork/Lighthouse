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
        /// Whether the issue came with its changelog, the changelog held every entry Jira counts for it, and
        /// every entry could be read.
        /// </summary>
        public bool HasItsWholeHistory { get; init; }

        /// <summary>
        /// Every link the issue's history says still stands, each with when it was made. A link with no entry
        /// here is one the history never recorded.
        /// </summary>
        public IReadOnlyList<StandingLink> StandingLinks { get; init; } = [];
    }
}
