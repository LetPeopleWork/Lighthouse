namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What this instance calls its Teams, Portfolios and Features, as set under Settings, Terminology. A term
    /// left empty reads as the word Lighthouse seeds it with.
    /// </summary>
    public sealed record OwnerTerms(string Team, string Teams, string Portfolio, string Portfolios, string Feature)
    {
        public static readonly OwnerTerms Seeded = new("Team", "Teams", "Portfolio", "Portfolios", "Feature");

        public static OwnerTerms From(IEnumerable<TerminologyEntry> entries)
        {
            var configured = entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
                .GroupBy(entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);

            return new(
                configured.GetValueOrDefault("team", Seeded.Team),
                configured.GetValueOrDefault("teams", Seeded.Teams),
                configured.GetValueOrDefault("portfolio", Seeded.Portfolio),
                configured.GetValueOrDefault("portfolios", Seeded.Portfolios),
                configured.GetValueOrDefault("feature", Seeded.Feature));
        }

        public string PluralFor(DefaultHeldFor heldFor) => heldFor == DefaultHeldFor.Teams ? Teams : Portfolios;

        /// <summary>"Teams'" for a plural ending in s, "Staff's" for one that does not.</summary>
        public static string PossessiveOfPlural(string plural)
            => plural.EndsWith('s') ? $"{plural}'" : $"{plural}'s";
    }
}
