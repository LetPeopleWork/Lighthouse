namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Whether this caller may move this Feature, decided once on the server (ADR-136). The client renders
    /// from it and never re-derives it: the natural client-side conjunction over the Portfolios shown on a
    /// row fails open twice — the row is already read-filtered, and "all of them" is vacuously true for a
    /// Feature that belongs to none.
    /// </summary>
    public sealed record FeatureMoveVerdict(bool CanMove, string? BlockReason, IReadOnlyList<Portfolio> BlockingPortfolios)
    {
        /// <summary>Movable by nobody — <c>Portfolios.Any()</c> is what stops the vacuous grant (ADR-132 §4).</summary>
        public const string NotInAnyPortfolio = "orphan";

        /// <summary>The caller does not run every Portfolio this Feature belongs to.</summary>
        public const string NoWriteOnEveryPortfolio = "no-write";

        public static FeatureMoveVerdict Allowed { get; } = new(true, null, []);
    }
}
