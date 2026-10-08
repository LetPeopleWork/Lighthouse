using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// What a browser or one of our own clients posts. A batch that names no source is a browser's,
    /// because the web page has never sent one and a tab still holding an old page never will.
    /// </summary>
    public sealed record UsageDataEventBatchDto(UsageDataEventDto[]? Events, UsageDataSource? Source = null);

    /// <summary>
    /// One thing that happened. Nothing here is text: choices from closed lists, one on-or-off answer
    /// and two bounded numbers, so there is no field in which a page address, a name someone picked
    /// or a sentence someone typed could travel - not because the reader is careful, but because no
    /// such field exists to be careless with.
    ///
    /// Every part is nullable because a whole number left out of a message arrives as zero rather
    /// than as absent, and zero is a real choice in every list; read straight, a missing part would
    /// name something nobody sent. A left-out on-or-off answer would likewise arrive as "off".
    /// Whoever reads this has to establish a value was sent and refuse the message otherwise.
    /// Marking the parts required at the binding layer is the fix that is not available: it turns
    /// every message written before a part existed into a rejection.
    /// </summary>
    public sealed record UsageDataEventDto(
        UsageDataEventName? Name,
        UsageDataRouteKey? Route,
        UsageDataWorkTrackingSystem? WorkTrackingSystem,
        UsageDataOptionalFeature? OptionalFeature,
        bool? Enabled,
        UsageDataSizingMoment? SizingMoment,
        int? OffsetMs,
        int? Sequence,
        UsageDataRefinementVerdict? RefinementVerdict = null);
}
