using Lighthouse.Backend.Tests.PlacementFixtures.Modules.Promises.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The unit tests of Promises sit next to the module: under Modules/Promises in this project, in the folder of the
    /// module layer their subject lives in, in the namespace of that folder, and with their class names kept. A unit test
    /// of a Promises type found anywhere else is reported; the integration, acceptance, controller and architecture tests
    /// are not judged. No Promises source file is left under Services apart from the named exceptions, each with the owner
    /// that will place it, and an exception whose file is gone is reported too. Files are found on disk, and the classes
    /// are read from the compiled test assembly together with its symbols, which say which file each class was compiled
    /// from, so no source text is pinned. Each rule also runs on made-up trees, so a test or source file left in an old
    /// folder must be reported, and a file in its place or a file that only looks alike must not be.
    /// </summary>
    [TestFixture]
    public class PromisesUnitTestPlacementTest
    {
        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string ModuleFolder = "Modules/Promises";

        private const string MadeUpModuleTestFolder = "PlacementFixtures/Modules/Promises";

        private const string ThisFile = "Architecture/PromisesUnitTestPlacementTest.cs";

        private const string AMadeUpTestFile = "AMadeUpDeliveryRuleTest";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        // Tests under these roots go through a host, a controller or the compiled code as a whole; they test no single
        // Promises type and stay where they are.
        private static readonly string[] TestRootsNotJudged = ["API", "Acceptance", "Architecture", "Contracts", "Integration"];

        // The folders the Promises sources came from; nothing belongs in them any more.
        private static readonly string[] FoldersThePromisesSourcesLeft =
        [
            "Services/Implementation/DeliverySources",
            "Services/Interfaces/DeliverySources",
        ];

        // Each Promises unit-test file, without its .cs, and the module layer folder its subject lives in.
        private static readonly (string TestFile, string Layer)[] PromisesUnitTestFiles =
        [
            ("DeliveryForecastBlockRendererBeyondTheHorizonTest", "Application"),
            ("DeliveryForecastBlockRendererTest", "Application"),
            ("DeliveryForecastPublishingServiceTest", "Application"),
            ("DeliveryMetricValuesProjectorTest", "Application"),
            ("DeliveryRuleServiceTest", "Application"),
            ("DeliverySourceResolverTest", "Application"),
            ("DeliverySourceSyncServiceTest", "Application"),
            ("DeliveryForecastPublishingHandlerTest", "Application/Policies"),
            ("DeliveryMetricSnapshotRecordingHandlerTest", "Application/Policies"),
        ];

        // Source files under Services that carry a Promises name but deliberately stay, each with the owner that places it.
        private static readonly (string Path, string Owner)[] SourceFilesThatStay =
        [
            // The Delivery tables are read and written by the platform persistence until the repositories are split by context.
            ("Services/Implementation/Repositories/DeliveryRepository.cs", "platform persistence"),
            ("Services/Implementation/Repositories/DeliveryMetricSnapshotRepository.cs", "platform persistence"),
            ("Services/Interfaces/Repositories/IDeliveryRepository.cs", "platform persistence"),
            ("Services/Interfaces/Repositories/IDeliveryMetricSnapshotRepository.cs", "platform persistence"),
            ("Services/Interfaces/Repositories/RecordableDeliveries.cs", "platform persistence"),

            // The write-back round writes forecast dates and Team or Portfolio fields to a tracker; Forecasting triggers it,
            // so it cannot live in Promises, and its own placement is still to be decided.
            ("Services/Implementation/WriteBackCollector.cs", "write-back placement"),
            ("Services/Implementation/WriteBackRound.cs", "write-back placement"),
            ("Services/Implementation/WriteBackRoundContext.cs", "write-back placement"),
            ("Services/Implementation/WriteBackService.cs", "write-back placement"),
            ("Services/Implementation/WriteBackTriggerService.cs", "write-back placement"),
            ("Services/Interfaces/IWriteBackCollector.cs", "write-back placement"),
            ("Services/Interfaces/IWriteBackService.cs", "write-back placement"),
            ("Services/Interfaces/IWriteBackTriggerService.cs", "write-back placement"),

            // Feature order and rank are Organisation set-up's rules, not a promise; they are placed with that context.
        ];

        // Words that once put a source file into Promises: the Delivery word, and the write-back and ordering names the
        // Promises scope first held. The update code carries none of them and stays in its own folders.
        private static readonly string[] PromisesWordsInATypeName = ["Deliver"];

        private static readonly string[] PromisesTypeNamePrefixes = ["WriteBack", "IWriteBack"];

        private static readonly Lazy<List<string>> TestSourcePaths = new(() => ReadSourcePaths(TestProjectDirectory, ThisFile));

        private static readonly Lazy<List<string>> ProductionSourcePaths = new(() => ReadSourcePaths(ProductionProjectDirectory, "Program.cs"));

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static IEnumerable<TestCaseData> EveryPromisesUnitTestFile()
            => PromisesUnitTestFiles.Select(entry => new TestCaseData(entry.TestFile, entry.Layer).SetArgDisplayNames(entry.TestFile, entry.Layer));

        private static IEnumerable<TestCaseData> MadeUpTestTrees()
        {
            const string moved = $"{ModuleFolder}/Application/{AMadeUpTestFile}.cs";
            const string leftBehind = $"Services/Implementation/DeliverySources/{AMadeUpTestFile}.cs";
            const string inTheWrongLayer = $"{ModuleFolder}/Application/Policies/{AMadeUpTestFile}.cs";
            const string aNeighbour = $"{ModuleFolder}/Application/ANeighbourTest.cs";
            const string anIntegrationTest = "API/Integration/ADeliveryRuleIntegrationTest.cs";

            yield return new TestCaseData(new[] { leftBehind }, new[] { moved, leftBehind }).SetArgDisplayNames("left in its old place");
            yield return new TestCaseData(new[] { moved, leftBehind }, new[] { leftBehind }).SetArgDisplayNames("moved with a copy left behind");
            yield return new TestCaseData(new[] { inTheWrongLayer }, new[] { moved, inTheWrongLayer }).SetArgDisplayNames("moved into the wrong layer");
            yield return new TestCaseData(new[] { moved, aNeighbour, anIntegrationTest }, Array.Empty<string>()).SetArgDisplayNames("moved into its layer, with the integration tests left where they are");
        }

        private static IEnumerable<TestCaseData> MadeUpTreesOfPromisesUnitTests()
        {
            const string inTheOldSourcesFolder = "Services/Implementation/DeliverySources/DeliverySourceResolverEdgeCaseTest.cs";
            const string inTheOldHandlersFolder = "Services/Implementation/DomainEvents/DeliveryForecastPublishingHandlerTest.cs";
            const string atTheTestRoot = "DeliveryRuleServiceTests.cs";
            const string inTheModule = $"{ModuleFolder}/Application/DeliveryRuleServiceTest.cs";
            const string aKernelTypeTest = "Services/Implementation/DeliverySources/DeliverySourceBindabilityTest.cs";
            const string aModelsTest = "Models/DeliveryTest.cs";
            const string aNameThatOnlyStartsAlike = "Services/Implementation/DeliveryRuleServiceLikeHelper.cs";
            const string anIntegrationTest = "API/Integration/DeliveryRuleServiceIntegrationTest.cs";
            const string anArchitectureTest = "Architecture/DeliveryRuleServiceApiPreservationTest.cs";
            const string aContractTest = "Contracts/DeliverySourceResolverWireContractTest.cs";

            yield return new TestCaseData(new[] { inTheOldSourcesFolder, inTheModule }, new[] { inTheOldSourcesFolder }).SetArgDisplayNames("a Promises unit test in the old sources folder");
            yield return new TestCaseData(new[] { inTheOldHandlersFolder, atTheTestRoot }, new[] { inTheOldHandlersFolder, atTheTestRoot }).SetArgDisplayNames("Promises unit tests in the old handlers folder and at the root");
            yield return new TestCaseData(
                    new[] { inTheModule, aKernelTypeTest, aModelsTest, aNameThatOnlyStartsAlike, anIntegrationTest, anArchitectureTest, aContractTest },
                    Array.Empty<string>())
                .SetArgDisplayNames("module tests, kernel tests, a helper and tests that are not judged only");
        }

        private static IEnumerable<TestCaseData> MadeUpSourceTrees()
        {
            string[] everyException = [.. SourceFilesThatStay.Select(entry => entry.Path)];
            const string aMovedTypeLeftBehind = "Services/Implementation/DeliveryRuleService.cs";
            const string aNewcomerInTheOldSourcesFolder = "Services/Interfaces/DeliverySources/IAMadeUpSource.cs";
            const string aNewcomerNamedAfterDeliveries = "Services/Implementation/DomainEvents/AMadeUpDeliveryHandler.cs";
            const string aNewcomerOfTheWriteBackRound = "Services/Interfaces/IWriteBackMadeUp.cs";
            const string aModuleFile = $"{ModuleFolder}/Application/DeliveryRuleService.cs";
            const string aKernelModel = "Models/Delivery.cs";
            const string anUpdater = "Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs";
            const string aFolderWithASimilarName = "Services/Implementation/DeliverySourcesNeighbour/AThing.cs";

            yield return new TestCaseData(everyException.Append(aMovedTypeLeftBehind).ToArray(), new[] { aMovedTypeLeftBehind }).SetArgDisplayNames("a moved type left under Services");
            yield return new TestCaseData(everyException.Append(aNewcomerInTheOldSourcesFolder).ToArray(), new[] { aNewcomerInTheOldSourcesFolder }).SetArgDisplayNames("a newcomer in the old sources folder");
            yield return new TestCaseData(everyException.Append(aNewcomerNamedAfterDeliveries).Append(aNewcomerOfTheWriteBackRound).ToArray(), new[] { aNewcomerNamedAfterDeliveries, aNewcomerOfTheWriteBackRound })
                .SetArgDisplayNames("a Delivery newcomer and a write-back newcomer under Services");
            yield return new TestCaseData(everyException.Skip(1).ToArray(), new[] { everyException[0] }).SetArgDisplayNames("a named exception that is gone");
            yield return new TestCaseData(everyException.Concat([aModuleFile, aKernelModel, anUpdater, aFolderWithASimilarName]).ToArray(), Array.Empty<string>())
                .SetArgDisplayNames("the named exceptions, the module, the kernel models and the update code only");
        }

        private static IEnumerable<TestCaseData> MadeUpClassesCompiledFromATestFile()
        {
            const string inItsLayer = $"{TestRootNamespace}.Modules.Promises.Application.{AMadeUpTestFile}";
            const string aHelperBesideIt = $"{TestRootNamespace}.Modules.Promises.Application.AHelperBesideIt";
            const string withAStaleNamespace = $"{TestRootNamespace}.Services.Implementation.DeliverySources.{AMadeUpTestFile}";
            const string renamed = $"{TestRootNamespace}.Modules.Promises.Application.ARenamedDeliveryRuleTest";

            yield return new TestCaseData(new[] { inItsLayer, aHelperBesideIt }, Array.Empty<string>()).SetArgDisplayNames("kept its name in its layer namespace");
            yield return new TestCaseData(new[] { withAStaleNamespace }, new[] { withAStaleNamespace, inItsLayer }).SetArgDisplayNames("kept a stale namespace");
            yield return new TestCaseData(new[] { renamed }, new[] { inItsLayer }).SetArgDisplayNames("renamed its class");
            yield return new TestCaseData(Array.Empty<string>(), new[] { $"{AMadeUpTestFile}.cs" }).SetArgDisplayNames("compiled into no class");
        }

        [TestCaseSource(nameof(EveryPromisesUnitTestFile))]
        public void APromisesUnitTestFile_SitsOnlyInItsModuleLayerFolder_AndKeepsItsClassInThatLayer(string testFile, string layer)
        {
            var misplacedFiles = FilePlacementProblems(testFile, layer, TestSourcePaths.Value);
            var misplacedClasses = ClassPlacementProblems(testFile, layer, CompiledTestClasses.Value);

            ThenNothingIsMisplaced(testFile, layer, misplacedFiles.Concat(misplacedClasses));
        }

        [Test]
        public void NoUnitTestOfAPromisesType_SitsOutsideThePromisesTestFolders()
        {
            var outside = PromisesUnitTestsOutsideTheModule(TestSourcePaths.Value);

            Assert.That(
                outside,
                Is.Empty,
                $"These unit tests of a Promises type sit outside {ModuleFolder}: [{string.Join(", ", outside)}]. Why it matters: " +
                "the Promises unit tests sit next to the module, and a test of a Promises type written into an old folder splits " +
                $"the context again. How to fix: git mv the file into the layer folder of its subject under {ModuleFolder} and " +
                "change its namespace to that folder's.");
        }

        [Test]
        public void EveryClassOfThePromisesModuleTests_HasTheNamespaceOfItsFolder()
        {
            var moduleTestClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, ModuleFolder);

            Assert.That(
                moduleTestClasses,
                Is.Not.Empty,
                $"The compiled tests hold no class from a file under {ModuleFolder} or in its namespace. Why it matters: the " +
                "Promises unit tests belong next to the module, and a folder rule over an empty folder passes without looking " +
                $"at anything. How to fix: move the Promises unit-test files into the module layer folders under {ModuleFolder} with git mv.");

            ThenEachClassHasTheNamespaceOfItsFolder(moduleTestClasses);
        }

        [Test]
        public void NoPromisesSourceFile_IsLeftUnderServices_ApartFromTheNamedExceptions()
        {
            var problems = PromisesSourceFilesLeftUnderServices(ProductionSourcePaths.Value);

            Assert.That(
                problems,
                Is.Empty,
                $"[{string.Join("; ", problems)}]. Why it matters: every Promises type lives under {ModuleFolder} apart from the " +
                "named exceptions, a Promises file left under Services splits the context again, and an exception whose file is " +
                $"gone covers nothing. How to fix: git mv the file into its layer folder under {ModuleFolder}, or, when it " +
                "deliberately stays, name it with its owner among the source files that stay; take a gone exception off that list.");
        }

        [TestCaseSource(nameof(MadeUpTestTrees))]
        public void AMadeUpTestFileOutsideItsLayerFolder_IsReported_AndOneThatSitsThereIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var problems = FilePlacementProblems(AMadeUpTestFile, "Application", sourcePaths);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the placement rule reported [{string.Join("; ", problems)}]. " +
                "Why it matters: a test file still in its old place, a copy left behind or a file in the wrong layer folder must " +
                "each be named, and a file in its layer folder must not be, nor the integration tests that stay where they are. " +
                "How to fix: report the expected path when it holds no file, and every other path that holds a file of the same name.");
        }

        [TestCaseSource(nameof(MadeUpTreesOfPromisesUnitTests))]
        public void AMadeUpUnitTestOfAPromisesTypeInAnOldFolder_IsReported_AndTestsThatAreNotJudgedAreNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var outside = PromisesUnitTestsOutsideTheModule(sourcePaths);

            Assert.That(
                outside,
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join(", ", outside)}]. Why it " +
                "matters: a test named after a Promises type and ending in Test is a Promises unit test, and outside the module it " +
                "must be named; a test of a kernel type, a helper that is no test, and the integration, architecture and contract " +
                "tests must not be. How to fix: report every test file outside the module and outside the roots that are not " +
                "judged whose name is a Promises type followed by a test name.");
        }

        [TestCaseSource(nameof(MadeUpSourceTrees))]
        public void AMadeUpPromisesSourceFileUnderServices_IsReported_AndTheNamedExceptionsAreNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var problems = PromisesSourceFilesLeftUnderServices(sourcePaths);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReportedPaths),
                $"For the source tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join("; ", problems)}]. Why it " +
                "matters: a moved type left under Services, a newcomer in the old sources folder and a newcomer carrying a Promises " +
                "name must each be named, a named exception must not be while its file exists and must be named when it is gone, " +
                "and the module, the kernel models and the update code must not be. How to fix: report every Promises-named file " +
                "under Services and every file in the old sources folders unless it is a named exception, and every named " +
                "exception that holds no file.");
        }

        [TestCaseSource(nameof(MadeUpClassesCompiledFromATestFile))]
        public void AMadeUpTestFileWhoseClassLostItsNameOrLayerNamespace_IsReported_AndOneThatKeptThemIsNot(string[] classFullNames, string[] expectedReported)
        {
            const string theFile = $"{ModuleFolder}/Application/{AMadeUpTestFile}.cs";
            var classes = classFullNames
                .Select(fullName => new CompiledClass(fullName, fullName[..fullName.LastIndexOf('.')], [theFile]))
                .ToList();

            var problems = ClassPlacementProblems(AMadeUpTestFile, "Application", classes);

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
                    Does.Contain(typeof(ACompliantTwinOfThePromisesPlacementRule).FullName).And.Contain(typeof(APromisesTestDeclaredOutsideItsFolder).FullName),
                    $"The compiled tests do not tie both made-up classes to {ThisFile}. Why it matters: a twin the rule never looked " +
                    "at proves nothing by passing. How to fix: read every class with the files its methods were compiled from.");

                Assert.That(
                    madeUpModuleClasses.Select(compiled => compiled.FullName),
                    Is.EqualTo(new[] { typeof(APromisesTestDeclaredOutsideItsFolder).FullName }),
                    $"The classes taken as tests of the made-up module {MadeUpModuleTestFolder} were " +
                    $"[{string.Join(", ", madeUpModuleClasses.Select(compiled => compiled.FullName))}]. Why it matters: a newcomer " +
                    "declared in the module's namespace from a file outside the module folder must still be looked at, or it slips " +
                    "past the folder rule. How to fix: take every class that is compiled from a file under the module folder or " +
                    "declared in the module's namespace.");

                Assert.That(
                    mismatches.Select(mismatch => mismatch.FullName),
                    Is.EqualTo(new[] { typeof(APromisesTestDeclaredOutsideItsFolder).FullName }),
                    $"Expected exactly {typeof(APromisesTestDeclaredOutsideItsFolder).FullName} to be reported, got " +
                    $"[{string.Join("; ", mismatches)}]. Why it matters: a class whose namespace names a module layer while its file " +
                    "sits elsewhere hides where the test really lives, and a class whose namespace matches its folder must pass, or " +
                    "the rule flags everything. How to fix: compare each class's namespace with the namespace its folder gives.");
            }
        }

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

        // A unit test of a Promises type is a file named after a Promises type, optionally followed by more words, and
        // ending in Test or Tests.
        private static List<string> PromisesUnitTestsOutsideTheModule(IEnumerable<string> testPaths)
        {
            var promisesTypes = PromisesMembership.Moves.Concat(PromisesMembership.ModelsTypesThatMove).ToList();

            return [.. testPaths
                .Where(path => !path.StartsWith($"{ModuleFolder}/", StringComparison.Ordinal))
                .Where(path => !IsInsideAny(path, TestRootsNotJudged))
                .Where(path => IsATestOfOneOf(TypeNameOf(path), promisesTypes))];
        }

        private static bool IsATestOfOneOf(string typeName, IEnumerable<string> subjects)
            => (typeName.EndsWith("Test", StringComparison.Ordinal) || typeName.EndsWith("Tests", StringComparison.Ordinal))
               && subjects.Any(subject => typeName.StartsWith(subject, StringComparison.Ordinal)
                   && typeName.Length > subject.Length
                   && char.IsUpper(typeName[subject.Length]));

        private static List<Misplacement> PromisesSourceFilesLeftUnderServices(IReadOnlyCollection<string> productionPaths)
        {
            var promisesTypes = PromisesMembership.Moves.Concat(PromisesMembership.ModelsTypesThatMove).ToHashSet(StringComparer.Ordinal);
            var exceptions = SourceFilesThatStay.Select(entry => entry.Path).ToHashSet(StringComparer.Ordinal);

            var leftBehind = productionPaths
                .Where(path => path.StartsWith("Services/", StringComparison.Ordinal))
                .Where(path => !exceptions.Contains(path))
                .Where(path => IsInsideAny(path, FoldersThePromisesSourcesLeft) || CarriesAPromisesName(TypeNameOf(path), promisesTypes))
                .Select(path => new Misplacement(path, "is a Promises source file left under Services"));

            var goneExceptions = SourceFilesThatStay
                .Where(entry => !productionPaths.Contains(entry.Path, StringComparer.Ordinal))
                .Select(entry => new Misplacement(entry.Path, $"is named as staying with its owner {entry.Owner} but holds no file"));

            return [.. leftBehind.Concat(goneExceptions)];
        }

        private static bool CarriesAPromisesName(string typeName, HashSet<string> promisesTypes)
            => promisesTypes.Contains(typeName)
               || PromisesWordsInATypeName.Any(word => typeName.Contains(word, StringComparison.Ordinal))
               || PromisesTypeNamePrefixes.Any(prefix => typeName.StartsWith(prefix, StringComparison.Ordinal));

        private static bool IsInsideAny(string path, IEnumerable<string> folders)
            => folders.Any(folder => path.StartsWith($"{folder}/", StringComparison.Ordinal));

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
                $"{testFile} should sit only in {ModuleFolder}/{layer} with its class {testFile} and every other class in it in " +
                $"namespace {NamespaceOfFolder($"{ModuleFolder}/{layer}")}; instead: [{string.Join("; ", found)}]. Why it matters: " +
                "the Promises unit tests sit next to the module only when each one is in the folder of the layer its subject " +
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

        // A file named Foo.cs or Foo.Part.cs holds the type Foo.
        private static string TypeNameOf(string path) => FileNameOf(path).Split('.')[0];

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
            var assemblyPath = typeof(PromisesUnitTestPlacementTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                ClassesCompiledFrom(classes, path => path == ThisFile).Select(compiled => compiled.FullName),
                Does.Contain(typeof(PromisesUnitTestPlacementTest).FullName),
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

    internal static class ACompliantTwinOfThePromisesPlacementRule
    {
        public static int Answer() => 42;
    }
}

namespace Lighthouse.Backend.Tests.PlacementFixtures.Modules.Promises.Application
{
    internal static class APromisesTestDeclaredOutsideItsFolder
    {
        public static int Answer() => 42;
    }
}
