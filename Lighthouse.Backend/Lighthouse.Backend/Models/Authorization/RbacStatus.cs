namespace Lighthouse.Backend.Models.Authorization
{
    public record RbacStatus
    {
        public bool Enabled { get; init; }

        public bool PremiumGateSatisfied { get; init; }

        public bool HasSystemAdmin { get; init; }

        public bool HasEmergencyAdminConfigured { get; init; }

        public bool ReadyForEnablement { get; init; }

        public int UnassignedUserCount { get; init; }
    }
}