namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The three queues updates run in, one per kind of work, so that a slow portfolio refresh cannot
    /// hold up team refreshes behind it.
    /// </summary>
    public enum UpdateLane
    {
        Team,

        Portfolio,

        Forecast,
    }

    public static class UpdateLaneMapping
    {
        /// <summary>
        /// Which lane a kind of update runs in. This is the only place the grouping is decided, so that the
        /// three places that need it - admitting work, draining a lane, and grouping the queue for display -
        /// cannot drift apart.
        ///
        /// Deleting an entity runs in the same lane as refreshing one of the same kind: run side by side they
        /// would be two things writing the same entity at once. A delete is also awaited by a caller holding
        /// an HTTP response open, so it must not be stuck behind unrelated work either.
        ///
        /// The switch deliberately has no fallback arm. Adding a sixth kind of update should stop the build
        /// here and make someone choose its lane, rather than let it quietly inherit someone else's.
        /// </summary>
#pragma warning disable CS8524 // A fallback arm here would silently give a future update type someone else's lane, which is the mistake this switch exists to prevent. Suppressing only CS8524 - which covers an int cast to an undeclared value - keeps CS8509 armed, so adding a sixth update type still fails the build until it is given a lane. An undeclared value throws at runtime instead, which is the right answer for a value that was never a kind of update.
        public static UpdateLane LaneOf(UpdateType updateType) => updateType switch
        {
            UpdateType.Team or UpdateType.TeamDelete => UpdateLane.Team,
            UpdateType.Features or UpdateType.PortfolioDelete => UpdateLane.Portfolio,
            UpdateType.Forecasts => UpdateLane.Forecast,
        };
#pragma warning restore CS8524
    }
}
