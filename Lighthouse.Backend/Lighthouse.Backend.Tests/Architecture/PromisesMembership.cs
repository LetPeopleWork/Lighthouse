namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The types that make up Promises, measured from the compiled code, by name without their namespace so the same
    /// lists hold before and after the move into Modules/Promises. Moves holds every type that goes into the module, and
    /// ModelsTypesThatMove the shared Models types the context may take with it. A name the code holds twice is written
    /// with its namespace.
    /// </summary>
    internal static class PromisesMembership
    {
        public static IReadOnlyList<string> Moves { get; } =
        [
            "DeliveryForecastBlockRenderer",
            "DeliveryForecastPublishingHandler",
            "DeliveryForecastPublishingService",
            "DeliveryMetricSnapshotRecordingHandler",
            "DeliveryMetricValues",
            "DeliveryMetricValuesProjector",
            "DeliveryRuleService",
            "DeliverySourceResolver",
            "DeliverySourceSyncService",
            "IDeliveryForecastBlockRenderer",
            "IDeliveryForecastPublishingService",
            "IDeliveryRuleService",
            "IDeliverySourceResolver",
            "IDeliverySourceSyncService",
            "PortfolioSourcePreview",
            "PromisesModule",
        ];

        public static IReadOnlyList<string> ModelsTypesThatMove { get; } =
        [
            "DeliveryForecastBlock",
            "DeliveryForecastBlockPercentile",
        ];
    }
}
