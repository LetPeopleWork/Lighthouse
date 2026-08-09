namespace Lighthouse.Backend.Models.WriteBack
{
    public class WriteBackItemResult
    {
        public required string WorkItemId { get; init; }

        public required string TargetFieldReference { get; init; }

        public bool Success { get; init; }

        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Whether this field reached the tracker quietly (ADR-142 §3). Defaults to
        /// <see cref="WriteBack.NotificationSuppression.NotApplicable"/>, so a connector that never asks
        /// for silence says nothing about it.
        /// </summary>
        public NotificationSuppression NotificationSuppression { get; init; } = NotificationSuppression.NotApplicable;

        /// <summary>
        /// How a result is derived from the update it answers is one piece of knowledge, and every
        /// write-back-capable connector needs it. The batch-and-degrade orchestration around it stays
        /// per connector on purpose (ADR-143); only this mapping is shared.
        /// </summary>
        public static WriteBackItemResult Written(WriteBackFieldUpdate update,
            NotificationSuppression suppression = NotificationSuppression.NotApplicable) => new()
        {
            WorkItemId = update.WorkItemId,
            TargetFieldReference = update.TargetFieldReference,
            Success = true,
            NotificationSuppression = suppression,
        };

        public static WriteBackItemResult Refused(WriteBackFieldUpdate update, string errorMessage,
            NotificationSuppression suppression = NotificationSuppression.NotApplicable) => new()
        {
            WorkItemId = update.WorkItemId,
            TargetFieldReference = update.TargetFieldReference,
            Success = false,
            ErrorMessage = errorMessage,
            NotificationSuppression = suppression,
        };
    }
}
