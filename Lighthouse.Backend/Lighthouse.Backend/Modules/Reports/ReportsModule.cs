using Lighthouse.Backend.Modules.Reports.Application.Metrics;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Services.Implementation.Repositories;

namespace Lighthouse.Backend.Modules.Reports
{
    public static class ReportsModule
    {
        public static IServiceCollection AddReports(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ReportRepository>();
            serviceCollection.AddScoped<IReportReader>(provider => provider.GetRequiredService<ReportRepository>());
            serviceCollection.AddScoped<IReportWriter>(provider => provider.GetRequiredService<ReportRepository>());
            serviceCollection.AddScoped<IReportNoteStore>(provider => provider.GetRequiredService<ReportRepository>());
            serviceCollection.AddScoped<IReportNoteQueries, ReportNoteQueries>();
            serviceCollection.AddScoped<IReportNoteCommands, ReportNoteCommands>();
            serviceCollection.AddScoped<IReportOwnerLookup, ReportOwnerLookup>();
            serviceCollection.AddScoped<TeamReportMetricSeries>();
            serviceCollection.AddScoped<PortfolioReportMetricSeries>();
            serviceCollection.AddScoped<IReportMetricSeries, ReportMetricSeriesByOwner>();
            serviceCollection.AddScoped<IReportMetric, CycleTimeReportMetric>();
            serviceCollection.AddScoped<IReportMetric, ThroughputReportMetric>();
            serviceCollection.AddScoped<IReportMetric, WipReportMetric>();
            serviceCollection.AddScoped<IReportMetric, WorkItemAgeReportMetric>();
            serviceCollection.AddScoped<IReportMetric, ArrivalsReportMetric>();
            serviceCollection.AddScoped<ReportMetricCatalog>();
            serviceCollection.AddScoped<IReportTemplate, ThenAndNowTemplate>();
            serviceCollection.AddScoped<IReportQueries, ReportQueries>();
            serviceCollection.AddScoped<IReportCommands, ReportCommands>();
            serviceCollection.AddScoped<IReportBulkCommands, ReportBulkCommands>();
            serviceCollection.AddScoped<IReportBulkRemover, ReportBulkRemover>();

            return serviceCollection;
        }
    }
}
