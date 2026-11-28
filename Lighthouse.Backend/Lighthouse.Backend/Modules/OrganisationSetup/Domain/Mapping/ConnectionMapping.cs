using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping
{
    /// <summary>
    /// The item types one connection lists per level, once per level whatever the case, in the mapping's own
    /// spelling. A type may sit on both levels of one connection; nothing blocks it.
    /// </summary>
    public sealed class ConnectionMapping
    {
        private readonly Dictionary<LevelId, IReadOnlyList<string>> typesByLevel;

        internal ConnectionMapping(int connectionId, IReadOnlyDictionary<LevelId, IReadOnlyList<string>> typesByLevel)
        {
            ConnectionId = connectionId;
            this.typesByLevel = typesByLevel.ToDictionary(level => level.Key, level => OnceEachWhateverTheCase(level.Value));
        }

        public int ConnectionId { get; }

        /// <summary>The levels this connection has a list for; a level without one is absent.</summary>
        public IReadOnlyCollection<LevelId> MappedLevels => typesByLevel.Keys;

        /// <summary>
        /// Today's create defaults: the types a new Team or Portfolio starts with, or nothing on a connector that has
        /// no item types.
        /// </summary>
        public static ConnectionMapping WithCreateDefaults(int connectionId, bool connectorUsesItemTypes)
            => new(connectionId, new Dictionary<LevelId, IReadOnlyList<string>>
            {
                [LevelId.Team] = connectorUsesItemTypes ? new Team().WorkItemTypes : [],
                [LevelId.Portfolio] = connectorUsesItemTypes ? new Portfolio().WorkItemTypes : [],
            });

        public static ConnectionMapping WithCreateDefaults(WorkTrackingSystemConnection connection)
            => WithCreateDefaults(connection.Id, UsesItemTypes(connection.WorkTrackingSystem));

        public static ConnectionMapping From(int connectionId, IReadOnlyDictionary<LevelId, IReadOnlyList<string>> typesByLevel)
            => new(connectionId, typesByLevel);

        // Linear has no item types: a unit there reads every item its query returns.
        public static bool UsesItemTypes(WorkTrackingSystems tracker) => tracker != WorkTrackingSystems.Linear;

        public ResolveOutcome Resolve(LevelId level, IEnumerable<string> requested)
        {
            var listed = TypesOn(level);
            var wanted = requested.ToList();

            var notListed = wanted.Find(type => !listed.Contains(type, StringComparer.OrdinalIgnoreCase));
            if (notListed is not null)
            {
                return new ResolveOutcome.TypeNotMapped(notListed, level, ConnectionId);
            }

            return new ResolveOutcome.MappedSubset([.. listed.Where(type => wanted.Contains(type, StringComparer.OrdinalIgnoreCase))]);
        }

        public IReadOnlyList<string> TypesOn(LevelId level)
            => typesByLevel.TryGetValue(level, out var types) ? types : [];

        public IReadOnlyList<string> TypesOnBothLevels()
        {
            var portfolio = TypesOn(LevelId.Portfolio);
            return [.. TypesOn(LevelId.Team).Where(type => portfolio.Contains(type, StringComparer.OrdinalIgnoreCase))];
        }

        private static IReadOnlyList<string> OnceEachWhateverTheCase(IEnumerable<string> types)
            => [.. types.Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}
