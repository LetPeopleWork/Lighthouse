namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    /// <param name="TypesByLevel">Keyed by the stored level key; a level without a row is absent.</param>
    public sealed record ConnectionMappingView(
        int ConnectionId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> TypesByLevel,
        IReadOnlyList<string> TypesOnBothLevels);
}
