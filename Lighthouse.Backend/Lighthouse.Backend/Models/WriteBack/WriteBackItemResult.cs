namespace Lighthouse.Backend.Models.WriteBack
{
    public class WriteBackItemResult
    {
        public required string WorkItemId { get; init; }

        public required string TargetFieldReference { get; init; }

        public bool Success { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
