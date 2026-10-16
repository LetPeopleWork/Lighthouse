using System.Text;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// <c>DateTime.Parse(s, culture, DateTimeStyles.AssumeUniversal)</c> reads a string that names its
    /// own zone ("2024-03-05T10:00:00.000+0100") and returns it converted to the SERVER's local time,
    /// with Kind = Local. Relabelling that result as UTC (SpecifyKind) then stores the instant shifted by
    /// the server's offset, and a test that only compares <c>.Date</c>, or runs on a UTC machine, never
    /// notices. Adding <c>DateTimeStyles.AdjustToUniversal</c> to the same call makes the parse return
    /// the instant in UTC on any host zone.
    ///
    /// A plain textual scan of the production sources, in the style of
    /// <see cref="CalendarDayAnchorSeamArchUnitTest"/>: it fails on any host zone, UTC included, which
    /// a behaviour test of the parse cannot do.
    /// </summary>
    [TestFixture]
    public class ZonedTimestampParsingArchUnitTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string AssumeUniversal = "AssumeUniversal";

        private const string AdjustToUniversal = "AdjustToUniversal";

        private const string DateTimeReceiver = "DateTime.";

        /// <summary>
        /// A guard that silently scans nothing is worse than no guard: a broken root resolution must
        /// fail loudly rather than pass vacuously. Well below the production project's size.
        /// </summary>
        private const int MinimumProductionFilesScanned = 400;

        private static readonly string[] ParseMethods =
        [
            "Parse",
            "TryParse",
            "ParseExact",
            "TryParseExact",
        ];

        private static readonly string[] ExcludedPathSegments =
        [
            "/obj/",
            "/bin/",
            "/StrykerOutput",
            "/.claude/worktrees/",
            "/tools/codesign/actions-runner/_work/",
        ];

        private sealed record ParseFinding(string RelativePath, int Line, string Call);

        private sealed record ScanResult(List<ParseFinding> Findings, int FilesScanned);

        [Test]
        public void No_production_code_parses_a_date_with_AssumeUniversal_without_AdjustToUniversal()
        {
            var scan = ScanProductionSources();

            TestContext.Out.WriteLine(
                $"Zoned timestamp parse scan: {scan.FilesScanned} production source files scanned, "
                + $"{scan.Findings.Count} DateTime parse(s) relabelling server time as UTC found.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    scan.FilesScanned,
                    Is.GreaterThanOrEqualTo(MinimumProductionFilesScanned),
                    "The scan covered almost nothing, so its silence means nothing. Either the production "
                    + "root moved or the exclusion list swallowed the project - fix the scanner, do not "
                    + "lower this floor to match.");

                Assert.That(
                    Describe(scan.Findings),
                    Is.Empty,
                    "A DateTime parse passes DateTimeStyles.AssumeUniversal without AdjustToUniversal. For a "
                    + "string that carries its own offset, that returns the instant in the server's local "
                    + "time, and marking it UTC afterwards stores it shifted by the server's offset. Use "
                    + "DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal "
                    + "| DateTimeStyles.AdjustToUniversal, out var at) and take at.UtcDateTime, or add "
                    + "AdjustToUniversal to the same call.");
            }
        }

        [Test]
        public void Scanner_ParseThatRelabelsServerTimeAsUtc_IsReported()
        {
            const string source = """
                var a = 1;
                var d = DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                """;

            var findings = FindRelabellingParses("Factories/OldIssueFactory.cs", source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(Describe(findings)[0], Does.StartWith("Factories/OldIssueFactory.cs:2 "));
            }
        }

        [Test]
        public void Scanner_EveryParseSpelling_IsReported()
        {
            const string source = """
                DateTime.TryParse(s, c, DateTimeStyles.AssumeUniversal, out var a);
                DateTime.ParseExact(s, "o", c, DateTimeStyles.AssumeUniversal);
                DateTime.TryParseExact(s, "o", c, DateTimeStyles.AssumeUniversal, out var b);
                """;

            Assert.That(FindRelabellingParses("API/Sample.cs", source), Has.Count.EqualTo(3));
        }

        [Test]
        public void Scanner_CallSpanningSeveralLines_IsReportedAtItsFirstLine()
        {
            const string source = """
                var parsed = DateTime.TryParse(
                    releaseDate.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var date);
                """;

            var findings = FindRelabellingParses("API/Sample.cs", source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].Line, Is.EqualTo(1));
            }
        }

        [Test]
        public void Scanner_ParseThatAlsoAdjustsToUniversal_IsAccepted()
        {
            const string source = """
                DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var at);
                var d = DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                var parsed = DateTime.TryParse(
                    releaseDate.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var date);
                """;

            Assert.That(FindRelabellingParses("API/Sample.cs", source), Is.Empty);
        }

        [Test]
        public void Scanner_ParseQuotedInAComment_IsNotReported()
        {
            const string source = """
                // Never DateTime.Parse(s, c, DateTimeStyles.AssumeUniversal) here.
                /* nor DateTime.Parse(s, c, DateTimeStyles.AssumeUniversal) */
                var d = MyDateTime.Parse(s, c, DateTimeStyles.AssumeUniversal);
                """;

            Assert.That(FindRelabellingParses("API/Sample.cs", source), Is.Empty);
        }

        private static ScanResult ScanProductionSources()
        {
            var productionRoot = Path.Combine(RepositoryRoot(), ProductionProjectDirectory);
            var findings = new List<ParseFinding>();
            var filesScanned = 0;

            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(productionRoot, file).Replace('\\', '/');

                if (IsExcluded(relativePath))
                {
                    continue;
                }

                filesScanned++;
                findings.AddRange(FindRelabellingParses(relativePath, File.ReadAllText(file)));
            }

            return new ScanResult(findings, filesScanned);
        }

        private static bool IsExcluded(string relativePath)
        {
            var probe = "/" + relativePath;

            return ExcludedPathSegments.Any(segment => probe.Contains(segment, StringComparison.Ordinal));
        }

        private static List<ParseFinding> FindRelabellingParses(string relativePath, string source)
        {
            var code = BlankComments(source);
            var findings = new List<ParseFinding>();
            var index = code.IndexOf(DateTimeReceiver, StringComparison.Ordinal);

            while (index >= 0)
            {
                var openParen = OpeningParenOfParseCall(code, index);

                if (openParen >= 0)
                {
                    var call = code[index..ClosingParenAfter(code, openParen)];

                    if (call.Contains(AssumeUniversal, StringComparison.Ordinal)
                        && !call.Contains(AdjustToUniversal, StringComparison.Ordinal))
                    {
                        var line = code.AsSpan(0, index).Count('\n') + 1;
                        findings.Add(new ParseFinding(relativePath, line, call.ReplaceLineEndings(" ")));
                    }
                }

                index = code.IndexOf(DateTimeReceiver, index + DateTimeReceiver.Length, StringComparison.Ordinal);
            }

            return findings;
        }

        /// <summary>
        /// The index of the call's "(" when <paramref name="receiverIndex"/> starts a DateTime parse
        /// call, otherwise -1. Rejects identifiers that merely end in DateTime, such as MyDateTime.
        /// </summary>
        private static int OpeningParenOfParseCall(string code, int receiverIndex)
        {
            if (receiverIndex > 0 && (char.IsLetterOrDigit(code[receiverIndex - 1]) || code[receiverIndex - 1] == '_'))
            {
                return -1;
            }

            var nameStart = receiverIndex + DateTimeReceiver.Length;
            var nameEnd = nameStart;
            while (nameEnd < code.Length && char.IsLetter(code[nameEnd]))
            {
                nameEnd++;
            }

            if (!ParseMethods.Contains(code[nameStart..nameEnd], StringComparer.Ordinal))
            {
                return -1;
            }

            var paren = nameEnd;
            while (paren < code.Length && char.IsWhiteSpace(code[paren]))
            {
                paren++;
            }

            return paren < code.Length && code[paren] == '(' ? paren : -1;
        }

        private static int ClosingParenAfter(string code, int openParen)
        {
            var depth = 0;

            for (var index = openParen; index < code.Length; index++)
            {
                depth += code[index] switch
                {
                    '(' => 1,
                    ')' => -1,
                    _ => 0,
                };

                if (depth == 0)
                {
                    return index + 1;
                }
            }

            return code.Length;
        }

        private static List<string> Describe(List<ParseFinding> findings)
        {
            return findings
                .Select(finding => $"{finding.RelativePath}:{finding.Line} [{finding.Call}]")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Blanks line and block comments while keeping line breaks, so a parse quoted in a comment
        /// that explains this very bug is not mistaken for code and line numbers stay true.
        /// </summary>
        private static string BlankComments(string source)
        {
            var code = new StringBuilder(source.Length);
            var index = 0;

            while (index < source.Length)
            {
                if (source.AsSpan(index).StartsWith("//"))
                {
                    while (index < source.Length && source[index] != '\n')
                    {
                        code.Append(' ');
                        index++;
                    }

                    continue;
                }

                if (source.AsSpan(index).StartsWith("/*"))
                {
                    var end = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
                    end = end < 0 ? source.Length : end + 2;

                    for (; index < end; index++)
                    {
                        code.Append(source[index] == '\n' ? '\n' : ' ');
                    }

                    continue;
                }

                code.Append(source[index]);
                index++;
            }

            return code.ToString();
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the zoned timestamp parse scan.");
            return directory!.FullName;
        }
    }
}
