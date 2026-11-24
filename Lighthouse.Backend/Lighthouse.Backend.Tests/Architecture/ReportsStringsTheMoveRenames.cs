namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A string in a test file that names a moving Reports type, file or namespace by its old Services path, and what the
    /// move makes of it: the text it reads after the move, or no text when the move deletes the string.
    /// </summary>
    internal sealed record ReportsRename(string File, string OldText, string? NewText)
    {
        public RenamedString AsRenamedString() => new(File, OldText, NewText ?? string.Empty);
    }

    /// <summary>
    /// Every string in the test project that names Reports by its old Services path when the Reports types are about to move
    /// into Lighthouse.Backend.Modules.Reports. The list was taken from a search of the test project before the move and only
    /// shrinks: an entry leaves the list in the commit that renames or deletes its string.
    /// </summary>
    internal static class ReportsStringsTheMoveRenames
    {
        private const string Architecture = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/";

        private const string OldImplementationFolder = "Services/Implementation/Reports/";

        private const string OldMetricsFolder = "Services/Implementation/Reports/Metrics/";

        private const string OldInterfacesFolder = "Services/Interfaces/Reports/";

        private const string NewApplicationFolder = "Modules/Reports/Application/";

        public static IReadOnlyList<ReportsRename> Entries { get; } =
        [
            new(Architecture + "FlowMetricsInboundDomainAndAdaptersArchUnitTest.cs",
                "Lighthouse.Backend.Services.Implementation.Reports.Metrics.DailySeriesSummary",
                "Lighthouse.Backend.Modules.Reports.Application.Metrics.DailySeriesSummary"),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Implementation.Reports.ReportCommands",
                "Lighthouse.Backend.Modules.Reports.Application.ReportCommands"),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Implementation.Reports.ReportQueries",
                "Lighthouse.Backend.Modules.Reports.Application.ReportQueries"),
            new(Architecture + "RepositoryLeakRatchetArchUnitTest.cs",
                OldImplementationFolder + "ReportOwnerLookup.cs",
                NewApplicationFolder + "ReportOwnerLookup.cs"),
            new(Architecture + "TrackerIntegrationModuleBoundariesArchUnitTest.cs",
                "Lighthouse.Backend.Services.Interfaces.Reports.IReportQueries",
                "Lighthouse.Backend.Modules.Reports.Application.IReportQueries"),

            // The platform lane's list of files that stay in Services names each moving Reports file; the row goes when its file moves.
            .. FilesThatLeaveTheStayingList(
                OldMetricsFolder,
                "ArrivalsReportMetric.cs",
                "CycleTimeReportMetric.cs",
                "DailySeriesSummary.cs",
                "ReportMetricCatalog.cs",
                "ThroughputReportMetric.cs",
                "WindowedReportMetric.cs",
                "WipReportMetric.cs",
                "WorkItemAgeReportMetric.cs"),
            .. FilesThatLeaveTheStayingList(
                OldImplementationFolder,
                "PortfolioReportMetricSeries.cs",
                "ReportBulkCommands.cs",
                "ReportBulkRemover.cs",
                "ReportCapPolicy.cs",
                "ReportCommands.cs",
                "ReportMetricSeriesByOwner.cs",
                "ReportOwnerLookup.cs",
                "ReportQueries.cs",
                "ReportSeriesReading.cs",
                "ReportWindowPolicy.cs",
                "TeamReportMetricSeries.cs",
                "ThenAndNowTemplate.cs"),
            .. FilesThatLeaveTheStayingList(
                OldInterfacesFolder,
                "IReportBulkCommands.cs",
                "IReportBulkRemover.cs",
                "IReportCommands.cs",
                "IReportMetric.cs",
                "IReportMetricSeries.cs",
                "IReportOwnerLookup.cs",
                "IReportOwnerWriteAccess.cs",
                "IReportQueries.cs",
                "IReportReader.cs",
                "IReportTemplate.cs",
                "IReportWriter.cs"),
        ];

        private static IEnumerable<ReportsRename> FilesThatLeaveTheStayingList(string folder, params string[] files)
            => files.Select(file => new ReportsRename(Architecture + "PlatformLaneUnitTestPlacementTest.cs", folder + file, NewText: null));
    }
}
