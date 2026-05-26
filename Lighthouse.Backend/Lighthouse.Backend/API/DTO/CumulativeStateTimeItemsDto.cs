namespace Lighthouse.Backend.API.DTO
{
    public sealed record CumulativeStateTimeItemsDto(string State, IReadOnlyList<CumulativeStateTimeItemDto> Items);
}
