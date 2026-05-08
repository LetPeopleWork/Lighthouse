namespace Lighthouse.Backend.Models.Auth
{
    public record AuthorizationConfiguration
    {
        public bool Enabled { get; init; }

        public IReadOnlyList<string> EmergencySystemAdminSubjects { get; init; } = [];
    }
}