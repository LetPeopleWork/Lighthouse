namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// One entry per production source line that still anchors "today" on UTC.
    /// <paramref name="RelativePath"/> is relative to the production project root and always uses
    /// forward slashes. <paramref name="Anchors"/> records which spelling(s) the line uses, joined
    /// in <see cref="CalendarDayAnchorBaseline.AnchorPatterns"/> order - that is the branch-B
    /// evidence: <c>API/ForecastController.cs</c> carries BOTH spellings in one file, which is why
    /// its two endpoints disagree about the calendar day on a non-UTC host.
    /// </summary>
    public sealed record CalendarDayAnchorSite(string RelativePath, string Anchors, string Reason);

    /// <summary>
    /// Bug #5567 - the warn-list for the anchor-seam source guard (RCA section 8-T2).
    ///
    /// 49 sites were verified at HEAD (RCA section 5, 24 files); steps 02-01 through 02-07 have
    /// migrated all of them, so the list is now EMPTY and stays that way. The guard fails on any
    /// anchor NOT listed here and on any entry listed here that no longer exists, so an empty list
    /// means every calendar day in production comes from ILighthouseClock. Step 03-01 turns the
    /// guard into a hard fail and deletes this type (RCA section 6, step 5).
    ///
    /// Deliberately NOT in this list: the four tracker history cutoffs of decision 4
    /// (Services/Implementation/WorkItems/WorkItemService.cs, the ADO connector and the Jira
    /// connector). They stay UTC on purpose - a tracker's history window is an instant offset, not a
    /// calendar day - and they spell it <c>DateTime.UtcNow.AddDays(...)</c>, which none of the three
    /// scanned patterns match. They need no baseline entry because the guard never sees them; each
    /// of those four lines now carries the same reason as a comment at the site.
    /// </summary>
    public static class CalendarDayAnchorBaseline
    {
        public const string DateOnlyFromDateTime = "DateOnly.FromDateTime(DateTime.";

        public const string DateTimeToday = "DateTime.Today";

        public const string UtcNowDate = "UtcNow.Date";

        public const string DateOnlyFromToday = DateOnlyFromDateTime + " + " + DateTimeToday;

        public const string DateOnlyFromUtcNowDate = DateOnlyFromDateTime + " + " + UtcNowDate;

        /// <summary>
        /// The three spellings of the defect. Scanned independently of one another, so a line such as
        /// <c>DateOnly.FromDateTime(DateTime.UtcNow.Date)</c> reports both of the patterns it carries.
        /// Order is significant: it is the order the spellings are joined in for a baseline entry.
        /// </summary>
        public static readonly string[] AnchorPatterns =
        [
            DateOnlyFromDateTime,
            DateTimeToday,
            UtcNowDate,
        ];

        /// <summary>
        /// Empty since step 02-07. Every cluster the RCA inventoried has been migrated: the forecast
        /// windows and throughput defaults (02-04), the snapshot day keys (02-03), the entity-level
        /// age and cycle-time reductions (02-01), the demo seam (02-06) and finally validation,
        /// licensing, write-back and delivery dates (02-07). It only ever shrinks - a new anchor is a
        /// guard failure, not a new entry.
        /// </summary>
        public static readonly CalendarDayAnchorSite[] KnownSites = [];
    }
}
