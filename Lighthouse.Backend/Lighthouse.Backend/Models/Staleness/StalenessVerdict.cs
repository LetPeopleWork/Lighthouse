namespace Lighthouse.Backend.Models.Staleness
{
    public sealed record StaleReason(StaleReasonKind Kind, int Days, string? StateName);

    public sealed record StalenessVerdict(bool IsStale, IReadOnlyList<StaleReason> Reasons)
    {
        public static StalenessVerdict NotStale { get; } = new(false, []);
    }
}
