namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>What the Reports module needs to know about an owner it does not own: that it exists, and how far back it keeps finished items.</summary>
    public sealed record ReportOwnerFacts(int DoneItemsCutoffDays);

    /// <summary>One owner as a list of every owner needs it: its kind, id and name, and how far back it keeps finished items.</summary>
    public sealed record ReportOwnerRow(ReportOwner Owner, string Name, int DoneItemsCutoffDays);

    public interface IReportOwnerLookup
    {
        ReportOwnerFacts? Find(ReportOwner owner);

        IReadOnlyList<ReportOwnerRow> All();
    }
}
