using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The counts one Release sweep gathers while it runs. Projects are read in parallel, so every count that grows
    /// during the reads is added to atomically.
    /// </summary>
    internal sealed class JiraReleaseSweepTally
    {
        private int releasesKept;
        private int versionsRead;
        private int versionPages;
        private int projectsSkipped;

        public JiraDeployment Deployment { get; set; } = JiraDeployment.Unknown;

        public int ProjectCount { get; set; }

        public bool Complete { get; set; }

        public void CountVersionPage(int versionsOnThePage)
        {
            Interlocked.Increment(ref versionPages);
            Interlocked.Add(ref versionsRead, versionsOnThePage);
        }

        public void CountProjectRead(int releasesKeptFromIt, bool sawEverything)
        {
            Interlocked.Add(ref releasesKept, releasesKeptFromIt);

            if (!sawEverything)
            {
                Interlocked.Increment(ref projectsSkipped);
            }
        }

        public JiraReleaseSweepCost ToCost(DeliverySourceSweepPurpose purpose, TimeSpan elapsed)
            => new(
                purpose,
                (long)elapsed.TotalMilliseconds,
                Deployment,
                Volatile.Read(ref releasesKept),
                Volatile.Read(ref versionsRead),
                Volatile.Read(ref versionPages),
                ProjectCount,
                Volatile.Read(ref projectsSkipped),
                Complete);
    }
}
