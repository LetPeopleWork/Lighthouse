namespace Lighthouse.Backend.API.DTO
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

    public sealed record CumulativeStateTimeItemDto(
        string ReferenceId,
        string Title,
        string Type,
        string State,
        double DaysContributed);
}
