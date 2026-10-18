using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.Connections
{
    /// <summary>How the owners a connection default is held for are named in its log entry.</summary>
    internal sealed record OwnerWords(string One, string Many);

    /// <summary>A connection default as it was before a save, with what it names after it and who follows it.</summary>
    internal sealed record ChangedConnectionDefault(
        string Name,
        NamedField Before,
        int? FieldIdAfter,
        OwnerWords Owners,
        Func<WorkTrackingSystemConnection, List<string>> FollowersOf);
}
