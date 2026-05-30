namespace Lighthouse.Backend.Models.Metrics
{
    public sealed record CumulativeStateTimeStateRowDto(
        string State,
        int WorkflowOrder,
        double TotalDays,
        double CompletedContributionDays,
        double OngoingContributionDays,
        int ItemCount,
        int CompletedItemCount,
        int OngoingItemCount,
        double MeanDays,
        double? MedianDays);

    public sealed record CumulativeStateTimeDto(IReadOnlyList<CumulativeStateTimeStateRowDto> States);

    public sealed record CumulativeStateTimeItemDto
    {
        public required int WorkItemId { get; init; }
        public required string ReferenceId { get; init; }
        public required string Title { get; init; }
        public required string Type { get; init; }
        public required string State { get; init; }
        public required string StateCategory { get; init; }
        public string? Url { get; init; }
        public required double DaysContributed { get; init; }
    }
}
