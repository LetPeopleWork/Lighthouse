using Lighthouse.Backend.Tests.PlacementFixtures.Modules.Refinement.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The unit tests of Refinement sit next to the module: under Modules/Refinement in this project, in the folder of the
    /// module layer their subject lives in (the layer the membership map gives it), in the namespace of that folder, and
    /// with their class names kept. The folders the Refinement code left hold no test any more, and the acceptance tests at
    /// the port, the wire-contract tests, the model tests, the usage-data tests and the sizing-log repository test stay where
    /// they are. Files are found on disk, and the classes are read from the compiled test assembly together with its
    /// symbols, which say which file each class was compiled from. Each rule also runs on made-up trees, so a test left in
    /// an old folder, a copy left behind, a file in the wrong layer or a staying test that moved must be reported, and a
    /// file in its place must not be.
    /// </summary>
    [TestFixture]
    public class RefinementUnitTestsBesideTheModuleArchUnitTest
    {
        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ModuleFolder = "Modules/Refinement";

        private const string MadeUpModuleTestFolder = "PlacementFixtures/Modules/Refinement";

        private const string ThisFile = "Architecture/RefinementUnitTestsBesideTheModuleArchUnitTest.cs";

        private const string AMadeUpTestFile = "AMadeUpCalculatorTest";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        // The type each Refinement unit test is about; its file is that name followed by Test, and the layer comes from the map.
        private static readonly string[] UnitTestSubjects =
        [
            "NeedBand",
            "RefinementCadenceCalendar",
            "RefinementCalendar",
            "RefinementList",
            "RefinementNeedCalculator",
            "RefinementResolution",
            "RefinementViewQuery",
            "SizingLogCommands",
            "SleYardstickResolver",
            "StageRuleMatcher",
            "VoterIdentityResolver",
        ];

        // Tests of Refinement that stay where they are: the acceptance tests at the port, the wire-contract tests and the
        // model tests as whole folders, the usage-data tests and the sizing-log repository test as single files.
        private static readonly string[] StayingFolders =
        [
            "API/Integration/Refinement",
            "Contracts/Refinement",
            "Models/Refinement",
        ];

        private static readonly string[] StayingFiles =
        [
            "Integration/UsageData/TeamRefinementNeedUsageEventsTests.cs",
            "Integration/UsageData/TeamRefinementUsageEventsTests.cs",
            "Services/Implementation/Repositories/SizingLogRepositoryTest.cs",
        ];

        private static readonly Lazy<List<string>> TestSourcePaths = new(() => ReadSourcePaths(TestProjectDirectory, ThisFile));

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static IEnumerable<TestCaseData> EveryRefinementUnitTest()
            => UnitTestSubjects.Select(subject => new TestCaseData(subject).SetArgDisplayNames($"{subject}Test"));

        private static IEnumerable<TestCaseData> MadeUpTestTrees()
        {
            var moved = $"{ModuleFolder}/Application/{AMadeUpTestFile}.cs";
            var leftBehind = $"{OldTestFolders()[0]}/{AMadeUpTestFile}.cs";
            var inTheWrongLayer = $"{ModuleFolder}/Domain/{AMadeUpTestFile}.cs";
            var aNeighbour = $"{ModuleFolder}/Application/ANeighbourTest.cs";
            var anAcceptanceTest = $"{StayingFolders[0]}/AnAcceptanceTest.cs";

            yield return new TestCaseData(new[] { leftBehind }, new[] { moved, leftBehind }).SetArgDisplayNames("left in its old place");
            yield return new TestCaseData(new[] { moved, leftBehind }, new[] { leftBehind }).SetArgDisplayNames("moved with a copy left behind");
            yield return new TestCaseData(new[] { inTheWrongLayer }, new[] { moved, inTheWrongLayer }).SetArgDisplayNames("moved into the wrong layer");
            yield return new TestCaseData(new[] { moved, aNeighbour, anAcceptanceTest }, Array.Empty<string>()).SetArgDisplayNames("moved into its layer, with the acceptance tests left where they are");
        }

        private static IEnumerable<TestCaseData> MadeUpTreesOfTheOldFolders()
        {
            var inTheFirstOldFolder = $"{OldTestFolders()[0]}/ANewcomerTest.cs";
            var inTheLastOldFolder = $"{OldTestFolders()[^1]}/ANewcomerTest.cs";
            var inTheModule = $"{ModuleFolder}/Application/ANewcomerTest.cs";
            var anAcceptanceTest = $"{StayingFolders[0]}/AnAcceptanceTest.cs";
            var aFolderWithASimilarName = $"{OldTestFolders()[0]}Neighbour/AThingTest.cs";

            yield return new TestCaseData(new[] { inTheFirstOldFolder, inTheModule }, new[] { inTheFirstOldFolder }).SetArgDisplayNames("a test in an old folder");
            yield return new TestCaseData(new[] { inTheFirstOldFolder, inTheLastOldFolder }, new[] { inTheFirstOldFolder, inTheLastOldFolder }).SetArgDisplayNames("tests in every old folder");
            yield return new TestCaseData(new[] { inTheModule, anAcceptanceTest, aFolderWithASimilarName }, Array.Empty<string>()).SetArgDisplayNames("module tests, acceptance tests and a similar folder only");
        }

        private static IEnumerable<TestCaseData> MadeUpModuleTrees()
        {
            var aUnitTest = $"{ModuleFolder}/Application/{UnitTestSubjects[1]}Test.cs";
            var anAcceptanceTestMovedIn = $"{ModuleFolder}/Application/AnAcceptanceTest.cs";
            var aWireContractTestMovedIn = $"{ModuleFolder}/Domain/AWireContractTest.cs";
            var aTestOutsideTheModule = $"{StayingFolders[1]}/AWireContractTest.cs";

            yield return new TestCaseData(new[] { aUnitTest, aTestOutsideTheModule }, Array.Empty<string>()).SetArgDisplayNames("only unit tests in the module");
            yield return new TestCaseData(new[] { aUnitTest, anAcceptanceTestMovedIn, aWireContractTestMovedIn }, new[] { anAcceptanceTestMovedIn, aWireContractTestMovedIn })
                .SetArgDisplayNames("an acceptance test and a wire-contract test moved into the module");
        }

        private static IEnumerable<TestCaseData> MadeUpStayingTrees()
        {
            var everyone = StayingFolders.Select(folder => $"{folder}/AStayingTest.cs").Concat(StayingFiles).ToArray();

            yield return new TestCaseData(everyone, Array.Empty<string>()).SetArgDisplayNames("every staying test where it is");
            yield return new TestCaseData(everyone.Skip(1).ToArray(), new[] { StayingFolders[0] }).SetArgDisplayNames("the acceptance tests gone");
            yield return new TestCaseData(everyone[..^1], new[] { StayingFiles[^1] }).SetArgDisplayNames("the repository test gone");
            yield return new TestCaseData(everyone.Where(path => !path.StartsWith(StayingFolders[2], StringComparison.Ordinal)).ToArray(), new[] { StayingFolders[2] }).SetArgDisplayNames("the model tests gone");
        }

        private static IEnumerable<TestCaseData> MadeUpClassesCompiledFromATestFile()
        {
            var inItsLayer = $"{TestRootNamespace}.Modules.Refinement.Application.{AMadeUpTestFile}";
            var aHelperBesideIt = $"{TestRootNamespace}.Modules.Refinement.Application.AHelperBesideIt";
            var withAStaleNamespace = $"{TestRootNamespace}.{OldTestFolders()[0].Replace('/', '.')}.{AMadeUpTestFile}";
            var renamed = $"{TestRootNamespace}.Modules.Refinement.Application.ARenamedCalculatorTest";

            yield return new TestCaseData(new[] { inItsLayer, aHelperBesideIt }, Array.Empty<string>()).SetArgDisplayNames("kept its name in its layer namespace");
            yield return new TestCaseData(new[] { withAStaleNamespace }, new[] { withAStaleNamespace, inItsLayer }).SetArgDisplayNames("kept a stale namespace");
            yield return new TestCaseData(new[] { renamed }, new[] { inItsLayer }).SetArgDisplayNames("renamed its class");
            yield return new TestCaseData(Array.Empty<string>(), new[] { $"{AMadeUpTestFile}.cs" }).SetArgDisplayNames("compiled into no class");
        }

        [Test]
        public void TheRefinementUnitTestsAreElevenFilesAboutTypesOnTheMap()
        {
            var unknown = UnitTestSubjects.Where(subject => RefinementMembership.MovingTypes.All(type => type.Name != subject)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    UnitTestSubjects,
                    Has.Length.EqualTo(11).And.Unique,
                    $"The list of Refinement unit tests holds {UnitTestSubjects.Length} entries. Why it matters: the move is judged against the eleven unit tests that exist, and a shorter or doubled list would let one stay behind unseen. How to fix: list each of the eleven once.");
                Assert.That(
                    unknown,
                    Is.Empty,
                    $"These test subjects are not types of the membership map: [{string.Join(", ", unknown)}]. Why it matters: the layer of a test comes from the map, so a subject the map does not know has no folder to move to. How to fix: name a type the map lists as moving.");
            }
        }

        [TestCaseSource(nameof(EveryRefinementUnitTest))]
        public void ARefinementUnitTest_SitsOnlyInItsModuleLayerFolder_AndKeepsItsClassInThatLayer(string subject)
        {
            var layer = LayerFolderOf(subject);
            var testFile = $"{subject}Test";

            var misplacedFiles = FilePlacementProblems(testFile, layer, TestSourcePaths.Value);
            var misplacedClasses = ClassPlacementProblems(testFile, layer, CompiledTestClasses.Value);

            ThenNothingIsMisplaced(testFile, layer, misplacedFiles.Concat(misplacedClasses));
        }

        [Test]
        public void NoRefinementTestIsLeftInTheFoldersTheRefinementCodeLeft()
        {
            var left = TestFilesInTheOldFolders(TestSourcePaths.Value);

            Assert.That(
                left,
                Is.Empty,
                $"These test files still sit in a folder the Refinement code left: [{string.Join(", ", left)}]. Why it matters: a Refinement unit test written into an old folder splits the context again, and the old folder is meant to end empty. How to fix: git mv the file into the layer folder of its subject under {ModuleFolder} and change its namespace to that folder's.");
        }

        [Test]
        public void EveryClassOfTheRefinementModuleTests_HasTheNamespaceOfItsFolder()
        {
            var moduleTestClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, ModuleFolder);

            Assert.That(
                moduleTestClasses,
                Is.Not.Empty,
                $"The compiled tests hold no class from a file under {ModuleFolder} or in its namespace. Why it matters: the Refinement unit tests belong next to the module, and a folder rule over an empty folder passes without looking at anything. How to fix: move the Refinement unit-test files into the module layer folders under {ModuleFolder} with git mv.");

            ThenEachClassHasTheNamespaceOfItsFolder(moduleTestClasses);
        }

        [Test]
        public void TheRefinementModuleTestFolder_HoldsTheElevenUnitTestsAndNothingElse()
        {
            var strangers = FilesInTheModuleOtherThanTheUnitTests(TestSourcePaths.Value);

            Assert.That(
                strangers,
                Is.Empty,
                $"These files under {ModuleFolder} are not one of the eleven Refinement unit tests in their layer folder: [{string.Join(", ", strangers)}]. Why it matters: the acceptance, wire-contract, model and usage-data tests stay where they are, and one moved into the module is a move this change was not asked for. How to fix: git mv the file back to its old place, or, for a unit test, into the layer folder of its subject.");
        }

        [Test]
        public void TheAcceptanceWireContractModelAndUsageDataTestsAndTheSizingLogRepositoryTest_StayWhereTheyAre()
        {
            var problems = StayingTestProblems(TestSourcePaths.Value);
            var classesMoved = ClassesFromStayingFilesInTheModuleNamespace(CompiledTestClasses.Value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    problems,
                    Is.Empty,
                    $"[{string.Join("; ", problems)}]. Why it matters: only the eleven unit tests move, and a staying test that is gone from its place was moved or lost. How to fix: git mv it back to where it was.");
                Assert.That(
                    classesMoved,
                    Is.Empty,
                    $"These classes of staying tests sit in the module's test namespace: [{string.Join(", ", classesMoved)}]. Why it matters: the acceptance, wire-contract, model and usage-data tests keep their namespaces; only their using lines change. How to fix: change the namespace line back to the one of the folder the file sits in.");
            }
        }

        [TestCaseSource(nameof(MadeUpTestTrees))]
        public void AMadeUpTestFileOutsideItsLayerFolder_IsReported_AndOneThatSitsThereIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var problems = FilePlacementProblems(AMadeUpTestFile, "Application", sourcePaths);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the placement rule reported [{string.Join("; ", problems)}]. Why it matters: a test file still in its old place, a copy left behind or a file in the wrong layer folder must each be named, and a file in its layer folder must not be, nor the acceptance tests that stay where they are. How to fix: report the expected path when it holds no file, and every other path that holds a file of the same name.");
        }

        [TestCaseSource(nameof(MadeUpTreesOfTheOldFolders))]
        public void AMadeUpTestInAnOldRefinementFolder_IsReported_AndTestsElsewhereAreNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var left = TestFilesInTheOldFolders(sourcePaths);

            Assert.That(
                left,
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join(", ", left)}]. Why it matters: every file in a folder the Refinement code left must be named, whatever it is called, and a file in the module, in a staying folder or in a folder that only starts alike must not be. How to fix: report each file whose folder is one of the old folders the map gives.");
        }

        [TestCaseSource(nameof(MadeUpModuleTrees))]
        public void AMadeUpStrangerInTheModuleTestFolder_IsReported_AndTheUnitTestsAreNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var strangers = FilesInTheModuleOtherThanTheUnitTests(sourcePaths);

            Assert.That(
                strangers,
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join(", ", strangers)}]. Why it matters: a moved acceptance or wire-contract test must be named, and a unit test in its layer folder must not be. How to fix: report each file under the module folder that is not one of the eleven unit tests in the layer folder of its subject.");
        }

        [TestCaseSource(nameof(MadeUpStayingTrees))]
        public void AMadeUpStayingTestThatIsGone_IsReported_AndOneWhereItStaysIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var problems = StayingTestProblems(sourcePaths);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join("; ", problems)}]. Why it matters: a staying folder with no file or a staying file that is gone must be named, and a complete tree must not be. How to fix: report each staying folder without a file and each staying file that does not exist.");
        }

        [TestCaseSource(nameof(MadeUpClassesCompiledFromATestFile))]
        public void AMadeUpTestFileWhoseClassLostItsNameOrLayerNamespace_IsReported_AndOneThatKeptThemIsNot(string[] classFullNames, string[] expectedReported)
        {
            var theFile = $"{ModuleFolder}/Application/{AMadeUpTestFile}.cs";
            var classes = classFullNames
                .Select(fullName => new CompiledClass(fullName, fullName[..fullName.LastIndexOf('.')], [theFile]))
                .ToList();

            var problems = ClassPlacementProblems(AMadeUpTestFile, "Application", classes);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReported),
                $"For the classes [{string.Join(", ", classFullNames)}] compiled from {theFile} the rule reported [{string.Join("; ", problems)}]. Why it matters: a moved test keeps its class name and takes the namespace of its layer folder, so a test filter and a reader still find it under the same name; a stale namespace, a renamed class or a file that compiles into nothing must each be named, and a helper class beside the test must not be. How to fix: report each class out of the layer namespace, the expected class name when no class carries it, and the file when it compiles into no class.");
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
                    Does.Contain(typeof(ACompliantTwinOfTheRefinementPlacementRule).FullName).And.Contain(typeof(ARefinementTestDeclaredOutsideItsFolder).FullName),
                    $"The compiled tests do not tie both made-up classes to {ThisFile}. Why it matters: a twin the rule never looked at proves nothing by passing. How to fix: read every class with the files its methods were compiled from.");

                Assert.That(
                    madeUpModuleClasses.Select(compiled => compiled.FullName),
                    Is.EqualTo(new[] { typeof(ARefinementTestDeclaredOutsideItsFolder).FullName }),
                    $"The classes taken as tests of the made-up module {MadeUpModuleTestFolder} were [{string.Join(", ", madeUpModuleClasses.Select(compiled => compiled.FullName))}]. Why it matters: a newcomer declared in the module's namespace from a file outside the module folder must still be looked at, or it slips past the folder rule. How to fix: take every class that is compiled from a file under the module folder or declared in the module's namespace.");

                Assert.That(
                    mismatches.Select(mismatch => mismatch.FullName),
                    Is.EqualTo(new[] { typeof(ARefinementTestDeclaredOutsideItsFolder).FullName }),
                    $"Expected exactly {typeof(ARefinementTestDeclaredOutsideItsFolder).FullName} to be reported, got [{string.Join("; ", mismatches)}]. Why it matters: a class whose namespace names a module layer while its file sits elsewhere hides where the test really lives, and a class whose namespace matches its folder must pass, or the rule flags everything. How to fix: compare each class's namespace with the namespace its folder gives.");
            }
        }

        private static string LayerFolderOf(string subject)
            => RefinementMembership.MovingTypes.Single(type => type.Name == subject).Layer.ToString();

        // The test-project folders that mirror the namespaces the Refinement code lived in, read from the map.
        private static List<string> OldTestFolders()
            => [.. RefinementMembership.MovingTypes
                .Select(type => type.OldNamespace[(TestRootNamespace.LastIndexOf('.') + 1)..].Replace('.', '/'))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];

        private static List<string> TestFilesInTheOldFolders(IEnumerable<string> testPaths)
        {
            var oldFolders = OldTestFolders();
            return [.. testPaths.Where(path => oldFolders.Any(folder => path.StartsWith($"{folder}/", StringComparison.Ordinal)))];
        }

        private static List<string> ExpectedUnitTestPaths()
            => [.. UnitTestSubjects.Select(subject => $"{ModuleFolder}/{LayerFolderOf(subject)}/{subject}Test.cs")];

        private static List<string> FilesInTheModuleOtherThanTheUnitTests(IEnumerable<string> testPaths)
        {
            var expected = ExpectedUnitTestPaths();
            return [.. testPaths
                .Where(path => path.StartsWith($"{ModuleFolder}/", StringComparison.Ordinal))
                .Where(path => !expected.Contains(path, StringComparer.Ordinal))];
        }

        private static List<Misplacement> StayingTestProblems(IReadOnlyCollection<string> testPaths)
        {
            var emptyFolders = StayingFolders
                .Where(folder => !testPaths.Any(path => path.StartsWith($"{folder}/", StringComparison.Ordinal)))
                .Select(folder => new Misplacement(folder, "holds no test file any more"));

            var goneFiles = StayingFiles
                .Where(file => !testPaths.Contains(file, StringComparer.Ordinal))
                .Select(file => new Misplacement(file, "is gone from where it stays"));

            return [.. emptyFolders.Concat(goneFiles)];
        }

        private static List<string> ClassesFromStayingFilesInTheModuleNamespace(IEnumerable<CompiledClass> classes)
        {
            var moduleNamespace = NamespaceOfFolder(ModuleFolder);

            return [.. classes
                .Where(compiled => compiled.Files.Any(IsAStayingFile))
                .Where(compiled => compiled.Namespace == moduleNamespace || compiled.Namespace.StartsWith($"{moduleNamespace}.", StringComparison.Ordinal))
                .Select(compiled => compiled.FullName)];
        }

        private static bool IsAStayingFile(string path)
            => StayingFiles.Contains(path, StringComparer.Ordinal)
               || StayingFolders.Any(folder => path.StartsWith($"{folder}/", StringComparison.Ordinal));

        private static List<Misplacement> FilePlacementProblems(string testFile, string layer, IReadOnlyCollection<string> sourcePaths)
        {
            var fileName = $"{testFile}.cs";
            var expectedPath = $"{ModuleFolder}/{layer}/{fileName}";
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
            var expectedNamespace = NamespaceOfFolder($"{ModuleFolder}/{layer}");
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
                $"{testFile} should sit only in {ModuleFolder}/{layer} with its class {testFile} and every other class in it in namespace {NamespaceOfFolder($"{ModuleFolder}/{layer}")}; instead: [{string.Join("; ", found)}]. Why it matters: the Refinement unit tests sit next to the module only when each one is in the folder of the layer its subject lives in, with the namespace of that folder, its class name kept and no copy left in its old place. How to fix: git mv the file into that folder and change its namespace and using lines; keep the class names.");
        }

        private static void ThenEachClassHasTheNamespaceOfItsFolder(IEnumerable<CompiledClass> classes)
        {
            var mismatches = NamespaceFolderMismatches(classes);

            Assert.That(
                mismatches,
                Is.Empty,
                $"These classes do not have the namespace of the folder their file sits in: [{string.Join("; ", mismatches)}]. Why it matters: a reader and a test filter find a test by its namespace, so a namespace that disagrees with the folder hides where the test lives. How to fix: move the file into the folder its namespace names, or change the namespace line to the folder's namespace.");
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

        private static List<string> ReadSourcePaths(string projectDirectory, string aFileItHolds)
        {
            var projectRoot = Path.Combine(SolutionRoot(), projectDirectory);

            var paths = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(projectRoot, file).Replace('\\', '/'))
                .Where(path => !IsBuildOutput(path))
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(paths, Does.Contain(aFileItHolds), $"The source scan of {projectRoot} did not find {aFileItHolds}, so it is not looking at that project.");
            return paths;
        }

        private static List<CompiledClass> ReadCompiledTestClasses()
        {
            var assemblyPath = typeof(RefinementUnitTestsBesideTheModuleArchUnitTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                ClassesCompiledFrom(classes, path => path == ThisFile).Select(compiled => compiled.FullName),
                Does.Contain(typeof(RefinementUnitTestsBesideTheModuleArchUnitTest).FullName),
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

        private sealed record Misplacement(string Path, string What)
        {
            public override string ToString() => $"{Path} {What}";
        }

        private sealed record NamespaceFolderMismatch(string FullName, string Namespace, string Path, string FolderNamespace)
        {
            public override string ToString() => $"{FullName} is in namespace {Namespace} but its file {Path} sits in the folder of namespace {FolderNamespace}";
        }
    }

    internal static class ACompliantTwinOfTheRefinementPlacementRule
    {
        public static int Answer() => 42;
    }
}

namespace Lighthouse.Backend.Tests.PlacementFixtures.Modules.Refinement.Application
{
    internal static class ARefinementTestDeclaredOutsideItsFolder
    {
        public static int Answer() => 42;
    }
}
