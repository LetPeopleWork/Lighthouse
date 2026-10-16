// SCAFFOLD: true
using System.Text.Json.Serialization;
using Lighthouse.Backend.Models.Staleness;

namespace Lighthouse.Backend.API.DTO
{
    public sealed record StaleReasonDto(
        StaleReasonKind Kind,
        int Days,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StateName)
    {
        public static StaleReasonDto From(StaleReason reason) => new(reason.Kind, reason.Days, reason.StateName);
    }
}
