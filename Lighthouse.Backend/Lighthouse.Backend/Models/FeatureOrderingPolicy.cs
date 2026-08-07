namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Who decides the order Lighthouse forecasts in. An enum rather than a boolean, because "manual
    /// sorting on/off" names a switch in the UI, not the thing being decided (ADR-132).
    /// </summary>
    public enum FeatureOrderingPolicy
    {
        /// <summary>The work tracking system's own value (<see cref="WorkItemBase.Order"/>). The default.</summary>
        SourceOrder,

        /// <summary>This instance's own places (<see cref="Feature.ManualRank"/>).</summary>
        ManualOrder,
    }
}
