namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>What the Reports module needs to know about an owner it does not own: that it exists, and how far back it keeps finished items.</summary>
    public sealed record ReportOwnerFacts(int DoneItemsCutoffDays);

    public interface IReportOwnerLookup
    {
        ReportOwnerFacts? Find(ReportOwner owner);
    }
}
