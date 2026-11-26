using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Modules.Reports.Domain
{
    public abstract record ReportNoteCaller
    {
        private protected ReportNoteCaller()
        {
        }

        /// <summary>Whether this caller may change or delete the note.</summary>
        public abstract bool MayChange(ReportNote note);

        public sealed record Anonymous : ReportNoteCaller
        {
            public override bool MayChange(ReportNote note) => note.MayBeChangedBy(null);
        }

        public sealed record Signed(int ProfileId, string? DisplayName) : ReportNoteCaller
        {
            public override bool MayChange(ReportNote note) => note.MayBeChangedBy(ProfileId);
        }

        /// <summary>Signed in, but the instance cannot name who: such a caller may change no note.</summary>
        public sealed record Unresolved : ReportNoteCaller
        {
            public override bool MayChange(ReportNote note) => false;
        }
    }
}
