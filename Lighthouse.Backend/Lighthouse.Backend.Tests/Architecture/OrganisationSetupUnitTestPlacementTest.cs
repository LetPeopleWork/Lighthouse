namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The unit tests of Organisation set-up sit next to the module: under Modules/OrganisationSetup in this project, in the
    /// folder of the layer their subject lives in, with the namespace of that folder, and with no copy left anywhere else.
    /// Files are found on disk and classes in the compiled test assembly. The check also runs on a made-up tree, where a
    /// copy left behind and a file in the wrong layer are each reported and the compliant file is not.
    /// </summary>
    [TestFixture]
    public class OrganisationSetupUnitTestPlacementTest
    {
        private static string ModuleTestFolder => "Modules/OrganisationSetup";

        private static string TestRootNamespace => "Lighthouse.Backend.Tests";

        // The class keeps the name it had before the move, which is not always the file name.
        private static readonly (string TestFile, string Layer, string TestClass)[] SetUpUnitTestFiles =
        [
            ("RecurringBlackoutRuleExtensionsTest", "Domain", "RecurringBlackoutRuleExtensionsTest"),
            ("WeeklyRecurrenceTest", "Domain", "WeeklyRecurrenceTest"),
            ("AppSettingServiceTest", "Application", "AppSettingServiceTests"),
            ("BlackoutPeriodServiceTest", "Application", "BlackoutPeriodServiceTest"),
            ("FeatureRankSeederTest", "Application", "FeatureRankSeederTest"),
            ("RecurringBlackoutRuleServiceTest", "Application", "RecurringBlackoutRuleServiceTest"),
            ("TerminologyServiceTest", "Application", "TerminologyServiceTest"),
            ("ConnectionSaveServiceTest", "Application/Connections", "ConnectionSaveServiceTest"),
            ("WorkTrackingSystemConnectionUsageTest", "Application/Connections", "WorkTrackingSystemConnectionUsageTest"),
        ];

        private static readonly Lazy<List<string>> TestSourcePaths = new(ReadTestSourcePaths);

        private static IEnumerable<TestCaseData> EverySetUpUnitTestFile()
            => SetUpUnitTestFiles.Select(entry => new TestCaseData(entry.TestFile, entry.Layer).SetArgDisplayNames(entry.TestFile, entry.Layer));

        private static IEnumerable<TestCaseData> EverySetUpUnitTestClass()
            => SetUpUnitTestFiles.Select(entry => new TestCaseData(entry.TestClass, entry.Layer).SetArgDisplayNames(entry.TestClass, entry.Layer));

        [TestCaseSource(nameof(EverySetUpUnitTestFile))]
        public void ASetUpUnitTest_SitsInTheFolderOfItsLayer_AndNoCopyIsLeftElsewhere(string testFile, string layer)
        {
            var paths = TestSourcePaths.Value;

            var misplaced = WhenThePlacementIsRead(paths, testFile, layer);

            ThenNothingIsMisplaced(misplaced, testFile, layer);
        }

        [TestCaseSource(nameof(EverySetUpUnitTestClass))]
        public void ASetUpUnitTestClass_CarriesTheNamespaceOfItsFolder(string testClass, string layer)
        {
            var expected = $"{TestRootNamespace}.{ModuleTestFolder.Replace('/', '.')}.{layer.Replace('/', '.')}.{testClass}";

            var found = typeof(OrganisationSetupUnitTestPlacementTest).Assembly.GetType(expected);

            Assert.That(found, Is.Not.Null,
                $"No class {expected} is compiled. A set-up unit test carries the namespace of its folder, so a reader finds it by the module " +
                "layer it tests. Change the namespace line to the folder's namespace and keep the class name.");
        }

        [Test]
        public void AMadeUpTree_ReportsACopyLeftBehindAndAFileInTheWrongLayer_AndNotTheCompliantFile()
        {
            string[] tree = [$"{ModuleTestFolder}/Domain/AMadeUpTest.cs", "Services/AMadeUpTest.cs", $"{ModuleTestFolder}/Application/AnotherMadeUpTest.cs"];

            var misplaced = WhenThePlacementIsRead(tree, "AMadeUpTest", "Domain")
                .Concat(WhenThePlacementIsRead(tree, "AnotherMadeUpTest", "Domain"))
                .ToList();

            string[] reported = ["Services/AMadeUpTest.cs is a copy left outside Modules/OrganisationSetup/Domain",
                "Modules/OrganisationSetup/Application/AnotherMadeUpTest.cs is a copy left outside Modules/OrganisationSetup/Domain",
                "AnotherMadeUpTest.cs is missing from Modules/OrganisationSetup/Domain"];

            Assert.That(misplaced, Is.EquivalentTo(reported),
                "The placement check did not report exactly the copy left behind and the file in the wrong layer of the made-up tree, so a real " +
                "misplaced test could pass unseen. Report every copy outside the layer folder and every file missing from it.");
        }

        private static List<string> WhenThePlacementIsRead(IReadOnlyCollection<string> paths, string testFile, string layer)
        {
            var expected = $"{ModuleTestFolder}/{layer}/{testFile}.cs";
            var copies = paths.Where(path => path.EndsWith($"/{testFile}.cs", StringComparison.Ordinal) || path == $"{testFile}.cs").ToList();

            var report = copies.Where(path => path != expected).Select(path => $"{path} is a copy left outside {ModuleTestFolder}/{layer}").ToList();
            if (!copies.Contains(expected))
            {
                report.Add($"{testFile}.cs is missing from {ModuleTestFolder}/{layer}");
            }

            return report;
        }

        private static void ThenNothingIsMisplaced(List<string> misplaced, string testFile, string layer)
            => Assert.That(misplaced, Is.Empty,
                $"{testFile} is not where Organisation set-up's unit tests live: {string.Join("; ", misplaced)}. Its subject lives in set-up's " +
                $"{layer}, so the test sits in {ModuleTestFolder}/{layer} and nowhere else. Move the file there and delete any copy.");

        private static List<string> ReadTestSourcePaths()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend.Tests.csproj")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "The test project folder was not found above the test run's folder, so no placement could be read.");

            var root = directory!.FullName;
            var paths = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
                .Where(path => !path.StartsWith("bin/", StringComparison.Ordinal) && !path.StartsWith("obj/", StringComparison.Ordinal))
                .ToList();

            Assert.That(paths, Has.Count.GreaterThan(500), $"Only {paths.Count} source files were found under {root}, so the check would pass without reading the test project.");
            return paths;
        }
    }
}
