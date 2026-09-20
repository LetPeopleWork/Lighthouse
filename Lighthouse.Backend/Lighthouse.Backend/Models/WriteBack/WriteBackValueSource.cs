namespace Lighthouse.Backend.Models.WriteBack
{
    public enum WriteBackValueSource
    {
        WorkItemAgeCycleTime,

        FeatureSize,

        ForecastPercentile50,

        ForecastPercentile70,

        ForecastPercentile85,

        ForecastPercentile95,

        // Appended, and it must stay last. The stored value is this member's ORDINAL - the mapping
        // persists ValueSource as an int - so inserting a member anywhere above would silently
        // re-point every existing write-back mapping at a different source, on every database.
        SleRisk,

        // The other end of the bar: when work on a Feature is expected to begin, so a roadmap can get
        // both of its dates from measured flow instead of from somebody maintaining them by hand.
        // Appended after SleRisk for the reason above, and these four must stay last in their turn.
        ForecastedStartPercentile50,

        ForecastedStartPercentile70,

        ForecastedStartPercentile85,

        ForecastedStartPercentile95,
    }
}
