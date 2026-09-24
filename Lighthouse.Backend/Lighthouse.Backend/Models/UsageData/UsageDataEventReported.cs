namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// One thing a browser reported, as this application holds it once the message has been read.
    /// Every part its event is supposed to have was actually sent: a message that left one out, or
    /// carried one its event has no business carrying, is refused where it is read.
    ///
    /// Most parts are absent for most of these, and that is the shape rather than a gap. Two events
    /// say which page somebody opened, one says which kind of work tracking system was connected, and
    /// one says which setting was switched and whether it is now on or off; the rest say only that
    /// somebody did something. Which event may carry which part is declared in
    /// <see cref="UsageDataEventShapes"/>.
    ///
    /// Choices from closed lists and bounded numbers, and nothing else - there is no field here in
    /// which a page address, a name somebody picked or a sentence somebody typed could travel, which
    /// is what makes that promise a property of the shape rather than a habit.
    /// </summary>
    public sealed record UsageDataEventReported(
        UsageDataEventName Name,
        UsageDataRouteKey? Route,
        UsageDataWorkTrackingSystem? WorkTrackingSystem,
        UsageDataOptionalFeature? OptionalFeature,
        bool? Enabled,
        int OffsetMs,
        int Sequence);
}
