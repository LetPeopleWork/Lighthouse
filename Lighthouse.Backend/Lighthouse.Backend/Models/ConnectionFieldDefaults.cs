namespace Lighthouse.Backend.Models
{
    /// <summary>Whether a connection default is held for the Teams or for the Portfolios on the connection.</summary>
    public enum DefaultHeldFor
    {
        Teams,
        Portfolios,
    }

    /// <summary>A connection default that names one of the connection's Additional Fields, and what that field is called.</summary>
    public sealed record ConnectionFieldDefault(
        ConnectionDefaultSetting Setting,
        DefaultHeldFor HeldFor,
        Func<OwnerTerms, string> NameIn,
        Func<WorkTrackingSystemConnection, int?> FieldOn,
        Action<WorkTrackingSystemConnection, int?> PointAt)
    {
        /// <summary>The name in the words Lighthouse seeds, for the log an operator reads.</summary>
        public string FieldName => NameIn(OwnerTerms.Seeded);
    }

    public static class ConnectionFieldDefaults
    {
        public static string FeatureOwnerFieldIn(OwnerTerms terms) => $"{terms.Feature} Owner Field";

        /// <summary>Every default a connection holds as one of its fields, in the order they are checked, logged and named.</summary>
        public static readonly IReadOnlyList<ConnectionFieldDefault> All =
        [
            new(ConnectionDefaultSetting.ParentOverrideForTeams, DefaultHeldFor.Teams, _ => "Parent Override Field",
                connection => connection.ParentOverrideForTeams, (connection, field) => connection.ParentOverrideForTeams = field),
            new(ConnectionDefaultSetting.ParentOverrideForPortfolios, DefaultHeldFor.Portfolios, _ => "Parent Override Field",
                connection => connection.ParentOverrideForPortfolios, (connection, field) => connection.ParentOverrideForPortfolios = field),
            new(ConnectionDefaultSetting.DependencyFieldForPortfolios, DefaultHeldFor.Portfolios, _ => "Dependency Field",
                connection => connection.DependencyFieldForPortfolios, (connection, field) => connection.DependencyFieldForPortfolios = field),
            new(ConnectionDefaultSetting.SizeEstimateFieldForPortfolios, DefaultHeldFor.Portfolios, _ => "Size Estimate Field",
                connection => connection.SizeEstimateFieldForPortfolios, (connection, field) => connection.SizeEstimateFieldForPortfolios = field),
            new(ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios, DefaultHeldFor.Portfolios, FeatureOwnerFieldIn,
                connection => connection.FeatureOwnerFieldForPortfolios, (connection, field) => connection.FeatureOwnerFieldForPortfolios = field),
        ];
    }
}
