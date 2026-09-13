namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// One thing a browser reported, as this application holds it once the message has been read.
    /// Nothing here can be absent: a part that was left out is refused where the message is read,
    /// so by the time one of these exists every value in it was actually sent.
    ///
    /// Two choices from closed lists and two bounded numbers, and nothing else - there is no field
    /// here in which a page address, a name somebody picked or a sentence somebody typed could
    /// travel, which is what makes that promise a property of the shape rather than a habit.
    /// </summary>
    public sealed record UsageDataEventReported(
        UsageDataEventName Name,
        UsageDataRouteKey Route,
        int OffsetMs,
        int Sequence);
}
