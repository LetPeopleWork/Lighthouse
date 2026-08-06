namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// The projection shape the position map orders (ADR-135) — the type is the evidence that no
    /// <c>Include</c> graph is loaded to number the whole table.
    /// </summary>
    public sealed record FeatureOrderKey(int Id, string Order);
}
