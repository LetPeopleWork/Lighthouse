using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// AC-5.4 / DDD-8. The invariant this file exists for is not "the fingerprint is correct" — it is
    /// "nobody can add a fetch-shaping property without deciding, in writing, what it costs".
    ///
    /// Delta skips the whole of an unchanged record: it is not re-downloaded, not re-mapped, not
    /// re-derived. So the set the fingerprint has to cover is <b>what the query asks the tracker for
    /// UNION how the answer is read into the stored record</b>. AC-5.4 as briefed says "reachable from
    /// <c>PrepareQuery</c> and the connector call sites", which is the first half only, and the first
    /// half misses state mappings, the connection's field definitions and the portfolio's owner/size
    /// field references — each of which changes every stored record while leaving the request identical.
    ///
    /// There are TWO consumers of the one property set (A2), and they see different things:
    /// <list type="bullet">
    /// <item><b>The fingerprint</b> is computed at sync time from the entity and the connection it points
    /// at, so it sees everything below.</item>
    /// <item><b>The save-time decision</b> (<c>WorkItemRelatedSettingsChanged</c>) compares an entity
    /// against an incoming settings DTO, so it can only see properties the DTO carries. The
    /// connection-scoped rows are marked accordingly.</item>
    /// </list>
    ///
    /// ArchUnitNET constrains types and dependencies, not property membership, which is why this is a
    /// reflection assertion rather than an ArchUnit rule (DDD-8).
    ///
    /// Both tests ship [Ignore]d: <see cref="FetchFingerprint"/> does not exist yet. DELIVER un-ignores
    /// them together with the type.
    /// </summary>
    [TestFixture]
    [Category("epic-5687-faster-updates")]
    [Category("slice-05")]
    public class FetchShapingPropertyGuardTest
    {
        private const string Pending = "DISTILL scaffold - FetchFingerprint does not exist yet.";

        private const string Because =
            "Epic #5687 AC-5.4. A fetch-shaping property that is neither registered nor excluded makes the "
            + "fingerprint drift silently, and a stale fingerprint means delta serves a stale result set "
            + "with every test green - wrong numbers, no error. If you are reading this because the test "
            + "went red: decide what the new property costs, then either add it to the fingerprint or add "
            + "it to the exclusion list below WITH the reason. Do not delete the row.";

        /// <summary>
        /// The surface the guard walks: every public settable property an operator can change on a query
        /// owner, plus the connection it points at - which the query owner only references, but whose
        /// field definitions and system decide what is asked for and what is stored.
        /// </summary>
        private static readonly Type[] TheSurfaceAnOperatorCanChange =
        [
            typeof(WorkTrackingSystemOptionsOwner),
            typeof(Team),
            typeof(Portfolio),
            typeof(WorkTrackingSystemConnection),
        ];

        /// <summary>
        /// What a change to the property costs the next cycle. The point of the enum is that "nothing" is
        /// a decision somebody made, not a property nobody looked at.
        /// </summary>
        private enum Cost
        {
            /// <summary>In the fingerprint: a change makes the next cycle download the whole query.</summary>
            AFullDownload,

            /// <summary>In the fingerprint AND a save-time purge: the same reference id now means a different item.</summary>
            AFullDownloadAndAFreshStart,

            /// <summary>Excluded: a change costs nothing remote, for the recorded reason.</summary>
            Nothing,
        }

        private sealed record Decision(string Property, Cost Cost, string Why);

        /// <summary>
        /// One row per property of the four types above. The three <c>AFullDownload…</c> groups are the
        /// widened fingerprint set; everything else is an exclusion with its reason attached, which is
        /// the artefact AC-5.4 actually asks for.
        /// </summary>
        private static readonly Decision[] WhatEachPropertyCosts =
        [
            // --- What the query asks the tracker for (reachable from PrepareQuery) ---
            new("DataRetrievalValue", Cost.AFullDownload, "The query text itself."),
            new("WorkItemTypes", Cost.AFullDownload, "Narrows the query by type."),
            new("DoneItemsCutoffDays", Cost.AFullDownload,
                "Part of the query's resolved-cutoff clause. It shapes the result set today and triggers no purge - the live gap A2 names."),
            new("ToDoStates", Cost.AFullDownload,
                "Half of the query's state clause, and half of the state category stored against every record. AllStates is a UNION, so moving a state between columns leaves the query identical and re-categorises every record in it - which is why the three columns are registered separately rather than as AllStates."),
            new("DoingStates", Cost.AFullDownload, "See ToDoStates."),
            new("DoneStates", Cost.AFullDownload, "See ToDoStates."),

            // --- How the answer is read into the stored record (NOT reachable from PrepareQuery) ---
            new("StateMappings", Cost.AFullDownload,
                "MapRawStateToMappedName and MapStateToStateCategory both consult it, so it decides the State and StateCategory stored against every record. Moving a raw state from one mapping to another leaves AllStates identical and changes both."),
            new("ParentOverrideAdditionalFieldDefinitionId", Cost.AFullDownload,
                "Decides which field the connectors read ParentReferenceId out of, so it decides the whole parent hierarchy."),
            new(nameof(Portfolio.FeatureOwnerAdditionalFieldDefinitionId), Cost.AFullDownload,
                "Read at sync time and stored as Feature.OwningTeam. Under delta an unchanged Feature is never re-derived, so the old owner survives the edit."),
            new(nameof(Portfolio.SizeEstimateAdditionalFieldDefinitionId), Cost.AFullDownload,
                "Read at sync time and stored as Feature.EstimatedSize. Same mechanism as FeatureOwner."),
            new(nameof(WorkTrackingSystemConnection.AdditionalFieldDefinitions), Cost.AFullDownload,
                "CONNECTION-SCOPED, so the save-time consumer cannot see it: a settings DTO carries no field definitions. Decides what is requested from the tracker AND what is stored in AdditionalFieldValues, for every entity on the connection."),
            new(nameof(WorkTrackingSystemConnection.WorkTrackingSystem), Cost.AFullDownload,
                "CONNECTION-SCOPED. Decides which connector answers the query at all, so it decides the shape of everything that comes back."),

            // --- The one edit that also discards what is stored ---
            new("WorkTrackingSystemConnectionId", Cost.AFullDownloadAndAFreshStart,
                "The same reference id on a different tracker is a DIFFERENT item, and SyncWorkItem updates the stored copy in place - so without a purge the old system's transition history silently becomes the new system's. This is the only edit removed = stored - fetched cannot reconcile, and therefore the only one that earns a purge."),

            // --- Excluded: identity and infrastructure ---
            new("Id", Cost.Nothing, "Identity, not a setting."),
            new("ConcurrencyToken", Cost.Nothing, "Optimistic-concurrency infrastructure."),
            new("Name", Cost.Nothing, "A label. It reaches the tracker only as the subject of a log line."),
            new("UpdateTime", Cost.Nothing, "Sync-owned, written by the cycle itself. In the fingerprint it would invalidate on every cycle."),
            new("FetchFingerprint", Cost.Nothing, "The fingerprint cannot hash itself."),
            new("WorkTrackingSystemConnection", Cost.Nothing, "The navigation property behind WorkTrackingSystemConnectionId; its own members are registered above."),
            new(nameof(WorkTrackingSystemConnection.AuthenticationMethodKey), Cost.Nothing,
                "CONNECTION-SCOPED credentials. Rotating a token changes who asks, never what is asked - and hashing it would make every credential rotation cost a full re-download of every entity on the connection."),
            new(nameof(WorkTrackingSystemConnection.Options), Cost.Nothing,
                "CONNECTION-SCOPED. Mostly credentials, by the same argument as AuthenticationMethodKey. NOTE: it also carries the instance URL, and re-pointing a connection at a different instance IS a fetch-shaping change - recorded as an upstream issue rather than decided here, because the fix is connection identity, not a hash."),
            new(nameof(WorkTrackingSystemConnection.WriteBackMappingDefinitions), Cost.Nothing, "Write-back is an outbound path; it shapes nothing that is fetched."),

            // --- Excluded: derived, or evaluated over the stored set every cycle (ADR-141 / D9 / D10) ---
            new("AllStates", Cost.Nothing, "Derived from the three state columns and the mappings, all of which are registered."),
            new("OpenStates", Cost.Nothing, "Derived, as AllStates."),
            new("StalenessThresholdDays", Cost.Nothing, "Evaluated over the WHOLE stored set every cycle (DDD-4), so a change takes effect on the next cycle without any download."),
            new("BlockedStalenessThresholdDays", Cost.Nothing, "As StalenessThresholdDays."),
            new("BlockedRuleSetJson", Cost.Nothing,
                "AC-5.3 names it free, and it is evaluated over STORED data rather than remote data - so re-deriving it needs no remote call. NOTE: the delta loop currently only visits downloaded items, so a rule edit does not re-open spells for quiet ones. That gap is real and belongs in the ADR-141 derivation pass, not here: paying a full remote download to fix a local derivation is the wrong instrument. Recorded as an upstream issue."),
            new("WaitStates", Cost.Nothing, "Read only on the metrics path, per request."),
            new("CycleTimeDefinitions", Cost.Nothing, "Read only on the metrics path, per request."),
            new("ServiceLevelExpectationProbability", Cost.Nothing, "Read path."),
            new("ServiceLevelExpectationRange", Cost.Nothing, "Read path."),
            new("SystemWIPLimit", Cost.Nothing, "Read path."),
            new("ProcessBehaviourChartBaselineStartDate", Cost.Nothing, "Read path."),
            new("ProcessBehaviourChartBaselineEndDate", Cost.Nothing, "Read path."),
            new("EstimationAdditionalFieldDefinitionId", Cost.Nothing,
                "Read on the metrics path only (BaseMetricsService), and the underlying value is stored via AdditionalFieldValues - which the connection's field definitions already cover."),
            new("EstimationUnit", Cost.Nothing, "As EstimationAdditionalFieldDefinitionId."),
            new("UseNonNumericEstimation", Cost.Nothing, "As EstimationAdditionalFieldDefinitionId."),
            new("EstimationCategoryValues", Cost.Nothing, "As EstimationAdditionalFieldDefinitionId."),

            // --- Excluded: Team-only, all read path or forecast input ---
            new(nameof(Team.FeatureWIP), Cost.Nothing, "Forecast input."),
            new(nameof(Team.AutomaticallyAdjustFeatureWIP), Cost.Nothing, "Forecast input."),
            new(nameof(Team.UseFixedDatesForThroughput), Cost.Nothing, "Throughput window; read path."),
            new(nameof(Team.ThroughputHistory), Cost.Nothing, "Throughput window; read path."),
            new(nameof(Team.ThroughputHistoryStartDate), Cost.Nothing, "Throughput window; read path."),
            new(nameof(Team.ThroughputHistoryEndDate), Cost.Nothing, "Throughput window; read path."),
            new(nameof(Team.ForecastFilterRuleSetJson), Cost.Nothing, "Evaluated over the stored set on the forecast path."),

            // --- Excluded: Portfolio-only derivations (D9 / ADR-141) ---
            new(nameof(Portfolio.DefaultAmountOfWorkItemsPerFeature), Cost.Nothing, "Recomputed every cycle over the stored set."),
            new(nameof(Portfolio.OverrideRealChildCountStates), Cost.Nothing, "Recomputed every cycle over the stored set."),
            new(nameof(Portfolio.UsePercentileToCalculateDefaultAmountOfWorkItems), Cost.Nothing, "Recomputed every cycle."),
            new(nameof(Portfolio.PercentileHistoryInDays), Cost.Nothing, "Recomputed every cycle."),
            new(nameof(Portfolio.DefaultWorkItemPercentile), Cost.Nothing, "Recomputed every cycle."),
            new(nameof(Portfolio.OwningTeamId), Cost.Nothing, "Who owns the portfolio in Lighthouse; never sent to the tracker."),
            new(nameof(Portfolio.OwningTeam), Cost.Nothing, "Navigation property for OwningTeamId."),
        ];

        /// <summary>
        /// Stored collections, not settings: an operator cannot edit them, the sync writes them.
        /// Read-only navigation properties are skipped by the walk itself; these are named so the reason
        /// is on the record rather than implied by an accessor check.
        /// </summary>
        private static readonly string[] WhatTheSyncOwnsRatherThanTheOperator =
        [
            nameof(Team.Portfolios),
            nameof(Team.WorkItems),
            nameof(Portfolio.Features),
            nameof(Portfolio.Teams),
        ];

        [Test]
        [Ignore(Pending)]
        public void EveryPropertyThatShapesAFetchIsEitherInTheFingerprintOrExplicitlyExcluded()
        {
            var undecided = TheOperatorEditableSurface()
                .Where(property => !WhatEachPropertyCosts.Any(decision => decision.Property == property))
                .ToList();

            Assert.That(undecided, Is.Empty,
                "Undecided properties: " + string.Join(", ", undecided) + ". " + Because);
        }

        /// <summary>
        /// The other half of "one property set, two consumers". Without this, the guard protects the new
        /// fingerprint from drift while an older, shorter list sits one directory away in
        /// <c>TeamExtensions</c> - which is the very drift the guard exists to prevent, reintroduced.
        /// </summary>
        [Test]
        [Ignore(Pending)]
        public void TheSaveTimeDecisionAndTheFingerprintReadTheSamePropertySet()
        {
            var inTheFingerprint = WhatEachPropertyCosts
                .Where(decision => decision.Cost != Cost.Nothing)
                .Select(decision => decision.Property)
                .ToHashSet(StringComparer.Ordinal);

            var alsoAFreshStart = WhatEachPropertyCosts
                .Where(decision => decision.Cost == Cost.AFullDownloadAndAFreshStart)
                .Select(decision => decision.Property)
                .ToHashSet(StringComparer.Ordinal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FetchFingerprint.RegisteredProperties.ToHashSet(StringComparer.Ordinal), Is.EquivalentTo(inTheFingerprint),
                    "The fingerprint and this file disagree about what shapes a fetch. One of them is wrong, and the "
                    + "one that is wrong is whichever was edited without reading the other. " + Because);

                Assert.That(FetchFingerprint.PropertiesThatAlsoCostAFreshStart.ToHashSet(StringComparer.Ordinal), Is.EquivalentTo(alsoAFreshStart),
                    "Only a connection change earns a purge, because it is the only edit that makes the same reference id a "
                    + "different item. Widening this set spends transition history to achieve what the next full cycle already "
                    + "achieves; narrowing it merges two trackers' work into one record. " + Because);
            }
        }

        /// <summary>
        /// Every property an operator can set, across the query owners and the connection they point at.
        /// Write-ability is the filter: a computed property has no independent value to hash, and a
        /// sync-owned collection is not something anybody edits.
        /// </summary>
        private static List<string> TheOperatorEditableSurface()
            => [.. TheSurfaceAnOperatorCanChange
                .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(property => property.CanWrite || IsAnEditableCollection(property))
                .Select(property => property.Name)
                .Where(name => !WhatTheSyncOwnsRatherThanTheOperator.Contains(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)];

        /// <summary>
        /// A get-only <c>List&lt;T&gt;</c> on a connection is still an operator-editable setting - the
        /// Connections screen adds to it in place. Excluding every get-only member would silently drop
        /// <c>AdditionalFieldDefinitions</c>, which is the single most consequential row in the table.
        /// </summary>
        private static bool IsAnEditableCollection(PropertyInfo property)
            => property.DeclaringType == typeof(WorkTrackingSystemConnection)
                && property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
    }
}
