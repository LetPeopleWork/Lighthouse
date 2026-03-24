namespace Lighthouse.Backend.Services.Interfaces.DatabaseManagement
{
    public record DatabaseOperationStatus(
        string OperationId,
        DatabaseOperationType OperationType,
        DatabaseOperationState State,
        string? FailureReason = null);
}
