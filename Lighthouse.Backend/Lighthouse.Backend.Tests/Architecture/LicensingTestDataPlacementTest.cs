namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Licensing test data sits with the Licensing tests: the helper class and the integration test live in the
    /// Modules/Licensing namespace of this project, the two committed licence files are copied to Modules/Licensing in
    /// the test output, and every file that names the licence fixtures (ignore list, both workflows, the project file
    /// and the examine builder) names that folder and never the old one. Repository files are read from the folder
    /// above the one that holds Lighthouse.sln. The path rule also runs on made-up file contents, so a file that names
    /// the old folder, one that mixes old and new, and one that names no licence file at all must each be reported,
    /// and a file that names only the new folder must not.
    /// </summary>
    [TestFixture]
    public class LicensingTestDataPlacementTest
    {
        private const string NewNamespace = "Lighthouse.Backend.Tests.Modules.Licensing";

        private const string NewFolder = "Modules/Licensing/";

        private const string OldFolder = "Services/Implementation/Licensing/";

        private static readonly string[] LicenceFixtures = ["invalid_license.json", "valid_expired_license.json", "valid_not_expired_license.json"];

        private static readonly string[] CommittedLicenceFixtures = ["invalid_license.json", "valid_expired_license.json"];

        private static readonly string[] FilesNamingTheFixtures =
        [
            ".gitignore",
            ".github/workflows/ci_backend.yml",
            ".github/workflows/ci_verifyauth.yml",
            "Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj",
            "Scripts/examine/build-examine-commands",
        ];

        private static IEnumerable<TestCaseData> EveryFileNamingTheFixtures()
            => FilesNamingTheFixtures.Select(file => new TestCaseData(file).SetArgDisplayNames(file));

        private static IEnumerable<TestCaseData> MadeUpFileContents()
        {
            yield return new TestCaseData("ci.yml", "python gen.py key Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/valid_not_expired_license.json \"1886\"", 1, OldFolder)
                .SetArgDisplayNames("a workflow line naming the old folder is reported");
            yield return new TestCaseData("tests.csproj", "<None Update=\"Services\\Implementation\\Licensing\\invalid_license.json\">", 1, OldFolder)
                .SetArgDisplayNames("a project file item with backslashes naming the old folder is reported");
            yield return new TestCaseData("builder", "licence=\"$repo/Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/Licensing/valid_not_expired_license.json\"", 0, string.Empty)
                .SetArgDisplayNames("a builder line naming only the new folder is clean");
            yield return new TestCaseData("tests.csproj", "<None Update=\"Modules\\Licensing\\invalid_license.json\">", 0, string.Empty)
                .SetArgDisplayNames("a project file item naming the new folder relative to the project is clean");
            yield return new TestCaseData(".gitignore", "Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/Licensing/valid_not_expired_license.json\nLighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/invalid_license.json", 1, OldFolder)
                .SetArgDisplayNames("one old line next to a new line is reported once");
            yield return new TestCaseData("ci.yml", "echo nothing about licences here", 1, "names no licence fixture")
                .SetArgDisplayNames("a file that names no licence fixture is reported, not passed");
            yield return new TestCaseData("tests.csproj", "<None Update=\"Modules\\Licensing\\Application\\other.json\">\n<None Update=\"Modules\\Licensing\\invalid_license.json\">", 0, string.Empty)
                .SetArgDisplayNames("another file in the new folder is not mistaken for a fixture");
        }

        [Test]
        [TestCase("TestLicenseData")]
        [TestCase("LicensingIntegrationTest")]
        public void LicensingTestClass_LivesInTheModuleNamespace(string typeName)
        {
            var namespaces = typeof(LicensingTestDataPlacementTest).Assembly.GetTypes()
                .Where(type => !type.IsNested && type.Name == typeName)
                .Select(type => type.Namespace)
                .ToList();

            Assert.That(namespaces, Is.EqualTo(new[] { NewNamespace }),
                $"{typeName} must exist exactly once in the test assembly, in namespace {NewNamespace}, but was found in: [{string.Join(", ", namespaces)}]. "
                + "Why: the Licensing test tree mirrors the Licensing module one to one. "
                + $"How to fix: move {typeName} to Modules/Licensing in the test project and make its namespace line match the folder.");
        }

        [Test]
        [TestCase("invalid_license.json")]
        [TestCase("valid_expired_license.json")]
        public void CommittedLicenceFile_IsCopiedToTheModuleFolderOfTheTestOutput(string fixture)
        {
            var output = TestContext.CurrentContext.TestDirectory;
            var inTheNewPlace = File.Exists(Path.Combine(output, NewFolder, fixture));
            var inTheOldPlace = File.Exists(Path.Combine(output, OldFolder, fixture));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inTheNewPlace, Is.True,
                    $"{fixture} is not in {NewFolder} of the test output ({output}). "
                    + "Why: the licence tests read it from there. "
                    + $"How to fix: move the file to Modules/Licensing in the test project and let its CopyToOutputDirectory item in the project file name that folder.");
                Assert.That(inTheOldPlace, Is.False,
                    $"{fixture} is still copied to {OldFolder} of the test output ({output}). "
                    + "Why: a second copy in the old folder means the project file still names it. "
                    + "How to fix: change the CopyToOutputDirectory item to Modules/Licensing and delete the old output folder.");
            }
        }

        [Test]
        [TestCaseSource(nameof(EveryFileNamingTheFixtures))]
        public void FileNamingTheLicenceFixtures_NamesTheModuleFolderAndNotTheOldOne(string relativePath)
        {
            var full = Path.Combine(RepositoryRoot(), relativePath);
            Assert.That(File.Exists(full), Is.True,
                $"{relativePath} was not found at {full}. Why: it is one of the five files that name the licence fixtures. How to fix: restore the file or run the tests from inside the repository checkout.");

            var findings = FindingsFor(relativePath, File.ReadAllText(full));

            Assert.That(findings, Is.Empty, string.Join(Environment.NewLine, findings)
                + Environment.NewLine + "Why: the fixtures live under Lighthouse.Backend.Tests/Modules/Licensing, so a path to any other folder reads a file that is not there. "
                + "How to fix: change the named path to Lighthouse.Backend.Tests/Modules/Licensing.");
        }

        [Test]
        [TestCaseSource(nameof(MadeUpFileContents))]
        public void FixturePathRule_ReportsAFileThatNamesTheOldFolderOrNoFixture(string file, string content, int expectedFindings, string expectedMention)
        {
            var findings = FindingsFor(file, content);

            Assert.That(findings, Has.Count.EqualTo(expectedFindings),
                $"The rule gave {findings.Count} findings for the made-up content of {file}, expected {expectedFindings}: [{string.Join(" | ", findings)}]. "
                + "Why: a rule that misreads a path would let a stale one pass or flag a good one. How to fix: correct FindingsFor so it normalises separators and compares the folder before the file name.");
            Assert.That(findings.All(finding => finding.Contains(file) && finding.Contains(expectedMention)), Is.True,
                $"Each finding must name the file ({file}) and the offending path or reason ({expectedMention}); got [{string.Join(" | ", findings)}]. How to fix: put both in the finding text.");
        }

        private static List<string> FindingsFor(string file, string text)
        {
            var findings = new List<string>();
            var lines = text.Replace('\\', '/').Split('\n');
            var fixtureLines = 0;

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                if (line.Contains(OldFolder, StringComparison.Ordinal))
                {
                    findings.Add($"{file}:{index + 1} names {OldFolder}: {line.Trim()}");
                }

                if (!LicenceFixtures.Any(fixture => line.Contains(fixture, StringComparison.Ordinal)))
                {
                    continue;
                }

                fixtureLines++;
                var namesNewFolder = LicenceFixtures.Any(fixture => line.Contains(NewFolder + fixture, StringComparison.Ordinal));
                if (!namesNewFolder && !line.Contains(OldFolder, StringComparison.Ordinal))
                {
                    findings.Add($"{file}:{index + 1} names a licence fixture outside {NewFolder}: {line.Trim()}");
                }
            }

            if (fixtureLines == 0)
            {
                findings.Add($"{file} names no licence fixture, so the rule would pass without checking anything");
            }

            return findings;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory?.Parent, Is.Not.Null,
                "Could not locate Lighthouse.sln above the test output. Why: the repository files are read from the folder above it. How to fix: run the tests from inside the repository checkout.");

            return directory!.Parent!.FullName;
        }
    }
}
