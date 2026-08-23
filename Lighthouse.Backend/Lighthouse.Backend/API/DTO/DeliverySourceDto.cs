using System.Text.Json.Serialization;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// One selection mode a Portfolio's connection offers. The key is what a url segment and a create
    /// payload name; the display name is what the tab shows.
    /// </summary>
    public sealed record DeliverySourceDto(string Key, string DisplayName);

    /// <summary>
    /// One thing a Delivery could bind its date to. The date is written only when there is one - a
    /// remote object nobody dated must arrive with the field missing, so that no reader can turn an
    /// absent date into a plausible-looking real one.
    /// </summary>
    public sealed record DeliverySourceOptionDto(
        string Id,
        string Name,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        DateTime? Date,
        string ProjectKey,
        string ProjectName,
        bool IsSelectable,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        SourceOptionBlockReason? BlockedBecause);
}
