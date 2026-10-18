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
        string FieldName,
        Func<WorkTrackingSystemConnection, int?> FieldOn,
        Action<WorkTrackingSystemConnection, int?> PointAt);

    public static class ConnectionFieldDefaults
    {
        /// <summary>Every default a connection holds as one of its fields, in the order they are checked, logged and named.</summary>
        public static readonly IReadOnlyList<ConnectionFieldDefault> All =
        [
            new(ConnectionDefaultSetting.ParentOverrideForTeams, DefaultHeldFor.Teams, "Parent Override Field",
                connection => connection.ParentOverrideForTeams, (connection, field) => connection.ParentOverrideForTeams = field),
            new(ConnectionDefaultSetting.ParentOverrideForPortfolios, DefaultHeldFor.Portfolios, "Parent Override Field",
                connection => connection.ParentOverrideForPortfolios, (connection, field) => connection.ParentOverrideForPortfolios = field),
            new(ConnectionDefaultSetting.DependencyFieldForPortfolios, DefaultHeldFor.Portfolios, "Dependency Field",
                connection => connection.DependencyFieldForPortfolios, (connection, field) => connection.DependencyFieldForPortfolios = field),
            new(ConnectionDefaultSetting.SizeEstimateFieldForPortfolios, DefaultHeldFor.Portfolios, "Size Estimate Field",
                connection => connection.SizeEstimateFieldForPortfolios, (connection, field) => connection.SizeEstimateFieldForPortfolios = field),
        ];
    }
}
