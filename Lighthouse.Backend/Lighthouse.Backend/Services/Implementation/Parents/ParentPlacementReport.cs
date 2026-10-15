using System.Globalization;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Parents
{
    /// <summary>
    /// The words a refresh writes about the records whose links named several issues: one line for those it
    /// placed and one for those it could not, never one per record, and nothing for an outcome with no
    /// records. A bulk edit that went wrong produces these by the hundred, and a line per record would bury
    /// the rest of the refresh under them.
    /// </summary>
    public static class ParentPlacementReport
    {
        private const string WhoseLinksNameMoreThanOneIssue = " whose links name more than one issue: ";

        private const string AndTheNext = "; ";

        public static string Team(string name) => $"Team \"{name}\"";

        public static string Portfolio(string name) => $"Portfolio \"{name}\"";

        public static string ParentsOfThePortfoliosFeatures(string name) => $"the parents of Portfolio \"{name}\"'s Features";

        public static List<string> Lines(string owner, IReadOnlyList<RecordPlacement> placements)
        {
            var placed = placements.Where(record => record.Placement.Outcome == ParentPlacementOutcome.Placed).ToList();
            var notPlaced = placements.Where(record => record.Placement.Outcome == ParentPlacementOutcome.NotPlaced).ToList();
            var lines = new List<string>();

            if (placed.Count > 0)
            {
                lines.Add($"{owner} placed {Records(placed.Count)}{WhoseLinksNameMoreThanOneIssue}"
                    + string.Join(AndTheNext, placed.Select(WhereItWent)));
            }

            if (notPlaced.Count > 0)
            {
                lines.Add($"{owner} could not place {Records(notPlaced.Count)}{WhoseLinksNameMoreThanOneIssue}"
                    + string.Join(AndTheNext, notPlaced.Select(WhyItWentNowhere))
                    + ". It takes no parent from its links.");
            }

            return lines;
        }

        private static string Records(int count) => count == 1 ? "1 record" : $"{count} records";

        private static string WhereItWent(RecordPlacement record)
            => $"{record.ReferenceId} under {record.Placement.Key} ({TheRuleThatDecided(record.Placement)}; "
                + $"set aside {string.Join(", ", record.Placement.SetAside.Select(Named))})";

        private static string Named(ParentCandidate candidate) => WithTheDay(candidate.Key, candidate.LinkedAt);

        private static string WhyItWentNowhere(RecordPlacement record)
            => $"{record.ReferenceId} (linked to {string.Join(", ", record.Links.Candidates)}; {TheReason(record.Placement.WhyNotPlaced)})";

#pragma warning disable CS8524 // Every named rule has an arm; a value outside the enum is never stored, and a discard would hide a rule added later without words.
        private static string TheRuleThatDecided(ParentPlacement placement) => placement.DecidedBy switch
        {
            ParentPlacementRule.OnlyFeatureAmongLinks => "only Feature",
            ParentPlacementRule.OldestLink => WithTheDay("oldest link", placement.DecidedOn),
            ParentPlacementRule.EarliestCreated => WithTheDay("earliest created", placement.DecidedOn),
            ParentPlacementRule.LowestKey => "lowest key",
            ParentPlacementRule.None => throw new ArgumentOutOfRangeException(nameof(placement), placement.DecidedBy, "A placed record always names the rule that placed it."),
        };
#pragma warning restore CS8524

        /// <summary>
        /// The day is the UTC calendar day, written the same on every machine: read in the server's own zone,
        /// a link made late in the evening would show as made the next day.
        /// </summary>
        private static string WithTheDay(string words, DateTime? at)
            => at is { } instant
                ? string.Create(CultureInfo.InvariantCulture, $"{words}, {AsUtc(instant):yyyy-MM-dd}")
                : words;

        private static DateTime AsUtc(DateTime instant)
            => instant.Kind == DateTimeKind.Local ? instant.ToUniversalTime() : instant;

        private static string TheReason(ParentNotPlacedReason reason) => reason switch
        {
            ParentNotPlacedReason.NoneIsAStoredFeature => "none is a Feature Lighthouse holds",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "A record that was not placed always has a reason."),
        };
    }

    /// <summary>
    /// One record whose links named several issues, what those links were, and what placing it decided.
    /// </summary>
    public readonly record struct RecordPlacement(string ReferenceId, ParentResolution Links, ParentPlacement Placement);
}
