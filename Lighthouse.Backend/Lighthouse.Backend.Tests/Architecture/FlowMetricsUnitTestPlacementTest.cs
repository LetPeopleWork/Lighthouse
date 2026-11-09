using Lighthouse.Backend.Tests.PlacementFixtures.Modules.FlowMetrics.Domain;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The unit tests of the Flow metrics module sit next to it: under Modules/FlowMetrics in this project, in the
    /// folder of the module layer their subject lives in, and in the namespace of that folder. The test files are
    /// found on disk, and the classes are read from the compiled test assembly together with its symbols, which say
    /// which file each class was compiled from, so no source text is pinned. Each rule also runs on made-up inputs:
    /// a test file left in its old place, a copy left behind, a file in the wrong layer and a class whose namespace
    /// does not match its folder must each be reported, and a compliant twin must not be.
    /// </summary>
    [TestFixture]
    public class FlowMetricsUnitTestPlacementTest
    {
        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ModuleTestFolder = "Modules/FlowMetrics";

        private const string ThisFile = "Architecture/FlowMetricsUnitTestPlacementTest.cs";

        private const string AMadeUpTestFile = "AMadeUpCalculatorTest";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        // Each Flow metrics unit-test file, without its .cs, and the module layer folder its subject lives in.
        private static readonly (string TestFile, string Layer)[] FlowMetricsUnitTestFiles =
        [
            ("BaselineValidationServiceTest", "Domain"),
            ("PercentileCalculatorTest", "Domain"),
            ("SleRiskCalculatorTest", "Domain"),
            ("XmRCalculatorTest", "Domain"),
            ("BaseMetricsServiceNamedCycleTimeTest", "Application"),
            ("BaseMetricsServiceTests", "Application"),
            ("BlockedCountSeriesBuilderTests", "Application"),
            ("GapAskingSeriesQueryTest", "Application"),
            ("MetricsAsOfGroupNameTest", "Application"),
            ("MetricsCacheKeyTest", "Application"),
            ("OverTimeGapReconcilerTest", "Application"),
            ("PercentileSnapshotWriterTest", "Application"),
            ("PortfolioMetricsServiceTests", "Application"),
            ("ProcessBehaviorSnapshotWriterTest", "Application"),
            ("TeamMetricsServiceSnapshotTests", "Application"),
            ("TeamMetricsServiceTests", "Application"),
            ("ClosedInRangeCharacterisationTest", "Application"),
            ("InstanceDayItemBucketingTest", "Application"),
            ("WorkItemAgeAsOfDateTest", "Application"),
            ("OverTimeFillTargetTest", "Application"),
            ("ReconstructionMemoTest", "Application"),
            ("RetentionEdgeTest", "Application"),
            ("BlackoutConfigurationChangedMetricsInvalidationHandlerTest", "Application/Policies"),
            ("BlockedCountSnapshotRecordingHandlerTests", "Application/Policies"),
            ("DemoBlockedHistoryBackfillHandlerTests", "Application/Policies"),
            ("DemoPercentilesBackfillHandlerTests", "Application/Policies"),
            ("PercentilesOverTimeRecordingHandlerTests", "Application/Policies"),
            ("PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest", "Application/Policies"),
            ("ProcessBehaviorRecordingHandlerTests", "Application/Policies"),
            ("OverTimeHistoryFillerTest", "Adapters/Background"),
            ("BlockedCountSnapshotRepositoryTests", "Adapters/Persistence"),
            ("PercentilesOverTimeSnapshotRepositoryTests", "Adapters/Persistence"),
            ("ProcessBehaviorSnapshotRepositoryTests", "Adapters/Persistence"),
        ];

        private static readonly Lazy<List<string>> TestSourcePaths = new(ReadTestSourcePaths);

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static IEnumerable<TestCaseData> EveryFlowMetricsUnitTestFile()
            => FlowMetricsUnitTestFiles.Select(entry => new TestCaseData(entry.TestFile, entry.Layer).SetArgDisplayNames(entry.TestFile, entry.Layer));

        private static IEnumerable<TestCaseData> MadeUpTestTrees()
        {
            const string moved = $"{ModuleTestFolder}/Domain/{AMadeUpTestFile}.cs";
            const string leftBehind = $"Services/Implementation/{AMadeUpTestFile}.cs";
            const string inTheWrongLayer = $"{ModuleTestFolder}/Application/{AMadeUpTestFile}.cs";
            const string aNeighbour = $"{ModuleTestFolder}/Domain/ANeighbourTest.cs";

            yield return new TestCaseData(new[] { leftBehind }, new[] { moved, leftBehind }).SetArgDisplayNames("left in its old place");
            yield return new TestCaseData(new[] { moved, leftBehind }, new[] { leftBehind }).SetArgDisplayNames("moved with a copy left behind");
            yield return new TestCaseData(new[] { inTheWrongLayer }, new[] { moved, inTheWrongLayer }).SetArgDisplayNames("moved into the wrong layer");
            yield return new TestCaseData(new[] { moved, aNeighbour }, Array.Empty<string>()).SetArgDisplayNames("moved into its layer and nowhere else");
        }

        [TestCaseSource(nameof(EveryFlowMetricsUnitTestFile))]
        public void AFlowMetricsUnitTestFile_SitsOnlyInItsModuleLayerFolder_AndItsClassesLiveInThatLayer(string testFile, string layer)
        {
            var misplacedFiles = FilePlacementProblems(testFile, layer, TestSourcePaths.Value);
            var misplacedClasses = ClassPlacementProblems(testFile, layer, CompiledTestClasses.Value);

            ThenNothingIsMisplaced(testFile, layer, misplacedFiles.Concat(misplacedClasses));
        }

        [Test]
        public void EveryClassUnderTheFlowMetricsModuleTestFolder_HasTheNamespaceOfItsFolder()
        {
            var moduleTestClasses = ClassesCompiledFrom(CompiledTestClasses.Value, path => path.StartsWith($"{ModuleTestFolder}/", StringComparison.Ordinal));

            Assert.That(
                moduleTestClasses,
                Is.Not.Empty,
                $"The compiled tests hold no class from a file under {ModuleTestFolder}. Why it matters: the Flow metrics unit tests " +
                "belong next to the module, and a folder rule over an empty folder passes without looking at anything. How to fix: " +
                $"move the Flow metrics unit-test files into the module layer folders under {ModuleTestFolder} with git mv.");

            ThenEachClassHasTheNamespaceOfItsFolder(moduleTestClasses);
        }

        [TestCaseSource(nameof(MadeUpTestTrees))]
        public void AMadeUpTestFileOutsideItsLayerFolder_IsReported_AndOneThatSitsThereIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var problems = FilePlacementProblems(AMadeUpTestFile, "Domain", sourcePaths);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the placement rule reported [{string.Join("; ", problems)}]. " +
                "Why it matters: a test file still in its old place, a copy left behind or a file in the wrong layer folder must " +
                "each be named, and a file that sits in its layer folder must not be, or the rule cannot tell a finished move " +
                "from an unfinished one. How to fix: report the expected path when it holds no file, and every other path " +
                "that holds a file of the same name.");
        }

        [Test]
        public void AMadeUpClassWhoseNamespaceDoesNotMatchItsFolder_IsReported_AndItsCompliantTwinIsNot()
        {
            var classesInThisFile = ClassesCompiledFrom(CompiledTestClasses.Value, path => path == ThisFile);

            var mismatches = NamespaceFolderMismatches(classesInThisFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    classesInThisFile.Select(compiled => compiled.FullName),
                    Does.Contain(typeof(ACompliantTwinOfThePlacementRule).FullName).And.Contain(typeof(ACalculatorTestDeclaredOutsideItsFolder).FullName),
                    $"The compiled tests do not tie both made-up classes to {ThisFile}. Why it matters: a twin the rule never looked " +
                    "at proves nothing by passing. How to fix: read every class with the files its methods were compiled from.");

                Assert.That(
                    mismatches.Select(mismatch => mismatch.FullName),
                    Is.EqualTo(new[] { typeof(ACalculatorTestDeclaredOutsideItsFolder).FullName }),
                    $"Expected exactly {typeof(ACalculatorTestDeclaredOutsideItsFolder).FullName} to be reported, got " +
                    $"[{string.Join("; ", mismatches)}]. Why it matters: a class whose namespace names a module layer while its file " +
                    "sits elsewhere hides where the test really lives, and a class whose namespace matches its folder must pass, or " +
                    "the rule flags everything. How to fix: compare each class's namespace with the namespace its folder gives.");
            }
        }

        private static List<Misplacement> FilePlacementProblems(string testFile, string layer, IReadOnlyCollection<string> sourcePaths)
        {
            var fileName = $"{testFile}.cs";
            var expectedPath = $"{ModuleTestFolder}/{layer}/{fileName}";
            var problems = new List<Misplacement>();

            if (!sourcePaths.Contains(expectedPath, StringComparer.Ordinal))
            {
                problems.Add(new Misplacement(expectedPath, "holds no such file"));
            }

            problems.AddRange(sourcePaths
                .Where(path => FileNameOf(path) == fileName && path != expectedPath)
                .Select(path => new Misplacement(path, "still holds the file")));

            return problems;
        }

        private static List<Misplacement> ClassPlacementProblems(string testFile, string layer, IReadOnlyCollection<CompiledClass> classes)
        {
            var fileName = $"{testFile}.cs";
            var expectedNamespace = NamespaceOfFolder($"{ModuleTestFolder}/{layer}");
            var fromTheFile = ClassesCompiledFrom(classes, path => FileNameOf(path) == fileName);

            if (fromTheFile.Count == 0)
            {
                return [new Misplacement(fileName, "was compiled into no class")];
            }

            return [.. fromTheFile
                .Where(compiled => compiled.Namespace != expectedNamespace)
                .Select(compiled => new Misplacement(compiled.FullName, $"lives in namespace {compiled.Namespace} instead of {expectedNamespace}"))];
        }

        private static List<NamespaceFolderMismatch> NamespaceFolderMismatches(IEnumerable<CompiledClass> classes)
            => [.. classes.SelectMany(compiled => compiled.Files
                .Select(path => (Path: path, FolderNamespace: NamespaceOfFolder(FolderOf(path))))
                .Where(file => file.FolderNamespace != compiled.Namespace)
                .Select(file => new NamespaceFolderMismatch(compiled.FullName, compiled.Namespace, file.Path, file.FolderNamespace)))];

        private static void ThenNothingIsMisplaced(string testFile, string layer, IEnumerable<Misplacement> problems)
        {
            var found = problems.ToList();

            Assert.That(
                found,
                Is.Empty,
                $"{testFile} should sit only in {ModuleTestFolder}/{layer} with every class in it in namespace " +
                $"{NamespaceOfFolder($"{ModuleTestFolder}/{layer}")}; instead: [{string.Join("; ", found)}]. Why it matters: the " +
                "Flow metrics unit tests sit next to the module only when each one is in the folder of the layer its subject " +
                "lives in, with the namespace of that folder and no copy left in its old place. How to fix: git mv the file " +
                "into that folder and change its namespace and using lines, nothing else; keep the class names.");
        }

        private static void ThenEachClassHasTheNamespaceOfItsFolder(IEnumerable<CompiledClass> classes)
        {
            var mismatches = NamespaceFolderMismatches(classes);

            Assert.That(
                mismatches,
                Is.Empty,
                $"These classes do not have the namespace of the folder their file sits in: [{string.Join("; ", mismatches)}]. " +
                "Why it matters: a reader and a test filter find a test by its namespace, so a namespace that disagrees with the " +
                "folder hides where the test lives. How to fix: change the namespace line to the folder's namespace.");
        }

        private static List<CompiledClass> ClassesCompiledFrom(IEnumerable<CompiledClass> classes, Func<string, bool> isWanted)
            => [.. classes.Where(compiled => compiled.Files.Any(isWanted))];

        private static string NamespaceOfFolder(string folder)
            => folder.Length == 0 ? TestRootNamespace : $"{TestRootNamespace}.{folder.Replace('/', '.')}";

        private static string FolderOf(string path)
        {
            var lastSlash = path.LastIndexOf('/');
            return lastSlash < 0 ? string.Empty : path[..lastSlash];
        }

        private static string FileNameOf(string path) => path[(path.LastIndexOf('/') + 1)..];

        private static List<string> ReadTestSourcePaths()
        {
            var projectRoot = TestProjectRoot();

            var paths = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(projectRoot, file).Replace('\\', '/'))
                .Where(path => !IsBuildOutput(path))
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(paths, Does.Contain(ThisFile), $"The test source scan of {projectRoot} did not find this very file, so it is not looking at the test project.");
            return paths;
        }

        private static List<CompiledClass> ReadCompiledTestClasses()
        {
            var assemblyPath = typeof(FlowMetricsUnitTestPlacementTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                ClassesCompiledFrom(classes, path => path == ThisFile).Select(compiled => compiled.FullName),
                Does.Contain(typeof(FlowMetricsUnitTestPlacementTest).FullName),
                $"The symbols of {assemblyPath} did not tie this very class to {ThisFile}, so no class could be tied to its file.");
            return classes;
        }

        // The files a class was compiled from, read from the symbols of its methods and of the types nested in it, such
        // as the state machines the compiler writes for async test methods.
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

        private static string TestProjectRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the test source scan.");
            return Path.Combine(directory!.FullName, TestProjectDirectory);
        }

        private sealed record CompiledClass(string FullName, string Namespace, IReadOnlyList<string> Files);

        private sealed record Misplacement(string Path, string What)
        {
            public override string ToString() => $"{Path} {What}";
        }

        private sealed record NamespaceFolderMismatch(string FullName, string Namespace, string Path, string FolderNamespace)
        {
            public override string ToString() => $"{FullName} is in namespace {Namespace} but its file {Path} sits in the folder of namespace {FolderNamespace}";
        }
    }

    internal static class ACompliantTwinOfThePlacementRule
    {
        public static int Answer() => 42;
    }
}

namespace Lighthouse.Backend.Tests.PlacementFixtures.Modules.FlowMetrics.Domain
{
    internal static class ACalculatorTestDeclaredOutsideItsFolder
    {
        public static int Answer() => 42;
    }
}
