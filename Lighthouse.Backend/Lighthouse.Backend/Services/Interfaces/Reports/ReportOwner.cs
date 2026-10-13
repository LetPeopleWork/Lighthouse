namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public enum ReportOwnerKind
    {
        Team,
        Portfolio,
    }

    public sealed record ReportOwner(ReportOwnerKind Kind, int Id)
    {
        public static ReportOwner Team(int teamId) => new(ReportOwnerKind.Team, teamId);

        public static ReportOwner Portfolio(int portfolioId) => new(ReportOwnerKind.Portfolio, portfolioId);
    }
}
