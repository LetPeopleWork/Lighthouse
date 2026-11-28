namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure
{
    /// <summary>
    /// The levels of an organisation: exactly two, the one where the work is done (ordinal 1) and the one above it.
    /// Levels is the read side the query maps to its views; every change goes through Rename.
    /// </summary>
    public sealed class OrganisationStructure
    {
        private const int TheLevelsOfAnOrganisation = 2;

        private readonly List<Level> levels;

        internal OrganisationStructure(IReadOnlyList<Level> levels)
        {
            if (levels.Count != TheLevelsOfAnOrganisation || levels.Select(level => level.Id).Distinct().Count() != TheLevelsOfAnOrganisation)
            {
                throw new ArgumentException(
                    $"An organisation has exactly {TheLevelsOfAnOrganisation} levels with distinct keys, but [{string.Join(", ", levels.Select(level => level.Id.Key))}] were given. "
                    + "Build a structure through OrganisationStructure.Seed, or load the stored one, which the start-up step writes with both levels.",
                    nameof(levels));
            }

            this.levels = [.. levels.OrderBy(level => level.Ordinal)];
        }

        public IReadOnlyList<Level> Levels => levels;

        public static OrganisationStructure Seed(TermPair teamUnit, TermPair workItem, TermPair portfolioUnit, TermPair feature)
            => new([
                new Level(LevelId.Team, 1, teamUnit, workItem),
                new Level(LevelId.Portfolio, 2, portfolioUnit, feature),
            ]);

        public RenameOutcome Rename(LevelId level, TermPair unitName, TermPair itemName)
        {
            var position = levels.FindIndex(candidate => candidate.Id == level);
            if (position < 0)
            {
                return new RenameOutcome.LevelNotFound(level);
            }

            var missing = MissingName(unitName, itemName);
            if (missing is not null)
            {
                return new RenameOutcome.NameMissing(level, missing);
            }

            levels[position] = levels[position].Named(unitName, itemName);
            return new RenameOutcome.Renamed(level);
        }

        private static string? MissingName(TermPair unitName, TermPair itemName)
        {
            if (string.IsNullOrWhiteSpace(unitName.Singular))
            {
                return "UnitSingular";
            }

            if (string.IsNullOrWhiteSpace(unitName.Plural))
            {
                return "UnitPlural";
            }

            if (string.IsNullOrWhiteSpace(itemName.Singular))
            {
                return "ItemSingular";
            }

            return string.IsNullOrWhiteSpace(itemName.Plural) ? "ItemPlural" : null;
        }
    }
}
