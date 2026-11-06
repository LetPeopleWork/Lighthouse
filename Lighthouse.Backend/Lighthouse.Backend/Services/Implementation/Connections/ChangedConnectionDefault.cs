using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.Connections
{
    /// <summary>How the owners a connection default is held for are named in its log entry.</summary>
    internal sealed record OwnerWords(string One, string Many);

    /// <summary>A default a save pointed at a field it added, named in the error if it could not follow.</summary>
    internal sealed record PendingDefault(ConnectionFieldDefault Default, AdditionalFieldDefinition Field);

    /// <summary>How many Teams and how many Portfolios a save started updating, and how many of each it gave other parent sources.</summary>
    internal sealed record UpdatesQueued(int Teams, int Portfolios, ParentSourcesMoved ParentSourcesMoved)
    {
        public int For(DefaultHeldFor heldFor) => heldFor == DefaultHeldFor.Teams ? Teams : Portfolios;
    }

    /// <summary>How many Teams and how many Portfolios read their parent from something else after a save; null when that could not be read back.</summary>
    internal sealed record ParentSourcesMoved(int? Teams, int? Portfolios)
    {
        public static readonly ParentSourcesMoved NotKnown = new(null, null);

        public int? For(DefaultHeldFor heldFor) => heldFor == DefaultHeldFor.Teams ? Teams : Portfolios;
    }

    /// <summary>The owners whose fetch a save moved, by id, and how many it gave other parent sources.</summary>
    internal sealed record OwnersMoved(IReadOnlyList<int> TeamFetches, IReadOnlyList<int> PortfolioFetches, ParentSourcesMoved ParentSources);

    /// <summary>A connection default as it was before a save, with what it names after it and who follows it.</summary>
    internal sealed record ChangedConnectionDefault(
        string Name,
        NamedField Before,
        NamedField After,
        OwnerWords Owners,
        Func<WorkTrackingSystemConnection, List<string>> FollowersOf);
}
