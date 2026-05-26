namespace Lighthouse.Backend.API.DTO
{
    public sealed record CumulativeStateTimeCandidateRowDto(
        int WorkItemId,
        string ReferenceId,
        string Title,
        string WorkItemType,
        string? ParentReferenceId);

    public sealed record CumulativeStateTimeCandidatesDto(IReadOnlyList<CumulativeStateTimeCandidateRowDto> Items);
}
