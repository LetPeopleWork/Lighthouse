namespace Lighthouse.Backend.Modules.Reports.Domain
{
    public abstract record ReportNoteCaller
    {
        private protected ReportNoteCaller()
        {
        }

        public sealed record Anonymous : ReportNoteCaller;

        public sealed record Signed(int ProfileId, string? DisplayName) : ReportNoteCaller;

        public sealed record Unresolved : ReportNoteCaller;
    }
}
