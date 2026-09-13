namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// One thing a browser reported, as this application holds it once the message has been read.
    /// Every part its event is supposed to have was actually sent: a message that left one out, or
    /// carried one its event has no business carrying, is refused where it is read.
    ///
    /// The page is absent for most of these, and that is the shape rather than a gap. Two events say
    /// which page somebody opened; the rest say somebody did something, which happens on no
    /// particular page. Which events may name one is declared in <see cref="UsageDataEventShapes"/>.
    ///
    /// Choices from closed lists and bounded numbers, and nothing else - there is no field here in
    /// which a page address, a name somebody picked or a sentence somebody typed could travel, which
    /// is what makes that promise a property of the shape rather than a habit.
    /// </summary>
    public sealed record UsageDataEventReported(
        UsageDataEventName Name,
        UsageDataRouteKey? Route,
        int OffsetMs,
        int Sequence);
}
