namespace Lighthouse.Backend.Models.Authorization
{
    public record UserAuthorizationSummary
    {
        public bool IsRbacEnabled { get; init; }

        public bool IsSystemAdmin { get; init; }

        public bool CanCreateTeam { get; init; }

        public bool CanCreatePortfolio { get; init; }
    }
}
