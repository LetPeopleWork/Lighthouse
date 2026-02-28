namespace Lighthouse.Backend.Models.WriteBack
{
    public class WriteBackFieldUpdate
    {
        public required string WorkItemId { get; init; }

        public required string TargetFieldReference { get; init; }

        public required string Value { get; init; }
    }
}
