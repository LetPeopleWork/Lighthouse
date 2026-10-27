
namespace Lighthouse.Backend.Models
{
    public enum ConnectionDefaultSetting
    {
        ParentOverrideForTeams,
        EstimationForTeams,
        ParentOverrideForPortfolios,
        DependencyFieldForPortfolios,
        SizeEstimateFieldForPortfolios,
        FeatureOwnerFieldForPortfolios,
        EstimationForPortfolios,
    }

    /// <summary>Which settings a connection may hold for the Teams and Portfolios that follow it, by tracker.</summary>
    public static class SupportedConnectionDefaults
    {
        private static readonly ConnectionDefaultSetting[] Every = Enum.GetValues<ConnectionDefaultSetting>();

        // A CSV file has no parent, dependency or size field of its own to point at; only who owns a Feature
        // and how work is estimated can come from a column.
        private static readonly ConnectionDefaultSetting[] OnCsv =
        [
            ConnectionDefaultSetting.EstimationForTeams,
            ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios,
            ConnectionDefaultSetting.EstimationForPortfolios,
        ];

        private static readonly ConnectionDefaultSetting[] None = [];

        public static IReadOnlyList<ConnectionDefaultSetting> For(WorkTrackingSystems tracker) => tracker switch
        {
            WorkTrackingSystems.Jira or WorkTrackingSystems.AzureDevOps => Every,
            WorkTrackingSystems.Csv => OnCsv,
            _ => None,
        };

        public static bool Honours(WorkTrackingSystems tracker, ConnectionDefaultSetting setting) => For(tracker).Contains(setting);
    }
}
