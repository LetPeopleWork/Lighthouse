using System.Text;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Bug #5567 - T2, the anchor-seam source guard (RCA section 8-T2).
    ///
    /// A plain source-text scanner, modelled on <see cref="ExpandOnlyMigrationGuard"/> and
    /// deliberately NOT ArchUnitNET: <c>DateTime.UtcNow</c> is a property access on a type every
    /// class already depends on, so no dependency rule can express it.
    ///
    /// This guard was also the only deterministic proof of branch B: an injected instant never
    /// reaches a statically-read clock, so no runtime test could show that <c>DateTime.Today</c> and
    /// <c>DateTime.UtcNow.Date</c> disagree. Both spellings stood recorded against
    /// <c>API/ForecastController.cs</c> until step 02-04 removed them.
    /// </summary>
    [TestFixture]
    public class CalendarDayAnchorSeamArchUnitTest
    {
        private const int BaselinedSiteCount = 10;

        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        /// <summary>
        /// The one place allowed to reduce an instant to a calendar day: it IS the seam. Expressed
        /// once, as a constant, so the exemption never spreads into scattered string comparisons.
        /// </summary>
        private const string ClockAdapterRelativePath = "Services/Implementation/LighthouseClock.cs";

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

        private sealed record AnchorFinding(string RelativePath, int Line, string Anchors);

        [Test]
        public void CalendarDayAnchors_OutsideTheClockAdapter_AreOnlyTheBaselinedSites()
        {
            var found = ScanProductionSources();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    UnbaselinedSites(found, CalendarDayAnchorBaseline.KnownSites),
                    Is.Empty,
                    "Bug #5567: a new calendar-day anchor was added. Take the day from ILighthouseClock " +
                    "(clock.Today / clock.TodayAsUtcMidnight / clock.ToInstanceDay) instead - DateTime.Today and " +
                    "DateTime.UtcNow.Date anchor on the host zone and on UTC respectively, which is the whole defect. " +
                    "Do NOT extend CalendarDayAnchorBaseline.KnownSites: it only shrinks.");

                Assert.That(
                    StaleBaselineEntries(found, CalendarDayAnchorBaseline.KnownSites),
                    Is.Empty,
                    "Bug #5567: a baselined anchor no longer exists in production - the migration moved it. " +
                    "Remove the stale entry from CalendarDayAnchorBaseline.KnownSites in the same commit, otherwise " +
                    "the warn-list rots into a permanent allowlist and stops meaning anything.");
            }
        }

        /// <summary>
        /// Proves the guard is scanning something rather than passing vacuously, and pins the number
        /// the RCA verified so a shrink is always a deliberate, reviewed edit.
        /// </summary>
        [Test]
        public void Baseline_OnHead_ListsTheVerifiedKnownSites()
        {
            Assert.That(
                CalendarDayAnchorBaseline.KnownSites,
                Has.Length.EqualTo(BaselinedSiteCount),
                "The baseline must match the RCA section 5 inventory (49 sites across 24 files) minus every "
                + "cluster phase 02 has already migrated. Lower this constant by exactly the cluster size "
                + "in the same commit that shrinks the baseline.");
        }

        [Test]
        public void Scanner_AnchorOutsideTheBaseline_IsReported()
        {
            var found = FindAnchors("API/BrandNewController.cs", "var today = DateTime.UtcNow.Date;");

            var unbaselined = UnbaselinedSites(found, CalendarDayAnchorBaseline.KnownSites);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unbaselined, Has.Count.EqualTo(1));
                Assert.That(unbaselined[0], Does.Contain("API/BrandNewController.cs"));
            }
        }

        [Test]
        public void Scanner_BaselinedSiteThatNoLongerExists_IsReportedAsStale()
        {
            CalendarDayAnchorSite[] baseline =
            [
                new(
                    "API/AlreadyMigratedController.cs",
                    CalendarDayAnchorBaseline.UtcNowDate,
                    "moved to clock.Today"),
            ];

            var stale = StaleBaselineEntries(new List<AnchorFinding>(), baseline);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stale, Has.Count.EqualTo(1));
                Assert.That(stale[0], Does.Contain("API/AlreadyMigratedController.cs"));
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
                Assert.That(findings[0].Anchors, Is.EqualTo(CalendarDayAnchorBaseline.UtcNowDate));
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
                Assert.That(findings[0].Anchors, Is.EqualTo(CalendarDayAnchorBaseline.DateOnlyFromUtcNowDate));
            }
        }

        private static List<AnchorFinding> ScanProductionSources()
        {
            var productionRoot = Path.Combine(RepositoryRoot(), ProductionProjectDirectory);
            var findings = new List<AnchorFinding>();

            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(productionRoot, file).Replace('\\', '/');

                if (IsExcluded(relativePath) || string.Equals(relativePath, ClockAdapterRelativePath, StringComparison.Ordinal))
                {
                    continue;
                }

                findings.AddRange(FindAnchors(relativePath, File.ReadAllText(file)));
            }

            return findings;
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
                var anchors = CalendarDayAnchorBaseline.AnchorPatterns
                    .Where(pattern => line.Contains(pattern, StringComparison.Ordinal))
                    .ToList();

                if (anchors.Count > 0)
                {
                    findings.Add(new AnchorFinding(relativePath, offset + 1, string.Join(" + ", anchors)));
                }
            }

            return findings;
        }

        private static List<string> UnbaselinedSites(List<AnchorFinding> found, CalendarDayAnchorSite[] baseline)
        {
            var baselined = CountByKey(baseline.Select(site => KeyOf(site.RelativePath, site.Anchors)));

            return found
                .GroupBy(finding => KeyOf(finding.RelativePath, finding.Anchors))
                .Where(group => group.Count() > CountFor(baselined, group.Key))
                .Select(group =>
                    $"{group.Key}: {group.Count()} found, {CountFor(baselined, group.Key)} baselined " +
                    $"(line(s) {string.Join(", ", group.Select(finding => finding.Line))})")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();
        }

        private static List<string> StaleBaselineEntries(List<AnchorFinding> found, CalendarDayAnchorSite[] baseline)
        {
            var present = CountByKey(found.Select(finding => KeyOf(finding.RelativePath, finding.Anchors)));

            return baseline
                .GroupBy(site => KeyOf(site.RelativePath, site.Anchors))
                .Where(group => group.Count() > CountFor(present, group.Key))
                .Select(group => $"{group.Key}: {group.Count()} baselined, {CountFor(present, group.Key)} still in production")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();
        }

        private static Dictionary<string, int> CountByKey(IEnumerable<string> keys)
        {
            return keys
                .GroupBy(key => key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        }

        private static int CountFor(Dictionary<string, int> counts, string key)
        {
            return counts.TryGetValue(key, out var count) ? count : 0;
        }

        private static string KeyOf(string relativePath, string anchors) => $"{relativePath} [{anchors}]";

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
    }
}
