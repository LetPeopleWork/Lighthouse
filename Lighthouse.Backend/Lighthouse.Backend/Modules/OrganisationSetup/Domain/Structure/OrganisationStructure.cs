namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure
{
    public sealed class OrganisationStructure
    {
        internal OrganisationStructure(IReadOnlyList<Level> levels)
        {
            Levels = levels;
        }

        public IReadOnlyList<Level> Levels { get; }

        public static OrganisationStructure Seed(TermPair teamUnit, TermPair workItem, TermPair portfolioUnit, TermPair feature)
            => throw new NotImplementedException($"Seeding the structure from {teamUnit}, {workItem}, {portfolioUnit} and {feature} is not built yet.");

        public RenameOutcome Rename(LevelId level, TermPair unitName, TermPair itemName)
            => throw new NotImplementedException($"Renaming level {level.Key} of {Levels.Count} to {unitName} and {itemName} is not built yet.");
    }
}
