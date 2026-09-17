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
    }
}
