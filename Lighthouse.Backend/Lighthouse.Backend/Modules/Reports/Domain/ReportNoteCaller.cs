namespace Lighthouse.Backend.Modules.Reports.Domain
{
    public abstract record ReportNoteCaller
    {
        private protected ReportNoteCaller()
        {
        }

        /// <summary>The profile the caller is signed in as, or none when nobody is.</summary>
        public int? ProfileIdOrNone => (this as Signed)?.ProfileId;

        public sealed record Anonymous : ReportNoteCaller;

        public sealed record Signed(int ProfileId, string? DisplayName) : ReportNoteCaller;

        public sealed record Unresolved : ReportNoteCaller;
    }
}
