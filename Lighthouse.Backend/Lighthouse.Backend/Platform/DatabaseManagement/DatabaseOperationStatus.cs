namespace Lighthouse.Backend.Platform.DatabaseManagement
{
    public record DatabaseOperationStatus(
        string OperationId,
        DatabaseOperationType OperationType,
        DatabaseOperationState State,
        string? FailureReason = null,
        DatabaseOperationFailure? FailureCode = null);
}
