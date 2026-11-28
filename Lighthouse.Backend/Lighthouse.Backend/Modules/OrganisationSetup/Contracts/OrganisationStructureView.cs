namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    public sealed record OrganisationStructureView(IReadOnlyList<LevelView> Levels);
}
