namespace Lighthouse.Backend.Models.Reports
{
    /// <summary>The codes a refused report request names. Clients match on them, so each one, once shipped, keeps its spelling.</summary>
    public static class ReportRefusalCodes
    {
        public const string NameMissing = "name-missing";

        public const string NameTooLong = "name-too-long";

        public const string TemplateNotSupported = "template-not-supported";

        public const string ThenTooShort = "then-too-short";

        public const string ThenBeforeCutoff = "then-before-cutoff";

        public const string NowTooShort = "now-too-short";

        public const string NowBeforeCutoff = "now-before-cutoff";
    }
}
