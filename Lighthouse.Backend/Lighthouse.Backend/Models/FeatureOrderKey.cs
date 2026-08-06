namespace Lighthouse.Backend.Models
{
    /// <summary>The projection the position map orders, so numbering the whole table loads no <c>Include</c> graph (ADR-135).</summary>
    public sealed record FeatureOrderKey(int Id, string Order);
}
