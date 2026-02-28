namespace Lighthouse.Backend.Models.WriteBack
{
    public class WriteBackResult
    {
        public IReadOnlyList<WriteBackItemResult> ItemResults { get; init; } = [];

        public int SuccessCount => ItemResults.Count(r => r.Success);

        public int FailureCount => ItemResults.Count(r => !r.Success);

        public bool AllSucceeded => ItemResults.All(r => r.Success);
    }
}
