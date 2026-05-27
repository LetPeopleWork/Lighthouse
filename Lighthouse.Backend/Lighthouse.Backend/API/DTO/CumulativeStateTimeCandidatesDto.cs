namespace Lighthouse.Backend.API.DTO
{
    public sealed record CumulativeStateTimeCandidateRowDto(
        int WorkItemId,
        string ReferenceId,
        string Title,
        string WorkItemType);

    public sealed record CumulativeStateTimeCandidatesDto(IReadOnlyList<CumulativeStateTimeCandidateRowDto> Items);
}
