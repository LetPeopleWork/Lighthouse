namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>The layer of the Reports module a moving type goes to.</summary>
    internal enum ReportsLayer
    {
        Domain,
        Application,
        ApplicationMetrics,
    }

    /// <summary>The kind of file a target naming a moving Reports type lives in.</summary>
    internal enum ReportsTargetKind
    {
        Suppression,
        Analyser,
        CiSelection,
        TestBudget,
        HostUser,
        Stryker,
        LogOverride,
    }

    /// <summary>
    /// A type of the Reports module: found today in its old namespace, after the move in Lighthouse.Backend.Modules.Reports.Domain,
    /// .Application or .Application.Metrics. File is the source file that declares it, which moves whole, so every type of one file
    /// has one layer. A port file declares a port interface and stays in Application with every record and enum it declares.
    /// </summary>
    internal sealed record ReportsType(string Name, ReportsLayer Layer, string OldNamespace, string File, bool InAPortFile)
    {
        public const string NewRootNamespace = "Lighthouse.Backend.Modules.Reports";

        public string NewNamespace => Layer switch
        {
            ReportsLayer.Domain => $"{NewRootNamespace}.Domain",
            ReportsLayer.Application => $"{NewRootNamespace}.Application",
            ReportsLayer.ApplicationMetrics => $"{NewRootNamespace}.Application.Metrics",
            _ => throw new ArgumentOutOfRangeException(nameof(Layer), Layer, "A Reports type goes to the Domain, the Application or the Application's Metrics."),
        };

        public string OldFullName => $"{OldNamespace}.{Name}";

        public string NewFullName => $"{NewNamespace}.{Name}";
    }

    /// <summary>A Reports type that stays where it is, with the part of the code that owns it and why it stays.</summary>
    internal sealed record ReportsTypeThatStays(string Name, string Namespace, string Owner, string Reason)
    {
        public string FullName => $"{Namespace}.{Name}";
    }

    /// <summary>
    /// A target in a suppression, analyser, CI-selection, test-budget, host-user, Stryker or log-override file that names a moving
    /// type. Target is written from the type's name on, without its namespace, so the entry holds before and after the move.
    /// </summary>
    internal sealed record ReportsTarget(string File, string Target, ReportsTargetKind Kind);

    /// <summary>
    /// The Reports module, measured from the compiled code: every type that moves into Lighthouse.Backend.Modules.Reports with its
    /// layer and old namespace, the Reports types that stay with their owner and reason, and the targets that name a moving type.
    /// A type is listed with its old namespace and found in that namespace or in its new one, so the map holds before, during and
    /// after the move.
    /// </summary>
    internal static class ReportsMembership
    {
        public const string ImplementationNamespace = "Lighthouse.Backend.Services.Implementation.Reports";

        public const string MetricsNamespace = "Lighthouse.Backend.Services.Implementation.Reports.Metrics";

        public const string InterfacesNamespace = "Lighthouse.Backend.Services.Interfaces.Reports";

        private const string KernelOwner = "The shared kernel";

        private const string ApiOwner = "The Reports API, the driving adapter";

        private const string ReturnedThroughTheApi = "The Reports ports return it and the API sends it, so moving it would change the wire format";

        public static IReadOnlyList<string> ModuleRoot { get; } = ["ReportsModule"];

        public static IReadOnlyList<ReportsType> MovingTypes { get; } =
        [
            .. File(ImplementationNamespace, ReportsLayer.Domain, "ReportCapPolicy.cs", "ReportCapPolicy"),
            .. File(ImplementationNamespace, ReportsLayer.Domain, "ReportSeriesReading.cs", "ReportSeriesReading"),
            .. File(ImplementationNamespace, ReportsLayer.Domain, "ReportWindowPolicy.cs", "ReportWindowPolicy"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "PortfolioReportMetricSeries.cs", "PortfolioReportMetricSeries"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportBulkCommands.cs", "ReportBulkCommands"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportBulkRemover.cs", "ReportBulkRemover"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportCommands.cs", "ReportCommands"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportMetricSeriesByOwner.cs", "ReportMetricSeriesByOwner"),
            .. File(ImplementationNamespace, ReportsLayer.Domain, "ReportNoteCaller.cs", "ReportNoteCaller"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportNoteCommands.cs", "ReportNoteCommands"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportNoteQueries.cs", "ReportNoteQueries"),
            .. File(ImplementationNamespace, ReportsLayer.Domain, "ReportNoteRead.cs", "ReportNoteRead"),
            .. File("Lighthouse.Backend.Models.Reports", ReportsLayer.Domain, "ReportOwnerGoneException.cs", "ReportOwnerGoneException"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportOwnerLookup.cs", "ReportOwnerLookup"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ReportQueries.cs", "ReportQueries"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "TeamReportMetricSeries.cs", "TeamReportMetricSeries"),
            .. File(ImplementationNamespace, ReportsLayer.Application, "ThenAndNowTemplate.cs", "ThenAndNowTemplate"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "ArrivalsReportMetric.cs", "ArrivalsReportMetric"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "CycleTimeReportMetric.cs", "CycleTimeReportMetric"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "DailySeriesSummary.cs", "DailySeriesSummary"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "ReportMetricCatalog.cs", "ReportMetricCatalog"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "ThroughputReportMetric.cs", "ThroughputReportMetric"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "WindowedReportMetric.cs", "WindowedReportMetric"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "WipReportMetric.cs", "WipReportMetric"),
            .. File(MetricsNamespace, ReportsLayer.ApplicationMetrics, "WorkItemAgeReportMetric.cs", "WorkItemAgeReportMetric"),
            .. PortFile("IReportBulkCommands.cs", "IReportBulkCommands"),
            .. PortFile("IReportBulkRemover.cs", "IReportBulkRemover"),
            .. PortFile("IReportCommands.cs", "IReportCommands", "ReportEdit"),
            .. PortFile("IReportMetric.cs", "IReportMetric"),
            .. PortFile("IReportMetricSeries.cs", "IReportMetricSeries", "CycleTimePercentiles"),
            .. PortFile("IReportNoteCommands.cs", "IReportNoteCommands"),
            .. PortFile("IReportNoteQueries.cs", "IReportNoteQueries"),
            .. PortFile("IReportNoteStore.cs", "IReportNoteStore"),
            .. PortFile("IReportOwnerLookup.cs", "IReportOwnerLookup", "ReportOwnerFacts", "ReportOwnerRow"),
            .. PortFile("IReportOwnerWriteAccess.cs", "IReportOwnerWriteAccess"),
            .. PortFile("IReportQueries.cs", "IReportQueries"),
            .. PortFile("IReportReader.cs", "IReportReader"),
            .. PortFile("IReportTemplate.cs", "IReportTemplate", "ReportCalendar"),
            .. PortFile("IReportWriter.cs", "IReportWriter"),
        ];

        public static IReadOnlyList<ReportsTypeThatStays> TypesThatStay { get; } =
        [
            new("ReportRepository", "Lighthouse.Backend.Services.Implementation.Repositories", "Reports, through the persistence adapters",
                "Every repository stays with the persistence adapters until those are reshaped; it implements the Reports reader and writer ports from there"),
            new("Report", "Lighthouse.Backend.Models.Reports", KernelOwner, "The database context stores it as the reports table, so moving it would change the stored model"),
            new("ThenAndNowPayload", "Lighthouse.Backend.Models.Reports", KernelOwner, "It is the payload stored with each report, so moving it would change what is stored"),
            .. Stays(KernelOwner, "Lighthouse.Backend.Models.Reports", "The stored report and its payload are built from it, so it stays beside them",
                "ReportOwner",
                "ReportOwnerKind",
                "ReportWindow",
                "CapturedValue"),
            .. Stays(KernelOwner, "Lighthouse.Backend.Models.Reports", ReturnedThroughTheApi,
                "ReportOutcomeStatus",
                "ReportOutcome`1",
                "NewReport",
                "NewThenAndNow",
                "ReportTemplatesRead",
                "ReportTemplateRead",
                "ReportTemplateMetricRead",
                "ReportList",
                "ReportsAcrossOwners",
                "ReportCreationOptions",
                "ReportCreationTemplate",
                "ReportCreationOwner",
                "ReportSummary",
                "ThenAndNowSummary",
                "ReportRead",
                "ThenAndNowRead",
                "MetricRead",
                "ReportBulkRun",
                "ReportCreationStatus",
                "ReportOwnerOutcome",
                "ReportBulkCreation",
                "ReportRemovalStatus",
                "ReportRemovalOutcome",
                "ReportBulkRemoval",
                "ReportRefusalCodes"),
            new("ReportNote", "Lighthouse.Backend.Models.Reports", KernelOwner, "The database context stores it as the report notes table, so moving it would change the stored model"),
            .. Stays(ApiOwner, "Lighthouse.Backend.API", "A controller is the module's driving adapter and stays with the other controllers",
                "TeamReportsController",
                "PortfolioReportsController",
                "ReportsController",
                "ReportsControllerBase"),
            new("ReportOwnerWriteAccess", "Lighthouse.Backend.API", ApiOwner,
                "It answers the owner-write port from the caller's permissions, which only the API holds, so it is built in the API"),
            .. Stays(ApiOwner, "Lighthouse.Backend.API.DTO.Reports", "A request is the wire shape of the driving adapter and stays with the other ones",
                "NewReportRequest",
                "NewThenAndNowRequest",
                "NewReportsForOwnersRequest",
                "ReportOwnerRequest",
                "ReportEditRequest",
                "ReportEditThenAndNowRequest",
                "ReportRemovalRequest",
                "ReportNoteDto",
                "ReportNoteRequest"),
        ];

        public static IReadOnlyList<ReportsTarget> TargetsNamingAMovingType { get; } = [];

        private static IEnumerable<ReportsType> File(string oldNamespace, ReportsLayer layer, string file, params string[] names)
            => names.Select(name => new ReportsType(name, layer, oldNamespace, file, InAPortFile: false));

        private static IEnumerable<ReportsType> PortFile(string file, params string[] names)
            => names.Select(name => new ReportsType(name, ReportsLayer.Application, InterfacesNamespace, file, InAPortFile: true));

        private static IEnumerable<ReportsTypeThatStays> Stays(string owner, string inNamespace, string reason, params string[] names)
            => names.Select(name => new ReportsTypeThatStays(name, inNamespace, owner, reason));
    }
}
