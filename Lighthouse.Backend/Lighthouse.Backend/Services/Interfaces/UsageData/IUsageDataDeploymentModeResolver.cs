namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    /// <summary>
    /// How an instance is deployed, as the thing that leaves this product says it. The list is closed
    /// and published on the usage data page, so adding a member changes what everyone who already
    /// agreed was told - which makes it a question for whoever answers those, not a code change.
    /// </summary>
    public enum UsageDataDeploymentMode
    {
        Standalone,
        Windows,
        Linux,
        MacOS,
        Docker,
        Kubernetes,
    }

    /// <summary>
    /// Answers which of those an instance is. It exists rather than a flag on the platform service
    /// because a Kubernetes pod looks exactly like a Docker container from inside - the container
    /// markers are all set in one - so telling them apart takes a question the platform service is
    /// never asked and must not start answering differently, since the update path reads it.
    /// </summary>
    public interface IUsageDataDeploymentModeResolver
    {
        UsageDataDeploymentMode Resolve();
    }
}
