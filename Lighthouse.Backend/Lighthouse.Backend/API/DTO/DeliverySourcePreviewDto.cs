namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// Why a preview came back with nothing in it. An empty list on its own leaves the reader guessing
    /// between two problems that are fixed in completely different places - one on the board, by tagging
    /// the work, and one here, by widening what this Portfolio covers.
    /// </summary>
    public enum DeliverySourcePreviewEmptyReason
    {
        None = 0,
        NothingTaggedAgainstTheSource = 1,
        TaggedWorkNotTrackedByThisPortfolio = 2,
    }

    /// <summary>
    /// What binding a Delivery to this source would mean right now: the name and date it would take, and
    /// the Features that would come along with it. The rows are the same shape the Feature grid already
    /// renders, so the preview needs no grid of its own.
    /// </summary>
    public sealed record DeliverySourcePreviewDto(
        string Name,
        DateTime Date,
        List<FeatureDto> Features,
        DeliverySourcePreviewEmptyReason EmptyBecause);

    /// <summary>Which remote object the caller wants to see the consequences of binding to.</summary>
    public class PreviewDeliverySourceRequest
    {
        public string SourceReference { get; set; } = string.Empty;
    }
}
