using System.Text.Json.Serialization;
using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// Reads a band only by its name. The converter every other choice is read with also takes a bare
    /// number, and here a number could be a count of Features rather than a band - so a number, even
    /// one a band happens to be stored as, is refused instead of read.
    /// </summary>
    public sealed class UsageDataFeaturesHeldByNameOnly()
        : JsonStringEnumConverter<UsageDataFeaturesHeld>(namingPolicy: null, allowIntegerValues: false);
}
