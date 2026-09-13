namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    /// <summary>
    /// What this instance is, worked out here and attached to what leaves. None of it is a field in
    /// any request or answer a browser can see, so a page cannot read these back and a caller cannot
    /// choose them - which is the difference between a fact about the instance and a claim by
    /// whoever happened to post.
    /// </summary>
    public sealed record UsageDataInstanceFacts(
        string Version,
        UsageDataDeploymentMode DeploymentMode,
        string LicenceTier,
        bool AuthenticationEnabled,
        bool IsPublishedRelease);

    public interface IUsageDataInstanceProperties
    {
        UsageDataInstanceFacts Describe();
    }
}
