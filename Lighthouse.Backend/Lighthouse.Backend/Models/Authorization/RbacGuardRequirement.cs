namespace Lighthouse.Backend.Models.Authorization
{
    public enum RbacGuardRequirement
    {
        SystemAdmin,
        TeamRead,
        TeamWrite,
        PortfolioRead,
        PortfolioWrite,
        CanCreateTeam,
        CanCreatePortfolio,
        SystemAdminOrBootstrap,
    }
}
