namespace Lighthouse.Backend.Platform.DatabaseManagement
{
    public record DatabaseCapabilityStatus(
        string Provider,
        bool IsOperationBlocked,
        string? BlockedReason,
        bool IsToolingAvailable,
        string? ToolingGuidanceMessage,
        string? ToolingGuidanceUrl,
        DatabaseOperationStatus? ActiveOperation);
}
