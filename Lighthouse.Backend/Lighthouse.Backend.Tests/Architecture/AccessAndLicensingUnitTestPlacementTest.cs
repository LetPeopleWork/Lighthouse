using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Tests.PlacementFixtures.Modules.Access.Application;
using Lighthouse.Backend.Tests.PlacementFixtures.Modules.Licensing.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Access and Licensing sit in their modules, and nothing points back at where they used to be. Every unit test of an
    /// Access or Licensing type sits under Modules/Access or Modules/Licensing in this project, in the folder of the layer
    /// its subject lives in, with the namespace of that folder. No file of either context is left in the old Services
    /// folders Auth, Authorization or Licensing, in the production code or in the tests, and no file there is excused. No
    /// namespace key of the shared module map still takes in a name from those old namespaces. Files are found on disk, the
    /// classes are read from the compiled test assembly with its symbols, and the keys are read from the module map's
    /// constants and static string fields, so no source text is pinned. Each rule also runs on made-up trees, classes and
    /// keys: what is out of place must be reported, and its twin in place, or a name that only looks alike, must not be.
    /// </summary>
    [TestFixture]
    public class AccessAndLicensingUnitTestPlacementTest
    {
        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string AccessFolder = "Modules/Access";

        private const string LicensingFolder = "Modules/Licensing";

        private const string MadeUpAccessFolder = "PlacementFixtures/Modules/Access";

        private const string MadeUpLicensingFolder = "PlacementFixtures/Modules/Licensing";

        private const string ThisFile = "Architecture/AccessAndLicensingUnitTestPlacementTest.cs";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        // Tests under these roots go through a host, a controller or the compiled code as a whole; they test no single
        // Access or Licensing type and stay where they are.
        private static readonly string[] TestRootsNotJudged = ["API", "Acceptance", "Architecture", "Contracts", "Integration"];

        // The folders Access and Licensing came from; nothing belongs in them any more, in either project.
        private static readonly string[] FoldersAccessAndLicensingLeft =
        [
            "Services/Implementation/Auth",
            "Services/Implementation/Authorization",
            "Services/Implementation/Licensing",
            "Services/Interfaces/Auth",
            "Services/Interfaces/Authorization",
            "Services/Interfaces/Licensing",
        ];

        // A made-up type in each of the old namespaces; a module-map key that takes in any of them still sees the old place.
        private static readonly string[] NamesInTheOldNamespaces =
        [
            "Lighthouse.Backend.Services.Implementation.Auth.AMadeUpSignIn",
            "Lighthouse.Backend.Services.Implementation.Authorization.AMadeUpGate",
            "Lighthouse.Backend.Services.Implementation.Licensing.AMadeUpLicenceCheck",
            "Lighthouse.Backend.Services.Interfaces.Auth.IAMadeUpSignIn",
            "Lighthouse.Backend.Services.Interfaces.Authorization.IAMadeUpGate",
            "Lighthouse.Backend.Services.Interfaces.Licensing.IAMadeUpLicenceCheck",
        ];

        private static readonly Lazy<List<string>> TestSourcePaths = new(() => ReadSourcePaths(TestProjectDirectory, ThisFile));

        private static readonly Lazy<List<string>> ProductionSourcePaths = new(() => ReadSourcePaths(ProductionProjectDirectory, "Program.cs"));

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static readonly Lazy<Dictionary<string, string>> FolderOfEachSubject = new(ReadFolderOfEachSubject);

        private static IEnumerable<TestCaseData> MadeUpTestTrees()
        {
            const string leftInAnOldFolder = "Services/Implementation/Auth/ApiKeyServiceTest.cs";
            const string inItsLayer = $"{AccessFolder}/Application/ApiKeyServiceTest.cs";
            const string atTheModuleRoot = $"{LicensingFolder}/LicenseServiceTest.cs";
            const string atTheTestRoot = "LicenseVerifierTest.cs";
            const string inTheWrongLayer = $"{AccessFolder}/Application/EffectiveAccessTests.cs";
            const string inTheOtherModule = $"{AccessFolder}/Adapters/Http/LicenseGuardAttributeTest.cs";
            const string anAdapterTestInItsLayer = $"{AccessFolder}/Adapters/Http/RbacGuardAttributeTest.cs";
            const string aDomainTestInItsLayer = $"{AccessFolder}/Domain/EffectiveAccessTest.cs";
            const string anInterfaceTestInItsLayer = $"{AccessFolder}/Application/IApiKeyServiceTests.cs";
            const string aLicensingTestInItsLayer = $"{LicensingFolder}/Application/LicenseVerifierTest.cs";
            const string aNameThatOnlyStartsAlike = "Services/Implementation/Forecast/LicenseServicesOverviewTest.cs";
            const string aHelperThatIsNoTest = "Services/Implementation/ApiKeyServiceLikeHelper.cs";
            const string aTestOfAnotherContext = "Services/Implementation/ForecastServiceTest.cs";
            const string anIntegrationTest = "API/Integration/LicenseServiceIntegrationTest.cs";
            const string anAcceptanceTest = "Acceptance/Rbac/RbacAdministrationServiceScenarioTest.cs";
            const string aContractTest = "Contracts/ApiKeyServiceWireContractTest.cs";
            const string anArchitectureTest = "Architecture/LicenseServiceGateTest.cs";

            yield return new TestCaseData(new[] { leftInAnOldFolder, inItsLayer }, new[] { leftInAnOldFolder })
                .SetArgDisplayNames("an Access unit test left in an old folder beside its moved copy");
            yield return new TestCaseData(new[] { atTheModuleRoot, atTheTestRoot, inTheWrongLayer, inTheOtherModule }, new[] { atTheModuleRoot, atTheTestRoot, inTheWrongLayer, inTheOtherModule })
                .SetArgDisplayNames("unit tests at a root, in the wrong layer and in the other module");
            yield return new TestCaseData(
                    new[]
                    {
                        inItsLayer, anAdapterTestInItsLayer, aDomainTestInItsLayer, anInterfaceTestInItsLayer, aLicensingTestInItsLayer,
                        aNameThatOnlyStartsAlike, aHelperThatIsNoTest, aTestOfAnotherContext, anIntegrationTest, anAcceptanceTest, aContractTest, anArchitectureTest,
                    },
                    Array.Empty<string>())
                .SetArgDisplayNames("tests in their layer, lookalikes, another context and tests that are not judged only");
        }

        private static IEnumerable<TestCaseData> MadeUpSourceTrees()
        {
            string[] aNewcomerInEachOldFolder = [.. FoldersAccessAndLicensingLeft.Select(folder => $"{folder}/AMadeUpNewcomer.cs")];
            const string deeperInAnOldFolder = "Services/Implementation/Licensing/Keys/AMadeUpKeyReader.cs";
            string[] notInAnOldFolder =
            [
                $"{AccessFolder}/Application/ApiKeyService.cs",
                $"{LicensingFolder}/Application/LicenseService.cs",
                "Services/Implementation/Authentication/AThing.cs",
                "Services/Interfaces/AuthorizationRules/AThing.cs",
                "Services/Implementation/Forecast/ForecastService.cs",
                "Services/Implementation/AuthModeHelper.cs",
                "Services/Licensing/AThing.cs",
            ];

            yield return new TestCaseData(aNewcomerInEachOldFolder, aNewcomerInEachOldFolder).SetArgDisplayNames("a newcomer in each old folder");
            yield return new TestCaseData(new[] { deeperInAnOldFolder }, new[] { deeperInAnOldFolder }).SetArgDisplayNames("a file in a folder below an old folder");
            yield return new TestCaseData(notInAnOldFolder, Array.Empty<string>()).SetArgDisplayNames("the modules, other Services folders and lookalike folder names only");
        }

        private static IEnumerable<TestCaseData> MadeUpModuleMaps()
        {
            yield return new TestCaseData(
                    typeof(AModuleMapThatStillSeesTheOldPlace),
                    new[]
                    {
                        nameof(AModuleMapThatStillSeesTheOldPlace.TheOldAndTheNewPlace),
                        nameof(AModuleMapThatStillSeesTheOldPlace.TheOldLicensingInterfaces),
                        nameof(AModuleMapThatStillSeesTheOldPlace.TheOldAuthorizationRead),
                    })
                .SetArgDisplayNames("a map with old-place keys beside keys that are not");
            yield return new TestCaseData(typeof(AModuleMapThatNoLongerSeesTheOldPlace), Array.Empty<string>())
                .SetArgDisplayNames("a map with module, neighbour and lookalike keys only");
        }

        private static IEnumerable<TestCaseData> MadeUpModuleTestFolders()
        {
            yield return new TestCaseData(MadeUpAccessFolder, typeof(ACompliantTwinOfTheAccessPlacementRule), typeof(AnAccessTestDeclaredOutsideItsFolder))
                .SetArgDisplayNames("Access");
            yield return new TestCaseData(MadeUpLicensingFolder, typeof(ACompliantTwinOfTheLicensingPlacementRule), typeof(ALicensingTestDeclaredOutsideItsFolder))
                .SetArgDisplayNames("Licensing");
        }

        [Test]
        public void EveryAccessAndLicensingUnitTest_SitsInTheFolderOfItsSubjectsLayer()
        {
            var judged = UnitTestsOfAnAccessOrLicensingType(TestSourcePaths.Value);
            var misplaced = Misplaced(judged);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    judged.Select(test => test.Path),
                    Has.Some.StartsWith($"{AccessFolder}/").And.Some.StartsWith($"{LicensingFolder}/"),
                    $"The test tree yielded no unit test of an Access type under {AccessFolder} or none of a Licensing type under " +
                    $"{LicensingFolder}: [{string.Join(", ", judged.Select(test => test.Path))}]. Why it matters: a placement rule " +
                    "that recognises no test passes without looking at anything. How to fix: recognise a test by its file name, an " +
                    "Access or Licensing type followed by more words or by Test or Tests, read from AccessAndLicensingMembership.");

                Assert.That(
                    misplaced,
                    Is.Empty,
                    $"These unit tests of an Access or Licensing type do not sit in the folder of their subject's layer: " +
                    $"[{string.Join("; ", misplaced)}]. Why it matters: the tests of Access and Licensing sit next to their " +
                    "modules, and a test written into an old folder, at a root or beside the wrong layer splits the context again. " +
                    "How to fix: git mv the file into the folder named and change its namespace to that folder's.");
            }
        }

        [TestCase(AccessFolder)]
        [TestCase(LicensingFolder)]
        public void EveryClassOfTheModuleTests_HasTheNamespaceOfItsFolder(string moduleFolder)
        {
            var moduleTestClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, moduleFolder);
            var mismatches = NamespaceFolderMismatches(moduleTestClasses);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    moduleTestClasses,
                    Is.Not.Empty,
                    $"The compiled tests hold no class from a file under {moduleFolder} or in its namespace. Why it matters: a " +
                    "folder rule over an empty folder passes without looking at anything. How to fix: read every class with the " +
                    "files its methods were compiled from, and keep the unit tests of the module under its folder.");

                Assert.That(
                    mismatches,
                    Is.Empty,
                    $"These classes do not have the namespace of the folder their file sits in: [{string.Join("; ", mismatches)}]. " +
                    "Why it matters: a reader and a test filter find a test by its namespace, so a namespace that disagrees with the " +
                    "folder hides where the test lives. How to fix: move the file into the folder its namespace names, or change the " +
                    "namespace line to the folder's namespace.");
            }
        }

        [Test]
        public void NoFileIsLeftInTheOldAccessAndLicensingFolders_InTheProductionCodeOrTheTests()
        {
            var leftInProduction = FilesLeftInTheOldFolders(ProductionSourcePaths.Value);
            var leftInTests = FilesLeftInTheOldFolders(TestSourcePaths.Value);

            Assert.That(
                leftInProduction.Select(path => $"{ProductionProjectDirectory}/{path}").Concat(leftInTests.Select(path => $"{TestProjectDirectory}/{path}")),
                Is.Empty,
                $"Files are left in the old folders [{string.Join(", ", FoldersAccessAndLicensingLeft)}]. Why it matters: Access " +
                "and Licensing live under Modules/Access and Modules/Licensing, with no file excused, and a file in an old folder " +
                "splits the context again. How to fix: git mv the file into the layer folder of its module, or, when it belongs " +
                "to another context, into that context's folder.");
        }

        [Test]
        public void NoNamespaceKeyOfTheModuleMap_TakesInANameFromTheOldAccessAndLicensingNamespaces()
        {
            var keys = NamespaceKeysOf(typeof(ModuleBoundariesArchUnitTest));
            var seeingTheOldPlace = KeysThatTakeInAnOldName(keys);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    keys.Keys,
                    Does.Contain(nameof(ModuleBoundariesArchUnitTest.TrackerIntegrationPattern)),
                    $"The keys read from the module map were [{string.Join(", ", keys.Keys)}]. Why it matters: a check that reads " +
                    "no key passes without looking at anything. How to fix: read every constant and static string field of the map.");

                Assert.That(
                    seeingTheOldPlace,
                    Is.Empty,
                    $"These namespace keys of the module map still take in a name from the old Services Auth, Authorization or " +
                    $"Licensing namespaces: [{string.Join(", ", seeingTheOldPlace)}]. Why it matters: Access and Licensing have " +
                    "their own module rules now, and a key that still sees the old place keeps a second, older module map alive " +
                    "beside them. How to fix: retire the key and the module it builds, with the checks that only read it; the " +
                    "module rules of Access and Licensing already carry what it guarded.");
            }
        }

        [TestCaseSource(nameof(MadeUpTestTrees))]
        public void AMadeUpUnitTestOutsideTheFolderOfItsSubjectsLayer_IsReported_AndOneInPlaceIsNot(string[] testPaths, string[] expectedReported)
        {
            var misplaced = Misplaced(UnitTestsOfAnAccessOrLicensingType(testPaths));

            Assert.That(
                misplaced.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReported),
                $"For the test tree [{string.Join(", ", testPaths)}] the rule reported [{string.Join("; ", misplaced)}]. Why it " +
                "matters: a unit test of an Access or Licensing type left in an old folder, at a root, in the wrong layer or in " +
                "the other module must each be named, and a test in its layer, a name that only starts alike, a helper that is no " +
                "test, a test of another context and the tests that are not judged must not be. How to fix: take a file as a unit " +
                "test of the type its name starts with, followed by a capital letter, when it ends in Test or Tests, and compare " +
                "its folder with the folder of that type's layer.");
        }

        [TestCaseSource(nameof(MadeUpSourceTrees))]
        public void AMadeUpFileInAnOldFolder_IsReported_AndAFileOutsideThemIsNot(string[] sourcePaths, string[] expectedReported)
        {
            var left = FilesLeftInTheOldFolders(sourcePaths);

            Assert.That(
                left,
                Is.EquivalentTo(expectedReported),
                $"For the source tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join(", ", left)}]. Why it " +
                "matters: a file anywhere in or below an old Auth, Authorization or Licensing folder of Services must be named, " +
                "and a file in a module, in another Services folder or in a folder whose name only starts alike must not be. How " +
                "to fix: report every path inside one of the old folders, and nothing else.");
        }

        [TestCaseSource(nameof(MadeUpModuleMaps))]
        public void AMadeUpModuleMapKeyThatTakesInAnOldName_IsReported_AndOneThatDoesNotIsNot(Type moduleMap, string[] expectedReported)
        {
            var keys = NamespaceKeysOf(moduleMap);
            var seeingTheOldPlace = KeysThatTakeInAnOldName(keys);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    keys,
                    Has.Count.EqualTo(moduleMap.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length),
                    $"The keys read from {moduleMap.Name} were [{string.Join(", ", keys.Keys)}]. Why it matters: a key that is not " +
                    "read is never judged. How to fix: read every constant and static string field.");

                Assert.That(
                    seeingTheOldPlace,
                    Is.EquivalentTo(expectedReported),
                    $"For the keys [{string.Join("; ", keys.Select(key => $"{key.Key} = {key.Value}"))}] the check reported " +
                    $"[{string.Join(", ", seeingTheOldPlace)}]. Why it matters: a key that takes in a name from the old Services " +
                    "Auth, Authorization or Licensing namespaces, as a constant or as a static field, must be named, and a key of a " +
                    "module, of another Services folder or of a namespace that only starts alike must not be. How to fix: match each " +
                    "key as a pattern against a made-up name in each old namespace.");
            }
        }

        [TestCaseSource(nameof(MadeUpModuleTestFolders))]
        public void AMadeUpClassDeclaredInAModuleNamespaceOutsideItsFolder_IsReported_AndItsTwinInTheFolderIsNot(string moduleFolder, Type twinInTheFolder, Type declaredOutsideIt)
        {
            var moduleTestClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, moduleFolder);
            var mismatches = NamespaceFolderMismatches(moduleTestClasses);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    moduleTestClasses.Select(compiled => compiled.FullName),
                    Is.EquivalentTo(new[] { twinInTheFolder.FullName, declaredOutsideIt.FullName }),
                    $"The classes taken as tests of the made-up module {moduleFolder} were " +
                    $"[{string.Join(", ", moduleTestClasses.Select(compiled => compiled.FullName))}]. Why it matters: a class compiled " +
                    "from a file under the module folder, and a newcomer declared in the module's namespace from a file elsewhere, " +
                    "must both be looked at, or the folder rule sees nothing. How to fix: take every class compiled from a file under " +
                    "the module folder or declared in the module's namespace.");

                Assert.That(
                    mismatches.Select(mismatch => mismatch.FullName),
                    Is.EqualTo(new[] { declaredOutsideIt.FullName }),
                    $"Expected exactly {declaredOutsideIt.FullName} to be reported, got [{string.Join("; ", mismatches)}]. Why it " +
                    "matters: a class whose namespace names a module layer while its file sits elsewhere hides where the test lives, " +
                    "and a class whose namespace matches its folder must pass, or the rule flags everything. How to fix: compare each " +
                    "class's namespace with the namespace its folder gives.");
            }
        }

        private static List<SubjectTest> UnitTestsOfAnAccessOrLicensingType(IEnumerable<string> testPaths)
            => [.. testPaths
                .Where(path => !IsInsideAny(path, TestRootsNotJudged))
                .Select(path => (Path: path, Subject: SubjectOf(TypeNameOf(path))))
                .Where(test => test.Subject is not null)
                .Select(test => new SubjectTest(test.Path, test.Subject!, FolderOfEachSubject.Value[test.Subject!]))];

        // A unit test of a type is a file named after the type, followed by more words starting with a capital letter, and
        // ending in Test or Tests. The longest such type is the subject, so a test of IApiKeyService is not taken for one
        // of a shorter name.
        private static string? SubjectOf(string fileTypeName)
        {
            if (!fileTypeName.EndsWith("Test", StringComparison.Ordinal) && !fileTypeName.EndsWith("Tests", StringComparison.Ordinal))
            {
                return null;
            }

            return FolderOfEachSubject.Value.Keys
                .Where(subject => fileTypeName.StartsWith(subject, StringComparison.Ordinal)
                    && fileTypeName.Length > subject.Length
                    && char.IsUpper(fileTypeName[subject.Length]))
                .MaxBy(subject => subject.Length);
        }

        private static List<Misplacement> Misplaced(IEnumerable<SubjectTest> tests)
            => [.. tests
                .Where(test => FolderOf(test.Path) != test.ExpectedFolder)
                .Select(test => new Misplacement(test.Path, $"tests {test.Subject}, so it belongs in {test.ExpectedFolder}"))];

        private static List<string> FilesLeftInTheOldFolders(IEnumerable<string> sourcePaths)
            => [.. sourcePaths.Where(path => IsInsideAny(path, FoldersAccessAndLicensingLeft))];

        private static Dictionary<string, string> ReadFolderOfEachSubject()
        {
            (IEnumerable<string> Types, string Folder)[] layers =
            [
                (AccessAndLicensingMembership.AccessDomain.Concat(AccessAndLicensingMembership.ModelsTypesThatMoveIntoAccess), $"{AccessFolder}/Domain"),
                (AccessAndLicensingMembership.AccessApplication, $"{AccessFolder}/Application"),
                (AccessAndLicensingMembership.AccessContracts, $"{AccessFolder}/Contracts"),
                (AccessAndLicensingMembership.AccessAdaptersHttp, $"{AccessFolder}/Adapters/Http"),
                (AccessAndLicensingMembership.LicensingApplication, $"{LicensingFolder}/Application"),
                (AccessAndLicensingMembership.LicensingContracts, $"{LicensingFolder}/Contracts"),
                (AccessAndLicensingMembership.LicensingAdaptersHttp, $"{LicensingFolder}/Adapters/Http"),
            ];

            return layers
                .SelectMany(layer => layer.Types.Select(type => (Name: type[(type.LastIndexOf('.') + 1)..], layer.Folder)))
                .ToDictionary(entry => entry.Name, entry => entry.Folder, StringComparer.Ordinal);
        }

        // Every constant and static string field of a module map, read without running its static set-up.
        private static Dictionary<string, string> NamespaceKeysOf(Type moduleMap)
            => moduleMap.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(field => field.FieldType == typeof(string))
                .ToDictionary(field => field.Name, field => (string)(field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null))!, StringComparer.Ordinal);

        private static List<string> KeysThatTakeInAnOldName(Dictionary<string, string> keys)
            => [.. keys
                .Where(key => NamesInTheOldNamespaces.Any(name => Regex.IsMatch(name, key.Value, RegexOptions.None, TimeSpan.FromSeconds(1))))
                .Select(key => key.Key)];

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
            var assemblyPath = typeof(AccessAndLicensingUnitTestPlacementTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                classes.Where(compiled => compiled.Files.Contains(ThisFile)).Select(compiled => compiled.FullName),
                Does.Contain(typeof(AccessAndLicensingUnitTestPlacementTest).FullName),
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

        private sealed record SubjectTest(string Path, string Subject, string ExpectedFolder);

        private sealed record CompiledClass(string FullName, string Namespace, IReadOnlyList<string> Files);

        private sealed record Misplacement(string Path, string What)
        {
            public override string ToString() => $"{Path} {What}";
        }

        private sealed record NamespaceFolderMismatch(string FullName, string Namespace, string Path, string FolderNamespace)
        {
            public override string ToString() => $"{FullName} is in namespace {Namespace} but its file {Path} sits in the folder of namespace {FolderNamespace}";
        }

        private static class AModuleMapThatStillSeesTheOldPlace
        {
            internal const string TheOldAndTheNewPlace =
                @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization|Licensing)($|\..*)|^Lighthouse\.Backend\.Modules\.(Access|Licensing)(\.|$)";

            internal const string TheOldLicensingInterfaces = @"^Lighthouse\.Backend\.Services\.Interfaces\.Licensing(\.|$)";

            internal const string TheAccessModule = @"^Lighthouse\.Backend\.Modules\.Access(\.|$)";

            internal static readonly string TheOldAuthorizationRead = @"^Lighthouse\.Backend\.Services\.Implementation\.Authorization\.";

            internal static readonly string AForecastingNeighbour = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Forecast($|\..*)";
        }

        private static class AModuleMapThatNoLongerSeesTheOldPlace
        {
            internal const string TheAccessModule = @"^Lighthouse\.Backend\.Modules\.Access(\.|$)";

            internal const string TheLicensingModule = @"^Lighthouse\.Backend\.Modules\.Licensing(\.|$)";

            internal const string AForecastingNeighbour = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Forecast($|\..*)";

            internal static readonly string ANamespaceThatOnlyStartsAlike = @"^Lighthouse\.Backend\.Services\.Implementation\.Authentication(\.|$)";
        }
    }
}

namespace Lighthouse.Backend.Tests.PlacementFixtures.Modules.Access.Application
{
    internal static class AnAccessTestDeclaredOutsideItsFolder
    {
        public static int Answer() => 42;
    }
}

namespace Lighthouse.Backend.Tests.PlacementFixtures.Modules.Licensing.Application
{
    internal static class ALicensingTestDeclaredOutsideItsFolder
    {
        public static int Answer() => 42;
    }
}
