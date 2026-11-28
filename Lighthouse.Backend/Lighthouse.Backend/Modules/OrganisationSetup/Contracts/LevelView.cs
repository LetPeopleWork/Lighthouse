namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    /// <param name="Level">The stored level key, such as Team or Portfolio.</param>
    public sealed record LevelView(string Level, int Ordinal, string UnitSingular, string UnitPlural, string ItemSingular, string ItemPlural);
}
