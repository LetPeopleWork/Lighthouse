using Lighthouse.Backend.Models.DeliverySources;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    // __SCAFFOLD__: the acceptance tests were written ahead of this; its body and the connector's call to it are still to come.
    /// <summary>
    /// What one sweep of a connection's Jira Releases cost: how long it took and how much it read. An operator
    /// may copy the line from a customer's instance, so it carries counts and closed values only, never a
    /// host, a connection, a project or a Release by name.
    /// </summary>
    internal sealed record JiraReleaseSweepCost(
        DeliverySourceSweepPurpose Purpose,
        long ElapsedMs,
        JiraDeployment Deployment,
        int ReleasesKept,
        int VersionsRead,
        int VersionPages,
        int ProjectCount,
        int ProjectsSkipped,
        bool Complete)
    {
        internal const string Template =
            "Jira Release sweep for the {SweepPurpose} took {ElapsedMs} ms on this {Deployment} connection: " +
            "kept {ReleasesKept} of {VersionsRead} versions read over {VersionPages} pages from {ProjectCount} projects; " +
            "complete: {Complete}, projects skipped: {ProjectsSkipped}";

        /// <summary>
        /// Every name the line may carry. Each one holds a count or a closed value; a name added here is a value
        /// somebody has to check for customer data first.
        /// </summary>
        internal static readonly IReadOnlyList<string> PropertyNames =
        [
            "SweepPurpose", "ElapsedMs", "Deployment", "ReleasesKept", "VersionsRead", "VersionPages", "ProjectCount", "ProjectsSkipped", "Complete",
        ];

        public void WriteTo(ILogger logger)
        {
            throw new NotImplementedException($"Writing the cost of a {Purpose} sweep to {logger} is not written yet");
        }
    }
}
