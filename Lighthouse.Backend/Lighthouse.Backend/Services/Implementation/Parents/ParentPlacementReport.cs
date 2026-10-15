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
            => $"{record.ReferenceId} under {record.Placement.Key} ({TheRuleThatDecided(record.Placement.DecidedBy)}; "
                + $"set aside {string.Join(", ", record.Placement.SetAside.Select(candidate => candidate.Key))})";

        private static string WhyItWentNowhere(RecordPlacement record)
            => $"{record.ReferenceId} (linked to {string.Join(", ", record.Links.Candidates)}; {TheReason(record.Placement.WhyNotPlaced)})";

        private static string TheRuleThatDecided(ParentPlacementRule rule) => rule switch
        {
            ParentPlacementRule.OnlyFeatureAmongLinks => "only Feature",
            _ => throw new NotSupportedException($"No placement is decided by {rule} yet, so there are no words for it."),
        };

        private static string TheReason(ParentNotPlacedReason reason) => reason switch
        {
            ParentNotPlacedReason.NoneIsAStoredFeature => "none is a Feature Lighthouse holds",
            ParentNotPlacedReason.MoreThanOneStoredFeature => "more than one is a Feature Lighthouse holds",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "A record that was not placed always has a reason."),
        };
    }

    /// <summary>
    /// One record whose links named several issues, what those links were, and what placing it decided.
    /// </summary>
    public readonly record struct RecordPlacement(string ReferenceId, ParentResolution Links, ParentPlacement Placement);
}
