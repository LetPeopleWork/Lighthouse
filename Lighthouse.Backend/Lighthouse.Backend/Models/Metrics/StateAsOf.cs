namespace Lighthouse.Backend.Models.Metrics
{
    /// <summary>
    /// The state an item held on a past day, resolved from its state-transition history (UPSTREAM-7).
    /// </summary>
    /// <remarks>
    /// Lives in Models rather than API.DTO because the application core returns it — ADR-027 D3/D5
    /// keeps Services free of any dependency on the API driving adapter.
    ///
    /// Only needed where the projection cannot be applied to the entity itself: teams project onto a
    /// WorkItem copy, whereas a Feature cannot be copied without dropping the forecast/work/portfolio
    /// data FeatureDto reads, and Features are EF-tracked so mutating them would persist the
    /// projection. The portfolio path therefore hands this to the DTO instead.
    /// </remarks>
    public record StateAsOf(string State, StateCategories StateCategory, DateTime EnteredAt);
}
