namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The domain event types the composition root names, as the type arguments of the handlers it registers in its one
    /// ordered block. Each lives in its publisher's Contracts, so the composition-root rules read them from here and
    /// treat them as event types, not as handlers.
    /// </summary>
    internal static class EventTypesTheCompositionRootNames
    {
        public const string Category = "event type of the ordered block";

        private static readonly Dictionary<string, string[]> ByContext = new(StringComparer.Ordinal)
        {
            ["Forecasting"] = ["ForecastRoundFinished", "PortfolioForecastsUpdated"],
            ["OrganisationSetup"] =
            [
                "BlackoutConfigurationChanged", "ConnectionFetchShapeChanged", "FeatureOrderingPolicyChanged",
                "FeatureRankChanged", "PortfolioDeleted", "TeamDeleted",
            ],
            ["WorkItems"] =
            [
                "FeatureBlocked", "FeatureUnblocked", "PortfolioFeaturesRefreshed", "TeamDataRefreshed",
                "WorkItemBlocked", "WorkItemUnblocked",
            ],
        };

        public static IReadOnlyList<string> Of(string context)
            => ByContext.TryGetValue(context, out var names)
                ? [.. names.Select(name => $"{ContextContractsMembership.TheModulesRoot}.{context}.Contracts.{name}")]
                : [];
    }
}
