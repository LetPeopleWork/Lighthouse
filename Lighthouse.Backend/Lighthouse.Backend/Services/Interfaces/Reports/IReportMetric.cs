using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>One metric a Then &amp; Now report can show: what it freezes for Then, and what it computes for Now.</summary>
    public interface IReportMetric
    {
        string Key { get; }

        bool AppliesTo(ReportOwnerKind ownerKind);

        JsonObject CaptureThen(ReportOwner owner, ReportWindow thenWindow);

        MetricRead Read(JsonObject? capturedThen, ReportOwner owner, ReportWindow nowWindow);
    }
}
