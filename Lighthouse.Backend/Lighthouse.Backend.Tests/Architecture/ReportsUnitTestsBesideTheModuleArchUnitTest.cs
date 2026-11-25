using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The 18 unit tests of Reports sit next to the module: under Modules/Reports in this project, in the folder of the
    /// layer their subject lives in, in the namespace of that folder, with their class names kept. The folder they left
    /// holds no test any more, and the tests at the port, the host and the wire, and the repository test, stay where they
    /// are. Files are found on disk, and classes are read from the compiled test assembly with its symbols, which say which
    /// file each class was compiled from. Each test class is looked up by name, so a class that is missing or renamed fails.
    /// </summary>
    [TestFixture]
    public class ReportsUnitTestsBesideTheModuleArchUnitTest
    {
        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ModuleFolder = "Modules/Reports";

        private const string ThisFile = "Architecture/ReportsUnitTestsBesideTheModuleArchUnitTest.cs";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        private static readonly string[] DomainTests = ["ReportCapPolicyTest", "ReportWindowPolicyTest"];

        private static readonly string[] MetricsTests =
        [
            "ArrivalsReportMetricTest",
            "CycleTimeReportMetricTest",
            "DailySeriesSummaryTest",
            "ThroughputReportMetricTest",
            "WorkItemAgeReportMetricTest",
        ];

        private static readonly string[] ApplicationTests =
        [
            "PortfolioReportMetricSeriesTest",
            "ReportBulkCommandsTest",
            "ReportBulkRemoverTest",
            "ReportCommandsTest",
            "ReportLoggingTest",
            "ReportMetricSeriesByOwnerTest",
            "ReportOwnerLookupTest",
            "ReportQueriesTest",
            "TeamReportMetricSeriesTest",
            "ThenAndNowTemplateEditTest",
            "ThenAndNowTemplateTest",
        ];

        private static readonly string[] StayingFiles =
        [
            "API/Integration/Reports/BulkReports/BulkReportsAcceptanceTest.cs",
            "API/Integration/Reports/ReportsAcceptanceTest.cs",
            "API/Integration/Reports/ReportsAtThePortTest.cs",
            "API/ReportOwnerWriteAccessTest.cs",
            "API/ReportsControllerTest.cs",
            "API/TeamReportsControllerTest.cs",
            "Models/Reports/ReportTest.cs",
            "Models/Reports/ThenAndNowPayloadRoundTripTest.cs",
            "Services/Implementation/Repositories/ReportRepositoryTest.cs",
        ];

        private static readonly Lazy<List<string>> TestSourcePaths = new(ReadSourcePaths);

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        // Built from segments so that no literal names the folder the tests left.
        private static string OldTestFolder => string.Join('/', "Services", "Implementation", "Reports");

        private static IEnumerable<(string Name, string Layer)> EveryUnitTest()
            => DomainTests.Select(name => (name, "Domain"))
                .Concat(MetricsTests.Select(name => (name, "Application/Metrics")))
                .Concat(ApplicationTests.Select(name => (name, "Application")));

        private static IEnumerable<TestCaseData> EveryReportsUnitTest()
            => EveryUnitTest().Select(test => new TestCaseData(test.Name, test.Layer).SetArgDisplayNames(test.Name));

        private static IEnumerable<TestCaseData> EveryStayingTest()
            => StayingFiles.Select(file => new TestCaseData(file).SetArgDisplayNames(Path.GetFileNameWithoutExtension(file)));

        [Test]
        public void TheReportsUnitTestsAreEighteenDistinctClasses_TwoInDomain_FiveInMetrics_ElevenInApplication()
        {
            var all = EveryUnitTest().Select(test => test.Name).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(all, Has.Count.EqualTo(18).And.Unique, $"The list of Reports unit tests holds {all.Count} entries. Why it matters: the move is judged against the 18 unit tests that exist, and a shorter or doubled list lets one stay behind unseen. How to fix: list each of the 18 once.");
                Assert.That(DomainTests, Has.Length.EqualTo(2), "Domain holds the two policy tests. Why it matters: the layer split is part of what is promised. How to fix: list ReportCapPolicyTest and ReportWindowPolicyTest.");
                Assert.That(MetricsTests, Has.Length.EqualTo(5), "Application/Metrics holds the five metric and series-summary tests. Why it matters: the layer split is part of what is promised. How to fix: list the five.");
                Assert.That(ApplicationTests, Has.Length.EqualTo(11), "Application holds the other eleven tests. Why it matters: the layer split is part of what is promised. How to fix: list the eleven.");
            }
        }

        [TestCaseSource(nameof(EveryReportsUnitTest))]
        public void AReportsUnitTest_SitsOnlyInItsModuleLayerFolder_AndKeepsItsClassInThatLayerNamespace(string name, string layer)
        {
            var expectedFile = $"{ModuleFolder}/{layer}/{name}.cs";
            var expectedNamespace = $"{TestRootNamespace}.{ModuleFolder.Replace('/', '.')}.{layer.Replace('/', '.')}";

            var filesNamedLikeIt = TestSourcePaths.Value.Where(path => path.EndsWith($"/{name}.cs", StringComparison.Ordinal)).ToList();
            var classesNamedLikeIt = CompiledTestClasses.Value.Where(compiled => compiled.Files.Any(file => file.EndsWith($"/{name}.cs", StringComparison.Ordinal))).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    filesNamedLikeIt,
                    Is.EqualTo(new[] { expectedFile }),
                    $"{name}.cs sits at [{string.Join(", ", filesNamedLikeIt)}] instead of only at {expectedFile}. Why it matters: a Reports unit test belongs beside the layer of the module it tests, and a copy or a stray file splits the context. How to fix: git mv the file to {expectedFile}.");
                Assert.That(
                    classesNamedLikeIt.Select(compiled => compiled.FullName),
                    Is.EqualTo(new[] { $"{expectedNamespace}.{name}" }),
                    $"The compiled classes of {name}.cs are [{string.Join(", ", classesNamedLikeIt.Select(compiled => compiled.FullName))}] instead of only {expectedNamespace}.{name}. Why it matters: a moved test keeps its class name and takes the namespace of its folder, so a test filter and a reader still find it under the same name. How to fix: change the namespace line to {expectedNamespace} and keep the class name.");
            }
        }

        [Test]
        public void NoReportsTestIsLeftInTheFolderTheUnitTestsLeft()
        {
            var left = TestSourcePaths.Value.Where(path => path.StartsWith($"{OldTestFolder}/", StringComparison.Ordinal)).ToList();

            Assert.That(
                left,
                Is.Empty,
                $"These test files still sit in the folder the Reports unit tests left: [{string.Join(", ", left)}]. Why it matters: a test left there splits the context again, and that folder is meant to end empty. How to fix: git mv the file into the layer folder of its subject under {ModuleFolder} and change its namespace to that folder's.");
        }

        [Test]
        public void TheReportsModuleTestFolder_HoldsTheEighteenUnitTestsAndNothingElse()
        {
            var expected = EveryUnitTest().Select(test => $"{ModuleFolder}/{test.Layer}/{test.Name}.cs").ToList();
            var inTheModule = TestSourcePaths.Value.Where(path => path.StartsWith($"{ModuleFolder}/", StringComparison.Ordinal)).ToList();

            Assert.That(
                inTheModule,
                Is.EquivalentTo(expected),
                $"The files under {ModuleFolder} are [{string.Join(", ", inTheModule)}], not the 18 unit tests in their layer folders. Why it matters: the port, host, wire-contract and repository tests stay where they are, and a file in the module that is not one of the 18 is a move nobody asked for. How to fix: git mv each of the 18 into its layer folder, and any other file back to its old place.");
        }

        [Test]
        public void EveryClassOfTheReportsModuleTests_HasTheNamespaceOfItsFolder()
        {
            var moduleClasses = CompiledTestClasses.Value.Where(compiled => compiled.Files.Any(file => file.StartsWith($"{ModuleFolder}/", StringComparison.Ordinal))).ToList();
            var mismatches = moduleClasses.Where(compiled => !compiled.Files.All(file => compiled.Namespace == FolderNamespace(file))).Select(compiled => compiled.FullName).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(moduleClasses, Is.Not.Empty, $"The compiled tests hold no class from a file under {ModuleFolder}. Why it matters: a folder rule over an empty folder passes without looking at anything. How to fix: git mv the Reports unit tests into {ModuleFolder}.");
                Assert.That(mismatches, Is.Empty, $"These classes do not have the namespace of their folder: [{string.Join(", ", mismatches)}]. Why it matters: the namespace equals the folder for every moved file. How to fix: change the namespace line to the folder's.");
            }
        }

        [TestCaseSource(nameof(EveryStayingTest))]
        public void AReportsPortHostWireOrRepositoryTest_StaysWhereItIs_AndOutsideTheModuleNamespace(string file)
        {
            var moduleNamespace = $"{TestRootNamespace}.{ModuleFolder.Replace('/', '.')}";
            var classesMoved = CompiledTestClasses.Value
                .Where(compiled => compiled.Files.Contains(file) && (compiled.Namespace == moduleNamespace || compiled.Namespace.StartsWith($"{moduleNamespace}.", StringComparison.Ordinal)))
                .Select(compiled => compiled.FullName)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TestSourcePaths.Value, Does.Contain(file), $"{file} is gone from its place. Why it matters: only the 18 unit tests move, and a staying test that is gone was moved or lost. How to fix: git mv it back to where it was.");
                Assert.That(classesMoved, Is.Empty, $"These classes of {file} sit in the module's test namespace: [{string.Join(", ", classesMoved)}]. Why it matters: staying tests keep their namespaces; only their using lines change. How to fix: change the namespace line back to the one of the folder the file sits in.");
            }
        }

        private static string FolderNamespace(string testProjectRelativePath)
            => $"{TestRootNamespace}.{Path.GetDirectoryName(testProjectRelativePath)!.Replace('\\', '.').Replace('/', '.')}";

        private static List<string> ReadSourcePaths()
        {
            var projectRoot = Path.Combine(SolutionRoot(), TestProjectDirectory);

            var paths = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(projectRoot, file).Replace('\\', '/'))
                .Where(path => !IsBuildOutput(path))
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(paths, Does.Contain(ThisFile), $"The source scan of {projectRoot} did not find {ThisFile}, so it is not looking at that project.");
            return paths;
        }

        private static List<CompiledClass> ReadCompiledTestClasses()
        {
            var assemblyPath = typeof(ReportsUnitTestsBesideTheModuleArchUnitTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                classes.Where(compiled => compiled.Files.Contains(ThisFile)).Select(compiled => compiled.FullName),
                Does.Contain(typeof(ReportsUnitTestsBesideTheModuleArchUnitTest).FullName),
                $"The symbols of {assemblyPath} did not tie this very class to {ThisFile}, so no class could be tied to its file.");
            return classes;
        }

        private static List<string> FilesOf(TypeDefinition type)
            => [.. TypeAndItsNestedTypes(type)
                .SelectMany(each => each.Methods)
                .Where(method => method.DebugInformation.HasSequencePoints)
                .SelectMany(method => method.DebugInformation.SequencePoints)
                .Select(point => TestProjectRelativePath(point.Document.Url))
                .OfType<string>()
                .Where(path => !IsBuildOutput(path))
                .Distinct(StringComparer.Ordinal)];

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => new[] { type }.Concat(type.NestedTypes.SelectMany(TypeAndItsNestedTypes));

        private static string? TestProjectRelativePath(string documentPath)
        {
            var normalised = documentPath.Replace('\\', '/');
            var marker = $"/{TestProjectDirectory}/";
            var start = normalised.LastIndexOf(marker, StringComparison.Ordinal);
            return start < 0 ? null : normalised[(start + marker.Length)..];
        }

        private static bool IsBuildOutput(string path) => BuildOutputFolders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal));

        private static string SolutionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the source scans.");
            return directory!.FullName;
        }

        private sealed record CompiledClass(string FullName, string Namespace, IReadOnlyList<string> Files);
    }
}
