using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// What a browser posts.
    /// </summary>
    public sealed record UsageDataEventBatchDto(UsageDataEventDto[]? Events);

    /// <summary>
    /// One thing that happened. Nothing here is text: two choices from closed lists and two bounded
    /// numbers, so there is no field in which a page address, a name someone picked or a sentence
    /// someone typed could travel - not because the reader is careful, but because no such field
    /// exists to be careless with.
    ///
    /// Every part is nullable because a whole number left out of a message arrives as zero rather
    /// than as absent, and zero is a real choice in both lists; read straight, a missing part would
    /// name something nobody sent. Whoever reads this has to establish a value was sent and refuse
    /// the message otherwise. Marking the parts required at the binding layer is the fix that is not
    /// available: it turns every message written before a part existed into a rejection.
    /// </summary>
    public sealed record UsageDataEventDto(
        UsageDataEventName? Name,
        UsageDataRouteKey? Route,
        UsageDataWorkTrackingSystem? WorkTrackingSystem,
        int? OffsetMs,
        int? Sequence);
}
