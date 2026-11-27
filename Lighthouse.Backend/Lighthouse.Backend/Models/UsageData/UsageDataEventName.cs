namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Everything a browser is allowed to report. A name that is not on this list does not arrive as
    /// an unknown event to be dealt with later - it fails to be read at all, so an event nobody wrote
    /// down cannot be sent by accident or on purpose.
    /// </summary>
    public enum UsageDataEventName
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        TeamTabOpened = 0,

        PortfolioTabOpened = 1,

        TeamCreated = 2,

        TeamDeleted = 3,

        PortfolioCreated = 4,

        PortfolioDeleted = 5,

        TeamManualForecastRun = 6,

        WorkTrackingSystemConnected = 7,

        TeamRefreshTriggered = 8,

        PortfolioRefreshTriggered = 9,

        OptionalFeatureToggled = 10,

        TeamForecastRealityCheckRun = 11,

        TeamRefinementConfigured = 12,

        TeamSizingVoteCast = 13,

        TeamSizingReadinessReached = 14,

        TeamRefinementDayVerdictShown = 15,

        ReportCreated = 16,

        ReportOpened = 17,

        ReportDeleted = 18,

        WorkTrackingSystemFieldsChanged = 19,

        /// <summary>
        /// Somebody chose to export a report as a PDF. It counts the choice, so a print dialog that was
        /// then cancelled counts too: nothing tells Lighthouse whether a file was saved.
        /// </summary>
        ReportPdfExportStarted = 20,

        /// <summary>
        /// Somebody opened the Reports page and it showed its list. It carries nothing, not even how many
        /// reports there were.
        /// </summary>
        AllReportsListed = 21,

        /// <summary>
        /// Somebody chose Create in Create reports and the request was sent, whether or not any report was made.
        /// It carries nothing: not how many Teams or Portfolios, not which, not how many reports were made. Each
        /// report made is counted as <see cref="ReportCreated"/> as well.
        /// </summary>
        ReportBulkCreationStarted = 22,

        /// <summary>
        /// Somebody confirmed Remove for the reports picked on the Reports page and the request was sent. It carries
        /// nothing: not how many reports, not which, not their Teams or Portfolios. Each report it removed is counted as
        /// <see cref="ReportDeleted"/> as well.
        /// </summary>
        ReportBulkRemovalStarted = 23,

        /// <summary>
        /// Somebody chose Export PDF for two or more reports picked on the Reports page, counted when chosen, before
        /// anything is printed. It carries nothing: not how many reports, not which, not their Teams or Portfolios, and
        /// not whether a file was saved. Exporting exactly one picked report counts as <see cref="ReportPdfExportStarted"/>.
        /// </summary>
        ReportCombinedPdfExportStarted = 24,

        /// <summary>
        /// Somebody saved parent sources of their own with two or more entries and the server accepted it. Saving an
        /// unchanged list, a list of one entry or following the default does not count. It carries nothing: not which
        /// fields, not how many, not whose.
        /// </summary>
        ParentSourcesSavedWithSeveralEntries = 25,

        /// <summary>
        /// A System Admin added an SSO group rule that gives Viewer or Admin on every Team and Portfolio, or turned
        /// "Everyone signed in" on. It carries nothing: not the group, not who did it, not how many people it reaches.
        /// </summary>
        SystemWideAccessGranted = 26,

        /// <summary>
        /// Somebody opened the list of outliers on a Cycle Time scatterplot, the chip in the widget header that names
        /// the closed Work Items too long to be drawn. It carries nothing: not which Team or Portfolio, not how many
        /// outliers, not which items.
        /// </summary>
        OutlierListOpened = 27,

        /// <summary>
        /// Somebody switched outlier handling off on a Cycle Time scatterplot, to see every closed Work Item. It
        /// carries nothing: not which Team or Portfolio, not how many outliers, not which items.
        /// </summary>
        OutlierHandlingSwitchedOff = 28,

        /// <summary>
        /// Somebody switched default Feature sizing off in a Portfolio's settings. It carries nothing: not which
        /// Portfolio, not how many Features, not whose.
        /// </summary>
        DefaultFeatureSizingSwitchedOff = 29,

        /// <summary>
        /// Somebody added a note to a report and the server accepted it. It carries nothing: not which report, not
        /// which Team or Portfolio, not the text or its length, not who wrote it.
        /// </summary>
        ReportNoteAdded = 30,

        /// <summary>
        /// Somebody opened the list of closed Work Items behind the Within SLE widget on a Team's or a Portfolio's
        /// Flow Overview. It carries nothing: not which Team or Portfolio, not the share, not how many items.
        /// </summary>
        WithinSleListOpened = 31,

        /// <summary>
        /// Somebody switched the trailing window of the Within SLE Over Time chart in a Team's or a Portfolio's
        /// Predictability. It carries nothing: not which Team or Portfolio, not the share, not the window chosen.
        /// </summary>
        WithinSleHorizonSwitched = 32,
    }
}
