namespace Lighthouse.Backend.Models.Metrics
{
    public sealed record CumulativeStateTimeItemsDto(string State, IReadOnlyList<CumulativeStateTimeItemDto> Items);
}
