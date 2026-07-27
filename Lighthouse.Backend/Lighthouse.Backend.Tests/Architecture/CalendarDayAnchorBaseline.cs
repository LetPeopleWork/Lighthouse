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
    /// 49 sites were verified at HEAD (RCA section 5, 24 files); steps 02-01, 02-03, 02-04 and 02-06 have
    /// since migrated 45 of them, leaving 4. The guard fails on any anchor NOT listed here and on
    /// any entry listed here that no longer exists, so the list can only shrink. When the last entry
    /// goes, the baseline is deleted and the guard becomes a hard fail (RCA section 6, step 5).
    ///
    /// Deliberately NOT in this list: the four tracker history cutoffs of decision 4
    /// (Services/Implementation/WorkItems/WorkItemService.cs, the ADO connector and the Jira
    /// connector). They stay UTC on purpose - a tracker's history window is an instant offset, not a
    /// calendar day - and they spell it <c>DateTime.UtcNow.AddDays(...)</c>, which none of the three
    /// scanned patterns match. They need no baseline entry because the guard never sees them; the
    /// stated reason lives here rather than being a silent omission.
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

        public static readonly CalendarDayAnchorSite[] KnownSites =
        [
            // --- Forecast projection, windows, throughput defaults and historic-range detection ---
            // Step 02-04 migrated this whole cluster onto ILighthouseClock. Twenty-five lines across
            // nine files now take the day from clock.Today / clock.TodayAsUtcMidnight, and the three
            // ItemCreationPredictionInputDto default initialisers were deleted rather than migrated
            // (decision 5 - every one of those properties is [JsonRequired], so model binding can
            // never reach the default). With them went API/ForecastController.cs, the one file that
            // carried BOTH spellings in a single request lifetime - the branch-B evidence.
            // --- Snapshot recording / day keys (RCA 5(a)) --------------------------------------
            // Step 02-03 migrated this whole cluster onto ILighthouseClock: the four snapshot
            // recording handlers now take the clock by constructor injection and every day key is
            // clock.Today (a DateOnly), with clock.TodayAsUtcMidnight where a DateTime range end is
            // still required. Six baselined lines went, and with them the two derived
            // DateOnly.FromDateTime(endDate) reductions and the bare DateTime.UtcNow day argument
            // that the three scanned spellings never saw.

            // --- Validation / licensing / write-back (RCA 5(a)) --------------------------------
            new("Services/Implementation/BaselineValidationService.cs", UtcNowDate, "Baseline validity day; moves to clock.Today."),
            new("Services/Implementation/BaselineValidationService.cs", UtcNowDate, "Baseline validity day; moves to clock.Today."),
            new("Services/Implementation/Licensing/LicenseService.cs", UtcNowDate, "License expiry day; moves to the instance zone per decision 1 - a licensee keeps premium through their own last day."),
            new("Services/Implementation/WriteBackTriggerService.cs", UtcNowDate, "Forecast window start day; moves to clock.Today."),

            // --- Demo data (had to move with the read paths or the E2E date assertions desync) --
            // Step 02-06 migrated the demo seam onto ILighthouseClock. The CSV placeholder
            // resolution in DemoDataFactory now anchors on clock.Today, so every seeded date stays
            // in lockstep with the windows the migrated read paths compute; the burnup seeding and
            // both demo backfill handlers take the same day. Seven anchors across six entries went,
            // including the DateOnly.FromDateTime(today) derivation the DemoPercentiles line carried.
        ];
    }
}
