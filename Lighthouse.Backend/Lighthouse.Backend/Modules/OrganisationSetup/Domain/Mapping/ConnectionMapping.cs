using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping
{
    public sealed class ConnectionMapping
    {
        internal ConnectionMapping(int connectionId)
        {
            ConnectionId = connectionId;
        }

        public int ConnectionId { get; }

        public static ConnectionMapping WithCreateDefaults(int connectionId, bool connectorUsesItemTypes)
            => throw new NotImplementedException($"The create defaults of connection {connectionId} (item types used: {connectorUsesItemTypes}) are not built yet.");

        public static ConnectionMapping From(int connectionId, IReadOnlyDictionary<LevelId, IReadOnlyList<string>> typesByLevel)
            => throw new NotImplementedException($"A mapping of connection {connectionId} from {typesByLevel.Count} levels is not built yet.");

        public ResolveOutcome Resolve(LevelId level, IEnumerable<string> requested)
            => throw new NotImplementedException($"Resolving {requested} on level {level.Key} of connection {ConnectionId} is not built yet.");

        public IReadOnlyList<string> TypesOn(LevelId level)
            => throw new NotImplementedException($"The types on level {level.Key} of connection {ConnectionId} are not built yet.");

        public IReadOnlyList<string> TypesOnBothLevels()
            => throw new NotImplementedException($"The types on both levels of connection {ConnectionId} are not built yet.");
    }
}
