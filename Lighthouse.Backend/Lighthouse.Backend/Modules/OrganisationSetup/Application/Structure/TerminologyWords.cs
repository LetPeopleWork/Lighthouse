using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    /// <summary>
    /// The eight Terminology words that name the two levels. An entry left empty means its default word, as the
    /// screens have always read it.
    /// </summary>
    public sealed record TerminologyWords(TermPair Team, TermPair WorkItem, TermPair Portfolio, TermPair Feature)
    {
        private const string TeamKey = "team";
        private const string TeamsKey = "teams";
        private const string WorkItemKey = "workItem";
        private const string WorkItemsKey = "workItems";
        private const string PortfolioKey = "portfolio";
        private const string PortfoliosKey = "portfolios";
        private const string FeatureKey = "feature";
        private const string FeaturesKey = "features";

        private static readonly string[] TheEightKeys = [TeamKey, TeamsKey, WorkItemKey, WorkItemsKey, PortfolioKey, PortfoliosKey, FeatureKey, FeaturesKey];

        public static bool NamesALevel(string key) => TheEightKeys.Contains(key, StringComparer.Ordinal);

        public static TerminologyWords From(IEnumerable<TerminologyEntry> entries)
        {
            var byKey = entries.Where(entry => NamesALevel(entry.Key)).ToDictionary(entry => entry.Key, WordOf, StringComparer.Ordinal);

            var missing = TheEightKeys.Where(key => !byKey.ContainsKey(key)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The Terminology rows [{string.Join(", ", missing)}] are missing, so the levels cannot be named. "
                    + "They are written by the Terminology seeder, which runs before the levels are built; start the application again so it seeds them.");
            }

            return new TerminologyWords(
                new TermPair(byKey[TeamKey], byKey[TeamsKey]),
                new TermPair(byKey[WorkItemKey], byKey[WorkItemsKey]),
                new TermPair(byKey[PortfolioKey], byKey[PortfoliosKey]),
                new TermPair(byKey[FeatureKey], byKey[FeaturesKey]));
        }

        public static TerminologyWords Of(OrganisationStructure structure)
        {
            var team = structure.Levels.Single(level => level.Id == LevelId.Team);
            var portfolio = structure.Levels.Single(level => level.Id == LevelId.Portfolio);
            return new TerminologyWords(team.UnitName, team.ItemName, portfolio.UnitName, portfolio.ItemName);
        }

        /// <summary>The word an entry is answered with; only the eight level keys have one.</summary>
        public string WordFor(string key) => key switch
        {
            TeamKey => Team.Singular,
            TeamsKey => Team.Plural,
            WorkItemKey => WorkItem.Singular,
            WorkItemsKey => WorkItem.Plural,
            PortfolioKey => Portfolio.Singular,
            PortfoliosKey => Portfolio.Plural,
            FeatureKey => Feature.Singular,
            FeaturesKey => Feature.Plural,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, $"Only the keys [{string.Join(", ", TheEightKeys)}] name a level; ask NamesALevel first."),
        };

        /// <summary>These words with the given entries applied; an entry left empty takes its default word.</summary>
        public TerminologyWords With(TerminologyEntry entry) => entry.Key switch
        {
            TeamKey => this with { Team = Team with { Singular = WordOf(entry) } },
            TeamsKey => this with { Team = Team with { Plural = WordOf(entry) } },
            WorkItemKey => this with { WorkItem = WorkItem with { Singular = WordOf(entry) } },
            WorkItemsKey => this with { WorkItem = WorkItem with { Plural = WordOf(entry) } },
            PortfolioKey => this with { Portfolio = Portfolio with { Singular = WordOf(entry) } },
            PortfoliosKey => this with { Portfolio = Portfolio with { Plural = WordOf(entry) } },
            FeatureKey => this with { Feature = Feature with { Singular = WordOf(entry) } },
            FeaturesKey => this with { Feature = Feature with { Plural = WordOf(entry) } },
            _ => this,
        };

        public IReadOnlyList<RenameOutcome> RenameTheLevelsOf(OrganisationStructure structure)
            => [structure.Rename(LevelId.Team, Team, WorkItem), structure.Rename(LevelId.Portfolio, Portfolio, Feature)];

        public OrganisationStructure SeedTheStructure() => OrganisationStructure.Seed(Team, WorkItem, Portfolio, Feature);

        private static string WordOf(TerminologyEntry entry)
            => string.IsNullOrWhiteSpace(entry.Value) ? entry.DefaultValue : entry.Value;
    }
}
