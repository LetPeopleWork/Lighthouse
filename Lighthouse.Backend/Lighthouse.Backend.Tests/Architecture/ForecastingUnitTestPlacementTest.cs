using Lighthouse.Backend.Tests.PlacementFixtures.Modules.Forecasting.Domain;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The unit tests of the Forecasting module sit next to it: under Modules/Forecasting in this project, in the folder
    /// of the module layer their subject lives in, in the namespace of that folder, and with their class names kept. The
    /// test files are found on disk, and the classes are read from the compiled test assembly together with its symbols,
    /// which say which file each class was compiled from, so no source text is pinned. Each rule also runs on made-up
    /// inputs: a test file left in its old place, a copy left behind, a file in the wrong layer, a newcomer in the folder
    /// the Forecasting tests left, a class with a stale namespace or a new name, and a class declared in the module's
    /// namespace from a file outside its folder must each be reported, and a compliant twin must not be.
    /// </summary>
    [TestFixture]
    public class ForecastingUnitTestPlacementTest
    {
        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ModuleTestFolder = "Modules/Forecasting";

        // Every test in this folder was a Forecasting test, so once they have moved it holds nothing.
        private const string FolderTheForecastingTestsLeft = "Services/Implementation/Forecast";

        private const string MadeUpModuleTestFolder = "PlacementFixtures/Modules/Forecasting";

        private const string ThisFile = "Architecture/ForecastingUnitTestPlacementTest.cs";

        private const string AMadeUpTestFile = "AMadeUpDrawStreamTest";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        // Each Forecasting unit-test file, without its .cs, and the module layer folder its subject lives in.
        private static readonly (string TestFile, string Layer)[] ForecastingUnitTestFiles =
        [
            ("AddressableDrawStreamTest", "Domain"),
            ("ForecastRunThatCannotEndTest", "Domain"),
            ("RealityCheckVerdictPolicyTest", "Domain"),
            ("ForecastBeyondTheHorizonTest", "Application"),
            ("ForecastDrawCoordinatesTest", "Application"),
            ("ForecastFilterFeatureForecastIntegrationTest", "Application"),
            ("ForecastFilterRuleServiceIntegrationTest", "Application"),
            ("ForecastInputFingerprintTest", "Application"),
            ("ForecastLaneTest", "Application"),
            ("ForecastRealityCheckServiceTest", "Application"),
            ("ForecastServiceCrossTeamDependencyTest", "Application"),
            ("ForecastServiceDependencyTest", "Application"),
            ("ForecastServiceSchedulerTest", "Application"),
            ("ForecastServiceStartDayTest", "Application"),
            ("ForecastServiceTest", "Application"),
            ("ForecastWorkerBoundTest", "Application"),
            ("MultiTeamJointForecastTest", "Application"),
            ("OnePortfolioForecast", "Application"),
            ("FeatureOrderingPolicyChangedForecastTriggerHandlerTest", "Application/Policies"),
            ("FeatureRankChangedForecastTriggerHandlerTest", "Application/Policies"),
            ("TeamDataRefreshedForecastTriggerHandlerTest", "Application/Policies"),
            ("TeamDeletedForecastRetriggerHandlerTest", "Application/Policies"),
            ("ForecastUpdaterTest", "Adapters/Background"),
            ("ForecastInputsForgottenAtStartSeederTests", "Adapters/Background"),
        ];

        private static readonly Lazy<List<string>> TestSourcePaths = new(ReadTestSourcePaths);

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static IEnumerable<TestCaseData> EveryForecastingUnitTestFile()
            => ForecastingUnitTestFiles.Select(entry => new TestCaseData(entry.TestFile, entry.Layer).SetArgDisplayNames(entry.TestFile, entry.Layer));

        private static IEnumerable<TestCaseData> MadeUpTestTrees()
        {
            const string moved = $"{ModuleTestFolder}/Domain/{AMadeUpTestFile}.cs";
            const string leftBehind = $"{FolderTheForecastingTestsLeft}/{AMadeUpTestFile}.cs";
            const string inTheWrongLayer = $"{ModuleTestFolder}/Application/{AMadeUpTestFile}.cs";
            const string aNeighbour = $"{ModuleTestFolder}/Domain/ANeighbourTest.cs";
            const string aSharedUpdateTest = "Services/Implementation/BackgroundServices/Update/UpdateServiceTestBase.cs";
            const string anIntegrationTest = "API/Integration/AForecastControllerIntegrationTest.cs";

            yield return new TestCaseData(new[] { leftBehind }, new[] { moved, leftBehind }).SetArgDisplayNames("left in its old place");
            yield return new TestCaseData(new[] { moved, leftBehind }, new[] { leftBehind }).SetArgDisplayNames("moved with a copy left behind");
            yield return new TestCaseData(new[] { inTheWrongLayer }, new[] { moved, inTheWrongLayer }).SetArgDisplayNames("moved into the wrong layer");
            yield return new TestCaseData(new[] { moved, aNeighbour, aSharedUpdateTest, anIntegrationTest }, Array.Empty<string>()).SetArgDisplayNames("moved into its layer, with the shared and integration tests left where they are");
        }

        private static IEnumerable<TestCaseData> MadeUpTreesAroundTheFolderTheTestsLeft()
        {
            const string aNewcomer = $"{FolderTheForecastingTestsLeft}/ANewcomerForecastTest.cs";
            const string aNewcomerInASubfolder = $"{FolderTheForecastingTestsLeft}/Simulation/ANewcomerSimulationTest.cs";
            const string aFolderWithASimilarName = "Services/Implementation/ForecastingNeighbour/ANeighbourTest.cs";
            const string aSharedUpdateTest = "Services/Implementation/BackgroundServices/Update/PortfolioUpdaterTest.cs";
            const string aModuleTest = $"{ModuleTestFolder}/Application/ANewcomerForecastTest.cs";

            yield return new TestCaseData(new[] { aNewcomer, aModuleTest }, new[] { aNewcomer }).SetArgDisplayNames("a newcomer in the folder the tests left");
            yield return new TestCaseData(new[] { aNewcomerInASubfolder }, new[] { aNewcomerInASubfolder }).SetArgDisplayNames("a newcomer in a subfolder of it");
            yield return new TestCaseData(new[] { aFolderWithASimilarName, aSharedUpdateTest, aModuleTest }, Array.Empty<string>()).SetArgDisplayNames("tests in other folders only");
        }

        private static IEnumerable<TestCaseData> MadeUpClassesCompiledFromATestFile()
        {
            const string inItsLayer = $"{TestRootNamespace}.Modules.Forecasting.Domain.{AMadeUpTestFile}";
            const string aHelperBesideIt = $"{TestRootNamespace}.Modules.Forecasting.Domain.AHelperBesideIt";
            const string withAStaleNamespace = $"{TestRootNamespace}.Services.Implementation.Forecast.{AMadeUpTestFile}";
            const string renamed = $"{TestRootNamespace}.Modules.Forecasting.Domain.ARenamedDrawStreamTest";

            yield return new TestCaseData(new[] { inItsLayer, aHelperBesideIt }, Array.Empty<string>()).SetArgDisplayNames("kept its name in its layer namespace");
            yield return new TestCaseData(new[] { withAStaleNamespace }, new[] { withAStaleNamespace, inItsLayer }).SetArgDisplayNames("kept a stale namespace");
            yield return new TestCaseData(new[] { renamed }, new[] { inItsLayer }).SetArgDisplayNames("renamed its class");
            yield return new TestCaseData(Array.Empty<string>(), new[] { $"{AMadeUpTestFile}.cs" }).SetArgDisplayNames("compiled into no class");
        }

        [TestCaseSource(nameof(EveryForecastingUnitTestFile))]
        public void AForecastingUnitTestFile_SitsOnlyInItsModuleLayerFolder_AndKeepsItsClassInThatLayer(string testFile, string layer)
        {
            var misplacedFiles = FilePlacementProblems(testFile, layer, TestSourcePaths.Value);
            var misplacedClasses = ClassPlacementProblems(testFile, layer, CompiledTestClasses.Value);

            ThenNothingIsMisplaced(testFile, layer, misplacedFiles.Concat(misplacedClasses));
        }

        [Test]
        public void TheFolderTheForecastingTestsLeft_HoldsNoTestFile()
        {
            var leftOver = FilesInTheFolderTheTestsLeft(TestSourcePaths.Value);

            Assert.That(
                leftOver,
                Is.Empty,
                $"{FolderTheForecastingTestsLeft} still holds [{string.Join(", ", leftOver)}]. Why it matters: that folder held only " +
                "Forecasting tests, and a test that sits there, moved or new, is a Forecasting test outside the module. How to " +
                $"fix: git mv each file into the layer folder of its subject under {ModuleTestFolder}.");
        }

        [Test]
        public void EveryClassOfTheForecastingModuleTests_HasTheNamespaceOfItsFolder()
        {
            var moduleTestClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, ModuleTestFolder);

            Assert.That(
                moduleTestClasses,
                Is.Not.Empty,
                $"The compiled tests hold no class from a file under {ModuleTestFolder} or in its namespace. Why it matters: the " +
                "Forecasting unit tests belong next to the module, and a folder rule over an empty folder passes without looking " +
                $"at anything. How to fix: move the Forecasting unit-test files into the module layer folders under {ModuleTestFolder} with git mv.");

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
                "each be named, and a file that sits in its layer folder must not be, nor the shared and integration tests that " +
                "stay where they are, or the rule cannot tell a finished move from an unfinished one. How to fix: report the " +
                "expected path when it holds no file, and every other path that holds a file of the same name.");
        }

        [TestCaseSource(nameof(MadeUpTreesAroundTheFolderTheTestsLeft))]
        public void AMadeUpTestFileInTheFolderTheForecastingTestsLeft_IsReported_AndOneElsewhereIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var leftOver = FilesInTheFolderTheTestsLeft(sourcePaths);

            Assert.That(
                leftOver,
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join(", ", leftOver)}]. Why it " +
                $"matters: a test written into {FolderTheForecastingTestsLeft} or below it after the move is a Forecasting test outside " +
                "the module and must be named, while tests in other folders, including one whose name only starts the same, must " +
                "not be. How to fix: report every path inside that folder and no other.");
        }

        [TestCaseSource(nameof(MadeUpClassesCompiledFromATestFile))]
        public void AMadeUpTestFileWhoseClassLostItsNameOrLayerNamespace_IsReported_AndOneThatKeptThemIsNot(string[] classFullNames, string[] expectedReported)
        {
            const string theFile = $"{ModuleTestFolder}/Domain/{AMadeUpTestFile}.cs";
            var classes = classFullNames
                .Select(fullName => new CompiledClass(fullName, fullName[..fullName.LastIndexOf('.')], [theFile]))
                .ToList();

            var problems = ClassPlacementProblems(AMadeUpTestFile, "Domain", classes);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReported),
                $"For the classes [{string.Join(", ", classFullNames)}] compiled from {theFile} the rule reported " +
                $"[{string.Join("; ", problems)}]. Why it matters: a moved test keeps its class name and takes the namespace of its " +
                "layer folder, so a test filter and a reader still find it under the same name; a stale namespace, a renamed class " +
                "or a file that compiles into nothing must each be named, and a helper class beside the test must not be. How to " +
                "fix: report each class out of the layer namespace, the expected class name when no class carries it, and the file " +
                "when it compiles into no class.");
        }

        [Test]
        public void AMadeUpClassDeclaredInTheModuleNamespaceOutsideItsFolder_IsReported_AndItsCompliantTwinIsNot()
        {
            var classesInThisFile = ClassesCompiledFrom(CompiledTestClasses.Value, path => path == ThisFile);
            var madeUpModuleClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, MadeUpModuleTestFolder);

            var mismatches = NamespaceFolderMismatches(classesInThisFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    classesInThisFile.Select(compiled => compiled.FullName),
                    Does.Contain(typeof(ACompliantTwinOfTheForecastingPlacementRule).FullName).And.Contain(typeof(AForecastTestDeclaredOutsideItsFolder).FullName),
                    $"The compiled tests do not tie both made-up classes to {ThisFile}. Why it matters: a twin the rule never looked " +
                    "at proves nothing by passing. How to fix: read every class with the files its methods were compiled from.");

                Assert.That(
                    madeUpModuleClasses.Select(compiled => compiled.FullName),
                    Is.EqualTo(new[] { typeof(AForecastTestDeclaredOutsideItsFolder).FullName }),
                    $"The classes taken as tests of the made-up module {MadeUpModuleTestFolder} were " +
                    $"[{string.Join(", ", madeUpModuleClasses.Select(compiled => compiled.FullName))}]. Why it matters: a newcomer " +
                    "declared in the module's namespace from a file outside the module folder must still be looked at, or it slips " +
                    "past the folder rule. How to fix: take every class that is compiled from a file under the module folder or " +
                    "declared in the module's namespace.");

                Assert.That(
                    mismatches.Select(mismatch => mismatch.FullName),
                    Is.EqualTo(new[] { typeof(AForecastTestDeclaredOutsideItsFolder).FullName }),
                    $"Expected exactly {typeof(AForecastTestDeclaredOutsideItsFolder).FullName} to be reported, got " +
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
            var expectedClass = $"{expectedNamespace}.{testFile}";
            var fromTheFile = ClassesCompiledFrom(classes, path => FileNameOf(path) == fileName);

            if (fromTheFile.Count == 0)
            {
                return [new Misplacement(fileName, "was compiled into no class")];
            }

            var problems = fromTheFile
                .Where(compiled => compiled.Namespace != expectedNamespace)
                .Select(compiled => new Misplacement(compiled.FullName, $"lives in namespace {compiled.Namespace} instead of {expectedNamespace}"))
                .ToList();

            if (!fromTheFile.Any(compiled => compiled.FullName == expectedClass))
            {
                problems.Add(new Misplacement(expectedClass, $"is not a class compiled from {fileName}"));
            }

            return problems;
        }

        private static List<string> FilesInTheFolderTheTestsLeft(IEnumerable<string> sourcePaths)
            => [.. sourcePaths.Where(path => path.StartsWith($"{FolderTheForecastingTestsLeft}/", StringComparison.Ordinal))];

        private static List<CompiledClass> ClassesOfTheModuleTests(IEnumerable<CompiledClass> classes, string moduleFolder)
        {
            var moduleNamespace = NamespaceOfFolder(moduleFolder);

            return [.. classes.Where(compiled =>
                compiled.Files.Any(path => path.StartsWith($"{moduleFolder}/", StringComparison.Ordinal))
                || compiled.Namespace == moduleNamespace
                || compiled.Namespace.StartsWith($"{moduleNamespace}.", StringComparison.Ordinal))];
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
                $"{testFile} should sit only in {ModuleTestFolder}/{layer} with its class {testFile} and every other class in it in " +
                $"namespace {NamespaceOfFolder($"{ModuleTestFolder}/{layer}")}; instead: [{string.Join("; ", found)}]. Why it matters: " +
                "the Forecasting unit tests sit next to the module only when each one is in the folder of the layer its subject " +
                "lives in, with the namespace of that folder, its class name kept and no copy left in its old place. How to fix: " +
                "git mv the file into that folder and change its namespace and using lines; keep the class names.");
        }

        private static void ThenEachClassHasTheNamespaceOfItsFolder(IEnumerable<CompiledClass> classes)
        {
            var mismatches = NamespaceFolderMismatches(classes);

            Assert.That(
                mismatches,
                Is.Empty,
                $"These classes do not have the namespace of the folder their file sits in: [{string.Join("; ", mismatches)}]. " +
                "Why it matters: a reader and a test filter find a test by its namespace, so a namespace that disagrees with the " +
                "folder hides where the test lives. How to fix: move the file into the folder its namespace names, or change the " +
                "namespace line to the folder's namespace.");
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
            var assemblyPath = typeof(ForecastingUnitTestPlacementTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                ClassesCompiledFrom(classes, path => path == ThisFile).Select(compiled => compiled.FullName),
                Does.Contain(typeof(ForecastingUnitTestPlacementTest).FullName),
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

    internal static class ACompliantTwinOfTheForecastingPlacementRule
    {
        public static int Answer() => 42;
    }
}

namespace Lighthouse.Backend.Tests.PlacementFixtures.Modules.Forecasting.Domain
{
    internal static class AForecastTestDeclaredOutsideItsFolder
    {
        public static int Answer() => 42;
    }
}
