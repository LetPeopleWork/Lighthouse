namespace Lighthouse.Backend.Models.Authorization
{
    public record UserAuthorizationSummary
    {
        public bool IsRbacEnabled { get; init; }

        public bool IsSystemAdmin { get; init; }

        public bool CanCreateTeam { get; init; }

        public bool CanCreatePortfolio { get; init; }

        public IReadOnlyList<string> SystemAdminDisplayNames { get; init; } = [];

        /// <summary>
        /// Team IDs for which the current user holds admin (write) rights.
        /// Empty when RBAC is disabled or the user is a System Admin (System Admin access is
        /// already covered by <see cref="IsSystemAdmin"/>).
        /// </summary>
        public IReadOnlyList<int> AdminTeamIds { get; init; } = [];

        /// <summary>
        /// Portfolio IDs for which the current user holds admin (write) rights.
        /// Empty when RBAC is disabled or the user is a System Admin.
        /// </summary>
        public IReadOnlyList<int> AdminPortfolioIds { get; init; } = [];
    }
}
