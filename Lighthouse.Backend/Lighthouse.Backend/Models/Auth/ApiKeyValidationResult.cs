namespace Lighthouse.Backend.Models.Auth
{
    public class ApiKeyValidationResult
    {
        public bool IsValid { get; set; }

        public int? ApiKeyId { get; set; }

        public ApiKeyOwnerResolutionState OwnerResolutionState { get; set; } = ApiKeyOwnerResolutionState.Unlinked;

        public string? OwnerSubject { get; set; }

        public string? OwnerDisplayName { get; set; }

        public string? OwnerEmail { get; set; }
    }
}
