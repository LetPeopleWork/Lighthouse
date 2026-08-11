using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
    /// <summary>
    /// What the last cycle asked the tracker for, and how it read the answer (Epic #5687, ADR-140).
    ///
    /// One property set, two consumers (A2): this type decides whether the NEXT cycle has to download the
    /// whole query, and the settings-save path decides whether the stored records have to be discarded
    /// first. They must not ship as two lists - an older, shorter copy one directory away is exactly the
    /// drift the guard test exists to prevent.
    ///
    /// The set is "what the query asks for UNION how the answer is read into the stored record", not
    /// "what <c>PrepareQuery</c> is handed". Delta skips an unchanged record's whole derivation, so a
    /// state mapping or a connection field definition is every bit as fetch-shaping as the query text.
    ///
    /// Pure static by DDD-5: a total function of data already in hand, and <c>WorkItemService</c> already
    /// carries twelve constructor dependencies.
    /// </summary>
    public static class FetchFingerprint
    {
        /// <summary>
        /// Every property a change to which makes the next cycle download the whole query.
        /// <c>Lighthouse.Backend.Tests/Architecture/FetchShapingPropertyGuardTest</c> holds the reason for
        /// each one, and for every property deliberately absent (AC-5.4).
        /// </summary>
        public static IReadOnlyCollection<string> RegisteredProperties { get; } = [];

        /// <summary>
        /// The subset that ALSO costs a fresh start at save time. Only a connection change belongs here:
        /// it is the only edit that makes the same reference id a different item, and therefore the only
        /// one <c>removed = stored - fetched</c> cannot reconcile on the next full cycle.
        /// </summary>
        public static IReadOnlyCollection<string> PropertiesThatAlsoCostAFreshStart { get; } = [];

        /// <remarks>
        /// A throw rather than an assertion failure, deliberately: nothing reaches this yet — the guards
        /// are ignored and the scenarios enter at the driving port — so it can only ever surface as an
        /// unreachable-code signal, which is what DT3-3's connectors already do for the same reason.
        /// </remarks>
        public static string For(IWorkItemQueryOwner queryOwner)
            => throw new NotSupportedException($"Not implemented yet - Epic #5687 slice 05 ({queryOwner.Name}).");
    }
}
