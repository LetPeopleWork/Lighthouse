using System.Reflection;
using System.Text;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Bug #5567 - the anchor seam guard (RCA section 8-T2), hard-fail since step 03-01.
    ///
    /// Three source-text rules and one type rule:
    /// <list type="number">
    /// <item>no <c>DateOnly.FromDateTime(DateTime.</c>, <c>DateTime.Today</c> or <c>UtcNow.Date</c>
    /// anywhere in the production project outside the clock adapter;</item>
    /// <item>each decision-4 tracker cutoff still exists and still states why it stays UTC;</item>
    /// <item>no persisted snapshot entity keys its day with an instant.</item>
    /// </list>
    ///
    /// A plain source scanner, modelled on <see cref="ExpandOnlyMigrationGuard"/> and deliberately
    /// NOT ArchUnitNET: <c>DateTime.UtcNow</c> is a property access on a type every class already
    /// depends on, so no dependency rule can express it. The type rule is reflection over
    /// <see cref="LighthouseAppContext"/>'s <c>DbSet</c>s, because it is about persisted shape.
    ///
    /// Root cause D was not "someone wrote the wrong expression", it was "nothing could tell them
    /// they had". There is no warn-list any more: the migration is complete, so a new anchor is a
    /// failure to fix at the source.
    /// </summary>
    [TestFixture]
    public class CalendarDayAnchorSeamArchUnitTest
    {
        private const string DateOnlyFromDateTime = "DateOnly.FromDateTime(DateTime.";

        private const string DateTimeToday = "DateTime.Today";

        private const string UtcNowDate = "UtcNow.Date";

        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        /// <summary>
        /// The one place allowed to reduce an instant to a calendar day: it IS the seam. Expressed
        /// once, as a constant, so the exemption never spreads into scattered string comparisons.
        /// </summary>
        private const string ClockAdapterRelativePath = "Services/Implementation/LighthouseClock.cs";

        /// <summary>
        /// A guard that silently scans nothing is worse than no guard. The production project held
        /// 510 source files at step 03-01; the floor is slack enough that ordinary growth and
        /// deletion never touch it, and a broken root resolution fails loudly rather than passing
        /// vacuously.
        /// </summary>
        private const int MinimumProductionFilesScanned = 400;

        private const string DecisionFourReasonMarker = "Bug #5567 decision 4";

        /// <summary>
        /// One reason comment may cover a small cluster of cutoff lines - in <c>WorkItemService</c> a
        /// single comment covers both the window end and the window start, three lines apart. Wide
        /// enough for that, far too narrow to pick up an unrelated comment from elsewhere.
        /// </summary>
        private const int MaxLinesBetweenReasonAndCutoff = 10;

        /// <summary>
        /// Pinned so growth of the exemption list is always a deliberate, reviewed edit.
        /// </summary>
        private const int ExemptedTrackerCutoffCount = 4;

        /// <summary>
        /// Four snapshot tables at step 03-01. A floor rather than an equality: a fifth table is not
        /// blocked, it is subjected to the day-key rule below, which is the point.
        /// </summary>
        private const int KnownSnapshotEntityCount = 4;

        /// <summary>
        /// The three spellings of the defect. Scanned independently of one another, so a line such as
        /// <c>DateOnly.FromDateTime(DateTime.UtcNow.Date)</c> reports both of the patterns it carries.
        /// </summary>
        private static readonly string[] AnchorPatterns =
        [
            DateOnlyFromDateTime,
            DateTimeToday,
            UtcNowDate,
        ];

        /// <summary>
        /// Build output, mutation output, worktrees and the vendored GitHub-runner artifact. Without
        /// these the scan double-counts generated copies and stops matching the RCA section 5 inventory.
        /// </summary>
        private static readonly string[] ExcludedPathSegments =
        [
            "/obj/",
            "/bin/",
            "/StrykerOutput",
            "/.claude/worktrees/",
            "/tools/codesign/actions-runner/_work/",
        ];

        /// <summary>
        /// Decision 4: a tracker's history window is an instant offset, not a calendar day, so these
        /// four lines stay UTC. They spell it <c>DateTime.UtcNow[.AddDays(...)]</c>, which none of the
        /// three scanned patterns match, so the scanner never sees them - which is exactly why they
        /// are listed here. The rule below requires each one to still exist AND to still carry its
        /// stated reason, so the list cannot rot into a permanent unexplained allowlist.
        /// </summary>
        private static readonly TrackerHistoryCutoff[] TrackerHistoryCutoffs =
        [
            new(
                "Services/Implementation/WorkItems/WorkItemService.cs",
                "var endDate = DateTime.UtcNow;",
                "percentile history window end"),
            new(
                "Services/Implementation/WorkItems/WorkItemService.cs",
                "var startDate = DateTime.UtcNow.AddDays(",
                "percentile history window start"),
            new(
                "Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs",
                "var cutoffDate = DateTime.UtcNow.AddDays(",
                "Azure DevOps history cutoff"),
            new(
                "Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs",
                "var cutoffDate = DateTime.UtcNow.AddDays(",
                "Jira history cutoff"),
        ];

        /// <summary>
        /// The only <c>DateTime</c>-typed columns allowed on a persisted snapshot entity. Both belong
        /// to <see cref="DeliveryMetricSnapshot"/> and both are stale-checked, so the contract-phase
        /// drop of the legacy column forces its entry out of this list in the same commit.
        /// </summary>
        private static readonly LegacyInstantColumn[] AllowedInstantTypedSnapshotColumns =
        [
            new(
                nameof(DeliveryMetricSnapshot),
                nameof(DeliveryMetricSnapshot.RecordedAt),
                "Expand-only legacy column (step 02-02). Written at the day key's midnight so a rollback "
                + "still reads correct data; superseded as the day key by RecordedDay. Remove this entry "
                + "when the contract-phase migration drops the column."),
            new(
                nameof(DeliveryMetricSnapshot),
                nameof(DeliveryMetricSnapshot.TargetDateAtSnapshot),
                "Payload, not a day key: a copy of the delivery's target date as it stood on the recorded "
                + "day. Its type follows the Delivery contract, and the row is keyed by RecordedDay."),
        ];

        private sealed record AnchorFinding(string RelativePath, int Line, string Anchors);

        private sealed record ScanResult(List<AnchorFinding> Findings, int FilesScanned);

        private sealed record TrackerHistoryCutoff(string RelativePath, string Expression, string Reason);

        private sealed record LegacyInstantColumn(string EntityName, string PropertyName, string Reason);

        [Test]
        public void CalendarDayAnchors_OutsideTheClockAdapter_AreAbsentFromProduction()
        {
            var scan = ScanProductionSources();

            TestContext.Out.WriteLine(
                $"Bug #5567 anchor scan: {scan.FilesScanned} production source files scanned, "
                + $"{scan.Findings.Count} calendar-day anchor(s) found.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    scan.FilesScanned,
                    Is.GreaterThanOrEqualTo(MinimumProductionFilesScanned),
                    "Bug #5567: the scan covered almost nothing, so its silence means nothing. Either the "
                    + "production root moved or the exclusion list swallowed the project - fix the scanner, "
                    + "do not lower this floor to match.");

                Assert.That(
                    Describe(scan.Findings),
                    Is.Empty,
                    "Bug #5567: a calendar-day anchor was added. Take the day from ILighthouseClock "
                    + "(clock.Today / clock.TodayAsUtcMidnight / clock.ToInstanceDay) instead - DateTime.Today "
                    + "and DateTime.UtcNow.Date anchor on the host zone and on UTC respectively, which is the "
                    + "whole defect. There is no warn-list to add it to: every site in the RCA section 5 "
                    + "inventory is migrated and this guard only ever reads zero.");
            }
        }

        [Test]
        public void TrackerHistoryCutoffs_StayUtcAndStillSayWhy()
        {
            TestContext.Out.WriteLine(
                $"Bug #5567 decision-4 exemptions checked: {TrackerHistoryCutoffs.Length}.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    TrackerHistoryCutoffs,
                    Has.Length.EqualTo(ExemptedTrackerCutoffCount),
                    "Bug #5567 decision 4 exempted exactly four tracker history cutoffs. Growing this list "
                    + "declares a new site instant-valued - that is a decision, not an edit.");

                Assert.That(
                    TrackerCutoffViolations(TrackerHistoryCutoffs, ProductionSourceOf),
                    Is.Empty,
                    "Bug #5567 decision 4: an exempted tracker cutoff either no longer exists or no longer "
                    + "states why it stays UTC. Both rot the exemption into a permanent unexplained "
                    + "allowlist, which is the confusion decision 4 exists to prevent. Restore the reason "
                    + "comment at the site, or remove the entry if the site was migrated.");
            }
        }

        /// <summary>
        /// The fourth rule, and the only one about type rather than text. Root cause C was "the
        /// recorded day was never modelled": a type rule closes that more completely than any source
        /// rule can, because it catches a FIFTH snapshot table on the day it is added rather than on
        /// the day someone writes UtcNow.Date next to it.
        ///
        /// It is deliberately silent about property NAMES - the four tables do not agree on one
        /// (three say RecordedAt, DeliveryMetricSnapshot says RecordedDay and still carries a legacy
        /// DateTime RecordedAt from the expand-only migration). A name-based rule would have to
        /// choose between failing on that legacy column and being trivially evaded; a type-based rule
        /// needs neither.
        /// </summary>
        [Test]
        public void PersistedSnapshots_KeyTheirDayWithACalendarDay_NeverAnInstant()
        {
            var snapshots = PersistedSnapshotEntities();

            TestContext.Out.WriteLine(
                $"Bug #5567 snapshot day-key rule: {snapshots.Length} persisted snapshot entities checked "
                + $"({string.Join(", ", snapshots.Select(entity => entity.Name))}).");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    snapshots,
                    Has.Length.GreaterThanOrEqualTo(KnownSnapshotEntityCount),
                    "Bug #5567: fewer snapshot entities were discovered than exist, so this rule is not "
                    + "looking at the model. Fix the discovery, do not lower the floor.");

                Assert.That(
                    SnapshotDayKeyViolations(snapshots, AllowedInstantTypedSnapshotColumns),
                    Is.Empty,
                    "Bug #5567: a persisted snapshot must key its day as a DateOnly. A DateTime day key is "
                    + "an instant, which the global UtcDateTimeConverter shifts on write and on query "
                    + "parameters (R1), and which forces every writer to pick an anchor zone of its own "
                    + "(root cause C). Add the DateOnly day key; do not add an allowance.");

                Assert.That(
                    StaleInstantColumnAllowances(snapshots, AllowedInstantTypedSnapshotColumns),
                    Is.Empty,
                    "Bug #5567: an allowed instant-typed snapshot column no longer exists. Remove its entry "
                    + "in the same commit, otherwise the allowance outlives the column it explains.");
            }
        }

        [Test]
        public void Scanner_AnchorAnywhereInProduction_IsReported()
        {
            var findings = FindAnchors("API/BrandNewController.cs", "var today = DateTime.UtcNow.Date;");

            var described = Describe(findings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(described, Has.Count.EqualTo(1));
                Assert.That(described[0], Does.Contain("API/BrandNewController.cs"));
            }
        }

        /// <summary>
        /// A guard that punishes documentation gets suppressed rather than obeyed. During step 01-02
        /// the repo-wide count came back 50 instead of 49 because the XML doc on ILighthouseClock
        /// QUOTED DateTime.UtcNow.Date while explaining the root cause. Comments and string literals
        /// are stripped before matching so the next person to explain this bug in a comment does not
        /// re-break the guard.
        /// </summary>
        [Test]
        public void Scanner_AnchorInACommentOrStringLiteral_IsNotReported()
        {
            const string source = """
                // Root cause B: DateTime.UtcNow.Date anchors on UTC.
                /* The other spelling, DateTime.Today, anchors on the host zone. */
                /// <summary>Replaces DateOnly.FromDateTime(DateTime.Today).</summary>
                var message = "DateTime.UtcNow.Date is the defect";
                var verbatim = @"DateTime.Today";
                var interpolated = $"anchored on {clock.Today} not DateTime.Today";
                var real = DateTime.UtcNow.Date;
                """;

            var findings = FindAnchors("API/Sample.cs", source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].Line, Is.EqualTo(7));
                Assert.That(findings[0].Anchors, Is.EqualTo(UtcNowDate));
            }
        }

        /// <summary>
        /// A hole in an interpolated string is code, not prose - blanking it would let an anchor hide
        /// inside a log message.
        /// </summary>
        [Test]
        public void Scanner_AnchorInsideAnInterpolationHole_IsReported()
        {
            var findings = FindAnchors("API/Sample.cs", "logger.LogInformation($\"day {DateTime.Today}\");");

            Assert.That(findings, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// A line may carry more than one spelling; the finding records all of them, in pattern order.
        /// </summary>
        [Test]
        public void Scanner_LineWithSeveralSpellings_RecordsEachOfThem()
        {
            var findings = FindAnchors("API/Sample.cs", "var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].Anchors, Is.EqualTo($"{DateOnlyFromDateTime} + {UtcNowDate}"));
            }
        }

        [Test]
        public void ReasonCheck_CutoffWithoutAReasonNearby_IsReported()
        {
            TrackerHistoryCutoff[] cutoffs = [new("Connectors/Silent.cs", "var cutoffDate = DateTime.UtcNow.AddDays(", "silent")];
            const string source = """
                // Bug #5567 decision 4: this reason is too far away to be about the cutoff below.
                var a = 1;
                var b = 2;
                var c = 3;
                var d = 4;
                var e = 5;
                var f = 6;
                var g = 7;
                var h = 8;
                var i = 9;
                var j = 10;
                var cutoffDate = DateTime.UtcNow.AddDays(-cutOffDays);
                """;

            var violations = TrackerCutoffViolations(cutoffs, _ => source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(violations, Has.Count.EqualTo(1));
                Assert.That(violations[0], Does.Contain("states no reason"));
            }
        }

        [Test]
        public void ReasonCheck_CutoffCarryingItsReason_IsAccepted()
        {
            TrackerHistoryCutoff[] cutoffs = [new("Connectors/Explained.cs", "var cutoffDate = DateTime.UtcNow.AddDays(", "explained")];
            const string source = """
                // Bug #5567 decision 4: stays UTC. A tracker's history window is an instant offset.
                var cutoffDate = DateTime.UtcNow.AddDays(-cutOffDays);
                """;

            Assert.That(TrackerCutoffViolations(cutoffs, _ => source), Is.Empty);
        }

        [Test]
        public void ReasonCheck_ExemptedCutoffThatNoLongerExists_IsReportedAsStale()
        {
            TrackerHistoryCutoff[] cutoffs = [new("Connectors/Migrated.cs", "var cutoffDate = DateTime.UtcNow.AddDays(", "migrated")];

            var violations = TrackerCutoffViolations(cutoffs, _ => "var cutoffDate = clock.Today.AddDays(-cutOffDays);");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(violations, Has.Count.EqualTo(1));
                Assert.That(violations[0], Does.Contain("no longer exists"));
            }
        }

        [Test]
        public void SnapshotRule_EntityWhoseDayKeyIsAnInstant_IsReported()
        {
            Type[] entities = [typeof(InstantKeyedSnapshotProbe)];

            var violations = SnapshotDayKeyViolations(entities, AllowedInstantTypedSnapshotColumns);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(violations, Has.Count.EqualTo(2));
                Assert.That(violations[0], Does.Contain("no DateOnly"));
                Assert.That(violations[1], Does.Contain("RecordedAt"));
            }
        }

        [Test]
        public void SnapshotRule_EntityKeyedByACalendarDay_IsAccepted()
        {
            Type[] entities = [typeof(DayKeyedSnapshotProbe)];

            Assert.That(SnapshotDayKeyViolations(entities, AllowedInstantTypedSnapshotColumns), Is.Empty);
        }

        [Test]
        public void SnapshotRule_AllowanceForAColumnThatNoLongerExists_IsReportedAsStale()
        {
            Type[] entities = [typeof(DayKeyedSnapshotProbe)];
            LegacyInstantColumn[] allowances = [new(nameof(DayKeyedSnapshotProbe), "DroppedInTheContractPhase", "gone")];

            var stale = StaleInstantColumnAllowances(entities, allowances);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stale, Has.Count.EqualTo(1));
                Assert.That(stale[0], Does.Contain("DroppedInTheContractPhase"));
            }
        }

        private static ScanResult ScanProductionSources()
        {
            var productionRoot = Path.Combine(RepositoryRoot(), ProductionProjectDirectory);
            var findings = new List<AnchorFinding>();
            var filesScanned = 0;

            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(productionRoot, file).Replace('\\', '/');

                if (IsExcluded(relativePath) || string.Equals(relativePath, ClockAdapterRelativePath, StringComparison.Ordinal))
                {
                    continue;
                }

                filesScanned++;
                findings.AddRange(FindAnchors(relativePath, File.ReadAllText(file)));
            }

            return new ScanResult(findings, filesScanned);
        }

        private static bool IsExcluded(string relativePath)
        {
            var probe = "/" + relativePath;

            return ExcludedPathSegments.Any(segment => probe.Contains(segment, StringComparison.Ordinal));
        }

        private static List<AnchorFinding> FindAnchors(string relativePath, string source)
        {
            var lines = StripCommentsAndStringLiterals(source).Split('\n');
            var findings = new List<AnchorFinding>();

            for (var offset = 0; offset < lines.Length; offset++)
            {
                var line = lines[offset];
                var anchors = AnchorPatterns
                    .Where(pattern => line.Contains(pattern, StringComparison.Ordinal))
                    .ToList();

                if (anchors.Count > 0)
                {
                    findings.Add(new AnchorFinding(relativePath, offset + 1, string.Join(" + ", anchors)));
                }
            }

            return findings;
        }

        private static List<string> Describe(List<AnchorFinding> findings)
        {
            return findings
                .Select(finding => $"{finding.RelativePath}:{finding.Line} [{finding.Anchors}]")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();
        }

        private static string? ProductionSourceOf(string relativePath)
        {
            var file = Path.Combine(RepositoryRoot(), ProductionProjectDirectory, relativePath);

            return File.Exists(file) ? File.ReadAllText(file) : null;
        }

        private static List<string> TrackerCutoffViolations(TrackerHistoryCutoff[] cutoffs, Func<string, string?> sourceOf)
        {
            var violations = new List<string>();

            foreach (var cutoff in cutoffs)
            {
                var source = sourceOf(cutoff.RelativePath);

                if (source is null)
                {
                    violations.Add($"{cutoff.RelativePath} ({cutoff.Reason}): the file no longer exists.");
                    continue;
                }

                var lines = source.Split('\n');
                var cutoffLine = IndexOfCodeLineContaining(lines, cutoff.Expression);

                if (cutoffLine < 0)
                {
                    violations.Add($"{cutoff.RelativePath} ({cutoff.Reason}): '{cutoff.Expression}' no longer exists.");
                    continue;
                }

                if (!HasReasonWithinReach(lines, cutoffLine))
                {
                    violations.Add(
                        $"{cutoff.RelativePath}:{cutoffLine + 1} ({cutoff.Reason}): the exempted cutoff states no reason - "
                        + $"no '{DecisionFourReasonMarker}' comment within {MaxLinesBetweenReasonAndCutoff} lines above it.");
                }
            }

            return violations;
        }

        private static int IndexOfCodeLineContaining(string[] lines, string expression)
        {
            for (var offset = 0; offset < lines.Length; offset++)
            {
                var trimmed = lines[offset].TrimStart();

                if (!trimmed.StartsWith("//", StringComparison.Ordinal)
                    && trimmed.Contains(expression, StringComparison.Ordinal))
                {
                    return offset;
                }
            }

            return -1;
        }

        private static bool HasReasonWithinReach(string[] lines, int cutoffLine)
        {
            var first = Math.Max(0, cutoffLine - MaxLinesBetweenReasonAndCutoff);

            for (var offset = first; offset < cutoffLine; offset++)
            {
                var trimmed = lines[offset].TrimStart();

                if (trimmed.StartsWith("//", StringComparison.Ordinal)
                    && trimmed.Contains(DecisionFourReasonMarker, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type[] PersistedSnapshotEntities()
        {
            return typeof(LighthouseAppContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.PropertyType.IsGenericType
                    && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(property => property.PropertyType.GetGenericArguments()[0])
                .Where(entity => entity.Name.EndsWith("Snapshot", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(entity => entity.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static List<string> SnapshotDayKeyViolations(Type[] snapshots, LegacyInstantColumn[] allowances)
        {
            var violations = new List<string>();

            foreach (var snapshot in snapshots)
            {
                var properties = PersistedPropertiesOf(snapshot);

                if (!properties.Any(property => UnderlyingTypeOf(property) == typeof(DateOnly)))
                {
                    violations.Add($"{snapshot.Name}: no DateOnly property, so it has no calendar-day key.");
                }

                violations.AddRange(properties
                    .Where(property => UnderlyingTypeOf(property) == typeof(DateTime))
                    .Where(property => !IsAllowed(allowances, snapshot.Name, property.Name))
                    .Select(property => $"{snapshot.Name}.{property.Name}: an instant on a snapshot, with no recorded reason."));
            }

            return violations;
        }

        private static List<string> StaleInstantColumnAllowances(Type[] snapshots, LegacyInstantColumn[] allowances)
        {
            var present = snapshots.ToDictionary(snapshot => snapshot.Name, PersistedPropertiesOf, StringComparer.Ordinal);

            return allowances
                .Where(allowance => !present.TryGetValue(allowance.EntityName, out var properties)
                    || properties.TrueForAll(property => !string.Equals(property.Name, allowance.PropertyName, StringComparison.Ordinal)))
                .Select(allowance => $"{allowance.EntityName}.{allowance.PropertyName}: allowed, but no such property exists any more.")
                .ToList();
        }

        private static List<PropertyInfo> PersistedPropertiesOf(Type snapshot)
        {
            return [.. snapshot.GetProperties(BindingFlags.Public | BindingFlags.Instance)];
        }

        private static Type UnderlyingTypeOf(PropertyInfo property)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        }

        private static bool IsAllowed(LegacyInstantColumn[] allowances, string entityName, string propertyName)
        {
            return allowances.Any(allowance =>
                string.Equals(allowance.EntityName, entityName, StringComparison.Ordinal)
                && string.Equals(allowance.PropertyName, propertyName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Blanks every comment and string literal while preserving line breaks and offsets, so a
        /// match is always real code on a real line. Interpolation holes are kept - they are code.
        /// </summary>
        private static string StripCommentsAndStringLiterals(string source)
        {
            var stripped = new StringBuilder(source.Length);
            var index = 0;

            while (index < source.Length)
            {
                if (Matches(source, index, "//"))
                {
                    index = BlankLineComment(source, stripped, index);
                    continue;
                }

                if (Matches(source, index, "/*"))
                {
                    index = BlankBlockComment(source, stripped, index);
                    continue;
                }

                var prefixLength = LiteralPrefixLength(source, index);
                if (prefixLength >= 0)
                {
                    index = BlankLiteral(source, stripped, index, prefixLength);
                    continue;
                }

                stripped.Append(source[index]);
                index++;
            }

            return stripped.ToString();
        }

        private static int BlankLineComment(string source, StringBuilder stripped, int index)
        {
            while (index < source.Length && source[index] != '\n')
            {
                stripped.Append(' ');
                index++;
            }

            return index;
        }

        private static int BlankBlockComment(string source, StringBuilder stripped, int index)
        {
            stripped.Append("  ");
            index += 2;

            while (index < source.Length && !Matches(source, index, "*/"))
            {
                stripped.Append(source[index] == '\n' ? '\n' : ' ');
                index++;
            }

            if (index < source.Length)
            {
                stripped.Append("  ");
                index += 2;
            }

            return index;
        }

        /// <summary>
        /// The number of <c>$</c> / <c>@</c> characters preceding a literal that starts at
        /// <paramref name="index"/> (zero for a bare literal), or -1 when no literal starts here.
        /// </summary>
        private static int LiteralPrefixLength(string source, int index)
        {
            var probe = index;
            while (probe < source.Length && (source[probe] == '$' || source[probe] == '@'))
            {
                probe++;
            }

            if (probe >= source.Length || (source[probe] != '"' && source[probe] != '\''))
            {
                return -1;
            }

            return probe - index;
        }

        private static int BlankLiteral(string source, StringBuilder stripped, int index, int prefixLength)
        {
            var isVerbatim = source.IndexOf('@', index, prefixLength) >= 0;
            var isInterpolated = source.IndexOf('$', index, prefixLength) >= 0;

            stripped.Append(' ', prefixLength + 1);
            var quote = source[index + prefixLength];
            index += prefixLength + 1;

            if (quote == '"' && !isVerbatim && Matches(source, index, "\"\""))
            {
                return BlankRawString(source, stripped, index);
            }

            var braceDepth = 0;

            while (index < source.Length)
            {
                var current = source[index];

                if (braceDepth == 0 && !isVerbatim && current == '\\' && index + 1 < source.Length)
                {
                    stripped.Append("  ");
                    index += 2;
                    continue;
                }

                if (braceDepth == 0 && current == quote)
                {
                    if (isVerbatim && Matches(source, index, "\"\""))
                    {
                        stripped.Append("  ");
                        index += 2;
                        continue;
                    }

                    stripped.Append(' ');
                    return index + 1;
                }

                if (isInterpolated && current == '{')
                {
                    if (braceDepth == 0 && Matches(source, index, "{{"))
                    {
                        stripped.Append("  ");
                        index += 2;
                        continue;
                    }

                    braceDepth++;
                    stripped.Append(current);
                    index++;
                    continue;
                }

                if (isInterpolated && current == '}' && braceDepth > 0)
                {
                    braceDepth--;
                    stripped.Append(current);
                    index++;
                    continue;
                }

                stripped.Append(BlankedOutside(current, braceDepth));
                index++;
            }

            return index;
        }

        private static char BlankedOutside(char current, int braceDepth)
        {
            if (braceDepth > 0 || current == '\n')
            {
                return current;
            }

            return ' ';
        }

        private static int BlankRawString(string source, StringBuilder stripped, int index)
        {
            stripped.Append("  ");
            index += 2;

            while (index < source.Length && !Matches(source, index, "\"\"\""))
            {
                stripped.Append(source[index] == '\n' ? '\n' : ' ');
                index++;
            }

            if (index < source.Length)
            {
                stripped.Append("   ");
                index += 3;
            }

            return index;
        }

        private static bool Matches(string source, int index, string token)
        {
            return index >= 0
                && index + token.Length <= source.Length
                && string.CompareOrdinal(source, index, token, 0, token.Length) == 0;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the calendar-day anchor scan.");
            return directory!.FullName;
        }

        /// <summary>A fifth snapshot table as it would look if it repeated root cause C.</summary>
        private sealed class InstantKeyedSnapshotProbe
        {
            public int Id { get; set; }

            public DateTime RecordedAt { get; set; }
        }

        /// <summary>The shape all four shipped snapshot tables have.</summary>
        private sealed class DayKeyedSnapshotProbe
        {
            public int Id { get; set; }

            public DateOnly RecordedDay { get; set; }
        }
    }
}
