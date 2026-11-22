using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A test that takes its "today" from <see cref="TestToday"/> must not also read the live clock.
    /// <see cref="TestToday"/> freezes the day when the test process starts, so a run that crosses
    /// midnight UTC leaves the two clocks on different days and every expectation built from both is
    /// off by one. Such a test derives every date from <see cref="TestToday"/> instead.
    ///
    /// A plain source scanner, like <see cref="CalendarDayAnchorSeamArchUnitTest"/>: reading the
    /// clock is a property access that no dependency rule can express.
    /// </summary>
    [TestFixture]
    public partial class TestsReadOneClockArchUnitTest
    {
        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        /// <summary>
        /// A guard that scans nothing is worse than no guard. The test project held several hundred
        /// files when this floor was set.
        /// </summary>
        private const int MinimumTestFilesScanned = 200;

        [GeneratedRegex(@"\bDateTime(?:Offset)?\.(?:UtcNow|Now|Today)\b")]
        private static partial Regex LiveClock();

        [GeneratedRegex(@"\bTestToday\b")]
        private static partial Regex FrozenClock();

        /// <summary>The files that may read both clocks, each with the reason.</summary>
        private static readonly Dictionary<string, string> Allowed = new()
        {
            ["TestClockBoundaryTest.cs"] = "its subject is the frozen instant, which it must compare with the real clock",
            ["Architecture/TestsReadOneClockArchUnitTest.cs"] = "this guard names both clocks in its patterns and its made-up case",
        };

        [Test]
        public void NoTestMixesTheFrozenDayWithTheLiveClock()
        {
            var scan = ScanTestSources();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scan.FilesScanned, Is.GreaterThanOrEqualTo(MinimumTestFilesScanned),
                    "The scan found too few test files; the test project root was probably resolved wrongly.");
                Assert.That(scan.Offenders, Is.Empty,
                    "These test files take their day from TestToday and also read the live clock "
                    + "(DateTime.UtcNow, DateTime.Now, DateTime.Today, DateTimeOffset.UtcNow/Now), so a run that crosses "
                    + "midnight UTC splits them by a day. Derive every date from TestToday.FrozenNow, TestToday.Ambient or "
                    + "TestToday.AmbientAsUtcMidnight instead:" + Environment.NewLine
                    + string.Join(Environment.NewLine, scan.Offenders));
            }
        }

        [Test]
        public void AFileThatMixesBothClocksIsReported()
        {
            const string madeUp = "var age = TestToday.Ambient.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;";

            Assert.That(MixesBothClocks(madeUp), Is.True);
        }

        [Test]
        public void ACommentThatNamesTheLiveClockIsNotReported()
        {
            Assert.That(MixesBothClocks("// not DateTime.UtcNow\nvar day = TestToday.Ambient;"), Is.False);
        }

        [Test]
        public void AFileThatReadsOnlyOneClockIsNotReported()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(MixesBothClocks("var start = TestToday.FrozenNow.UtcDateTime.AddDays(-5);"), Is.False);
                Assert.That(MixesBothClocks("var start = DateTime.UtcNow.AddDays(-5);"), Is.False);
            }
        }

        private static bool MixesBothClocks(string source)
        {
            var code = string.Join('\n', source.Split('\n').Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

            return FrozenClock().IsMatch(code) && LiveClock().IsMatch(code);
        }

        private static (List<string> Offenders, int FilesScanned) ScanTestSources()
        {
            var root = Path.Combine(RepositoryRoot(), TestProjectDirectory);
            var offenders = new List<string>();
            var filesScanned = 0;

            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');

                if (relativePath.StartsWith("obj/", StringComparison.Ordinal) || relativePath.StartsWith("bin/", StringComparison.Ordinal))
                {
                    continue;
                }

                filesScanned++;

                if (relativePath != "TestToday.cs" && !Allowed.ContainsKey(relativePath) && MixesBothClocks(File.ReadAllText(file)))
                {
                    offenders.Add(relativePath);
                }
            }

            offenders.Sort(StringComparer.Ordinal);
            return (offenders, filesScanned);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the test-source scan.");
            return directory!.FullName;
        }
    }
}
