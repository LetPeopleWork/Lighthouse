namespace Lighthouse.Backend.Models.Auth
{
    public record AuthConfigurationValidationResult
    {
        public bool IsValid { get; init; }

        public string? ErrorReason { get; init; }

        public static AuthConfigurationValidationResult Valid()
        {
            return new AuthConfigurationValidationResult { IsValid = true };
        }

        public static AuthConfigurationValidationResult Invalid(string reason)
        {
            return new AuthConfigurationValidationResult { IsValid = false, ErrorReason = reason };
        }
    }
}
