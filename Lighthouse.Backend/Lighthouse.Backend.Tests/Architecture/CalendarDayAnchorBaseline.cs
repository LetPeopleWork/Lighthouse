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
    /// These were the 49 sites verified at HEAD (RCA section 5, 24 files); step 02-01 migrated the
    /// five entity anchors (Models/Team.cs x2, Models/Feature.cs, Models/Delivery.cs,
    /// Models/WorkItemBase.cs) onto a caller-supplied DateOnly, leaving 44. The guard fails on any
    /// anchor NOT listed here, so no 50th site can be added while the migration runs, and it fails
    /// on any entry listed here that no longer exists, so the list cannot rot into a permanent
    /// allowlist as each phase-02 cluster shrinks it. When the last entry goes, the baseline is
    /// deleted and the guard becomes a hard fail (RCA section 6, step 5).
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
            // --- Forecast projection & windows (RCA 5(a)) --------------------------------------
            new("API/DTO/WhenForecastDto.cs", UtcNowDate, "Forecast projection start day; moves to clock.Today."),
            new("API/FeatureForecastWindow.cs", UtcNowDate, "Forecast window start day; moves to clock.Today."),
            new("API/FeaturesController.cs", UtcNowDate, "Forecast window start day; moves to clock.Today."),
            new("API/DeliveryRulesController.cs", UtcNowDate, "Forecast window start day; moves to clock.Today."),
            new("API/DeliveriesController.cs", UtcNowDate, "Forecast window start day; moves to clock.Today."),
            new("API/DeliveriesController.cs", UtcNowDate, "Forecast horizon lower bound; moves to clock.Today."),
            new("API/DeliveriesController.cs", UtcNowDate, "Forecast horizon comparison and fallback; moves to clock.Today."),

            // --- API/ForecastController.cs - the branch-B evidence -----------------------------
            // This one file carries BOTH spellings. DateTime.Today reads the HOST zone and
            // DateTime.UtcNow.Date reads UTC, so on a non-UTC standalone instance the two endpoints
            // of the same controller anchor on different calendar days. No runtime test can show
            // this - an injected instant never reaches a statically-read clock - which is why this
            // scanner is the only deterministic proof of branch B.
            new("API/ForecastController.cs", DateTimeToday, "Manual-forecast anchor day, host-zone spelling; moves to clock.Today."),
            new("API/ForecastController.cs", UtcNowDate, "Forecast projection anchor day, UTC spelling; moves to clock.Today."),
            new("API/ForecastController.cs", UtcNowDate, "Forecast projection anchor day, UTC spelling; moves to clock.Today."),
            new("API/ForecastController.cs", UtcNowDate, "Forecast projection anchor day, UTC spelling; moves to clock.Today."),
            new("API/ForecastController.cs", UtcNowDate, "Forecast projection anchor day, UTC spelling; moves to clock.Today."),
            new("API/ForecastController.cs", DateOnlyFromToday, "Item-creation prediction minimum start day; moves to clock.Today."),
            new("API/ForecastController.cs", DateTimeToday, "ItemCreationPredictionInputDto default initialiser; DELETED per decision 5 (the property is [JsonRequired])."),
            new("API/ForecastController.cs", DateTimeToday, "ItemCreationPredictionInputDto default initialiser; DELETED per decision 5 (the property is [JsonRequired])."),
            new("API/ForecastController.cs", DateTimeToday, "ItemCreationPredictionInputDto default initialiser; DELETED per decision 5 (the property is [JsonRequired])."),

            // --- Throughput defaults (RCA 5(a)) ------------------------------------------------
            new("Services/Implementation/TeamMetricsService.cs", UtcNowDate, "Current-WIP snapshot day; moves to clock.Today."),
            new("Services/Implementation/TeamMetricsService.cs", UtcNowDate, "Metric range end day; moves to clock.Today."),
            new("Services/Implementation/TeamMetricsService.cs", UtcNowDate, "Current features-in-progress day; moves to clock.Today."),
            new("Services/Implementation/TeamMetricsService.cs", UtcNowDate, "Metric range end day; moves to clock.Today."),

            // --- Historic-range detection (RCA 5(a)) -------------------------------------------
            new("API/PortfolioMetricsController.cs", UtcNowDate, "Historic-vs-live range detection; moves to clock.Today."),
            new("API/PortfolioMetricsController.cs", UtcNowDate, "Historic-vs-live range detection; moves to clock.Today."),
            new("API/PortfolioMetricsController.cs", UtcNowDate, "Historic-vs-live range detection; moves to clock.Today."),
            new("API/PortfolioMetricsController.cs", UtcNowDate, "Historic-vs-live range detection; moves to clock.Today."),
            new("API/PortfolioMetricsController.cs", DateOnlyFromUtcNowDate, "Derived DateOnly reduction of the UTC day; same defect, moves to clock.Today."),
            new("API/TeamMetricsController.cs", UtcNowDate, "Historic-vs-live range detection; moves to clock.Today."),
            new("API/TeamMetricsController.cs", UtcNowDate, "Historic-vs-live range detection; moves to clock.Today."),
            new("API/TeamMetricsController.cs", DateOnlyFromUtcNowDate, "Derived DateOnly reduction of the UTC day; same defect, moves to clock.Today."),

            // --- Snapshot recording / day keys (RCA 5(a)) --------------------------------------
            new("Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs", UtcNowDate, "Snapshot day key; moves to clock.TodayAsUtcMidnight, then to a DateOnly column (decision 8)."),
            new("Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs", UtcNowDate, "Snapshot day key; moves to clock.TodayAsUtcMidnight, then to a DateOnly column (decision 8)."),
            new("Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs", UtcNowDate, "Snapshot day key; moves to clock.TodayAsUtcMidnight, then to a DateOnly column (decision 8)."),
            new("Services/Implementation/DomainEvents/PercentilesOverTimeRecordingHandler.cs", DateTimeToday, "Snapshot range end day; moves to clock.Today."),
            new("Services/Implementation/DomainEvents/BlockedCountSnapshotRecordingHandler.cs", DateOnlyFromToday, "Snapshot day key; moves to clock.Today."),
            new("Services/Implementation/DomainEvents/ProcessBehaviorRecordingHandler.cs", DateTimeToday, "Snapshot range end day; moves to clock.Today."),

            // --- Validation / licensing / write-back (RCA 5(a)) --------------------------------
            new("Services/Implementation/BaselineValidationService.cs", UtcNowDate, "Baseline validity day; moves to clock.Today."),
            new("Services/Implementation/BaselineValidationService.cs", UtcNowDate, "Baseline validity day; moves to clock.Today."),
            new("Services/Implementation/Licensing/LicenseService.cs", UtcNowDate, "License expiry day; moves to the instance zone per decision 1 - a licensee keeps premium through their own last day."),
            new("Services/Implementation/WriteBackTriggerService.cs", UtcNowDate, "Forecast window start day; moves to clock.Today."),

            // --- Demo data (must move with the above or E2E desyncs) ---------------------------
            new("Factories/DemoDataFactory.cs", UtcNowDate, "Demo-data anchor day; moves to clock.Today."),
            new("Services/Implementation/DemoDataService.cs", UtcNowDate, "Demo-data anchor day; moves to clock.Today."),
            new("Services/Implementation/DemoDataService.cs", UtcNowDate, "Demo-data anchor day; moves to clock.Today."),
            new("Services/Implementation/DemoDataService.cs", UtcNowDate, "Demo-data anchor day; moves to clock.Today."),
            new("Services/Implementation/DomainEvents/DemoBlockedHistoryBackfillHandler.cs", DateTimeToday, "Demo backfill anchor day; moves to clock.Today."),
            new("Services/Implementation/DomainEvents/DemoPercentilesBackfillHandler.cs", DateOnlyFromToday, "Demo backfill anchor day; moves to clock.Today."),
        ];
    }
}
