namespace Lighthouse.Backend.Models.Authorization
{
    public record RbacOperationResult
    {
        public bool Succeeded { get; init; }

        public string? ErrorCode { get; init; }

        public string? Message { get; init; }

        public static RbacOperationResult Success() => new() { Succeeded = true };

        public static RbacOperationResult Failure(string errorCode, string message) => new()
        {
            Succeeded = false,
            ErrorCode = errorCode,
            Message = message,
        };
    }

    public static class RbacOperationErrorCodes
    {
        public const string MissingStableSubject = "MissingStableSubject";

        public const string AlreadyBootstrapped = "AlreadyBootstrapped";

        public const string UserNotFound = "UserNotFound";

        public const string LastSystemAdmin = "LastSystemAdmin";

        public const string InvalidRoleForScope = "InvalidRoleForScope";
    }
}