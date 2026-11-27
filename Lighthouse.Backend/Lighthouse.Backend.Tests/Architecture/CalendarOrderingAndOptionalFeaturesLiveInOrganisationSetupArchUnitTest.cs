using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The calendar (blackout periods and recurring blackout rules), Feature ordering and ranking and the optional-features
    /// mechanism are the first 18 types of OrganisationSetupMembership. Each lives in Lighthouse.Backend.Modules.OrganisationSetup,
    /// in the namespace of its layer and in no other (2 in Domain, 10 in Application, 2 in Contracts, 4 in Application.OptionalFeatures), and none is
    /// left in the loose Services namespaces. The five unit tests of the group sit under Modules/OrganisationSetup in this project, in the
    /// folder of their subject's layer and in the namespace of that folder. The map owns every name and layer; this class adds no
    /// list of types. The checks run on made-up types too: a type left behind or put in the wrong layer must be named.
    /// </summary>
    [TestFixture]
    public class CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest
    {
        private const int TypesInTheGroup = 18;

        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ModuleTestFolder = "Modules/OrganisationSetup";

        private const string ThisFile = "Architecture/CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest.cs";

        private const string FixtureLayerRoot = "Lighthouse.Backend.Modules.OrganisationSetupFixtures.CalendarAndOrdering";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.OrganisationSetupFixtures\.CalendarAndOrdering\.Services(\.|$)";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        // The subject of each unit test is named; its layer, and so its folder, is read from the map.
        private static readonly string[] SubjectsOfTheMovingUnitTests =
        [
            "BlackoutPeriodService",
            "RecurringBlackoutRuleService",
            "FeatureRankSeeder",
            "WeeklyRecurrence",
            "RecurringBlackoutRuleExtensions",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest).Assembly));

        private static readonly Lazy<List<string>> TestSourcePaths = new(ReadSourcePaths);

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static IReadOnlyList<OrganisationSetupType> Group => [.. OrganisationSetupMembership.MovingTypes.Take(TypesInTheGroup)];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheGroupIsEighteenDistinctTypes_TwoInDomain_TenInApplication_TwoInContracts_FourInOptionalFeatures()
        {
            var group = Group;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(group.Select(type => type.Name).ToList(), Has.Count.EqualTo(TypesInTheGroup).And.Unique, "WHAT: the calendar, ordering and optional-feature group is not 18 distinct types. WHY: the move is judged against the 18 types of the map, and a short or doubled group lets one stay behind unseen. HOW: the first 18 entries of OrganisationSetupMembership are this group.");
                Assert.That(group.Count(type => type.Layer == OrganisationSetupLayer.Domain), Is.EqualTo(2), "WHAT: the group does not hold 2 Domain types. WHY: the layer split is part of what is promised. HOW: the map lists the two calendar Domain candidates first.");
                Assert.That(group.Count(type => type.Layer == OrganisationSetupLayer.Application), Is.EqualTo(10), "WHAT: the group does not hold 10 Application types. WHY: the layer split is part of what is promised. HOW: the map lists ten services and ports of calendar and ordering plus the two calendar services.");
                Assert.That(group.Count(type => type.Layer == OrganisationSetupLayer.Contracts), Is.EqualTo(2), "WHAT: the group does not hold 2 Contracts types. WHY: the layer split is part of what is promised. HOW: the map lists the blackout period service and the feature ordering ports as published.");
                Assert.That(group.Count(type => type.Layer == OrganisationSetupLayer.ApplicationOptionalFeatures), Is.EqualTo(4), "WHAT: the group does not hold 4 optional-feature types. WHY: the layer split is part of what is promised. HOW: the map lists the three appliers, the registry and the port as the last four of the group.");
            }
        }

        [Test]
        public void TheTwoCalendarDomainTypes_LiveInTheModuleDomain_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, NamespaceOf(OrganisationSetupLayer.Domain), NamesIn(OrganisationSetupLayer.Domain)).Check();
        }

        [Test]
        public void TheTenCalendarOrderingAndRankingTypes_LiveInTheModuleApplication_AndTheTwoPublishedPortsInItsContracts_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, NamespaceOf(OrganisationSetupLayer.Application), NamesIn(OrganisationSetupLayer.Application)).Check();
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, NamespaceOf(OrganisationSetupLayer.Contracts), NamesIn(OrganisationSetupLayer.Contracts)).Check();
        }

        [Test]
        public void TheFourOptionalFeatureTypes_LiveInTheModuleApplicationOptionalFeatures_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, NamespaceOf(OrganisationSetupLayer.ApplicationOptionalFeatures), NamesIn(OrganisationSetupLayer.ApplicationOptionalFeatures)).Check();
        }

        [Test]
        public void EveryTypeOfTheGroup_IsFoundInItsNewModuleNamespaceOnly()
        {
            var problems = WhenTheNamespacesAreChecked(TypesInTheCode(), Group);

            ThenThereAreNoProblems(problems, "a calendar, ordering or optional-feature type is not in its module namespace only",
                "A type that moved must exist once, in Modules.OrganisationSetup.<Layer>: a type left in the loose Services namespaces or declared twice is a half move that the rules would read wrongly.",
                "Move the declaring file into Modules/OrganisationSetup/<layer folder> and change its namespace; delete the old file.");
        }

        [Test]
        public void ATypeLeftInItsOldNamespace_IsNamedAsNotInItsModuleNamespace()
        {
            var left = Group.First(type => type.Layer == OrganisationSetupLayer.Application);
            var code = CodeAfterTheMove().Select(type => type == (left.NewNamespace, left.Name) ? (left.OldNamespace, left.Name) : type).ToList();

            var problems = WhenTheNamespacesAreChecked(code, Group);

            ThenTheProblemsNameOnly(problems, left.Name);
        }

        [Test]
        public void ADomainCandidateDeclaredInTheApplicationNamespace_IsNamedAsInTheWrongLayer()
        {
            var misplaced = Group.First(type => type.Layer == OrganisationSetupLayer.Domain);
            var code = CodeAfterTheMove().Select(type => type == (misplaced.NewNamespace, misplaced.Name) ? (NamespaceOf(OrganisationSetupLayer.Application), misplaced.Name) : type).ToList();

            var problems = WhenTheNamespacesAreChecked(code, Group);

            ThenTheProblemsNameOnly(problems, misplaced.Name);
        }

        [Test]
        public void ATypeCopiedIntoTheModuleButKeptInItsOldNamespace_IsNamedAsFoundTwice()
        {
            var copied = Group.First(type => type.Layer == OrganisationSetupLayer.ApplicationOptionalFeatures);
            var code = CodeAfterTheMove().Append((copied.OldNamespace, copied.Name)).ToList();

            var problems = WhenTheNamespacesAreChecked(code, Group);

            ThenTheProblemsNameOnly(problems, copied.Name);
        }

        [Test]
        public void AMadeUpTypeLeftInTheFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureApplication, ["AWellPlacedService"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureApplication, ["ALeftBehindService"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "WHAT: a made-up type in its layer with no copy in the former home was reported. WHY: a rule that flags a correct move hides the real offenders. HOW: report only a type missing from the layer or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "WHAT: a made-up type left in the former home passed the layer rule. WHY: a half move would go unseen. HOW: report a type that is still in the former home.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindService"), "WHAT: the failure does not name the type left behind. WHY: the move owner must see which type is wrong. HOW: name each type left in the former home.");
        }

        [Test]
        public void AMadeUpDomainCandidatePlacedInTheApplication_IsNamedAsMissingFromTheDomain()
        {
            var misplaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureDomain, ["AMisplacedDomainCandidate"], FixtureFormerHome);

            var failure = Assert.Throws<AssertionException>(misplaced.Check, "WHAT: a made-up Domain candidate placed in the Application passed the Domain rule. WHY: the Domain would silently hold fewer types than the design gives it. HOW: expect the type in the Domain namespace, so a copy elsewhere is missing there.");
            Assert.That(failure!.Message, Does.Contain("AMisplacedDomainCandidate"), "WHAT: the failure does not name the misplaced type. WHY: the move owner must see which type is wrong. HOW: name each type missing from its layer.");
        }

        [TestCaseSource(nameof(EveryMovingUnitTest))]
        public void AnUnitTestOfTheGroup_SitsOnlyInTheModuleFolderOfItsSubjectsLayer_AndKeepsItsClassInThatFolderNamespace(string name, string layerFolder)
        {
            var expectedFile = $"{ModuleTestFolder}/{layerFolder}/{name}.cs";
            var expectedNamespace = $"{TestRootNamespace}.{ModuleTestFolder.Replace('/', '.')}.{layerFolder.Replace('/', '.')}";

            var filesNamedLikeIt = TestSourcePaths.Value.Where(path => path.EndsWith($"/{name}.cs", StringComparison.Ordinal)).ToList();
            var classesNamedLikeIt = CompiledTestClasses.Value.Where(compiled => compiled.Files.Any(file => file.EndsWith($"/{name}.cs", StringComparison.Ordinal))).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    filesNamedLikeIt,
                    Is.EqualTo(new[] { expectedFile }),
                    $"WHAT: {name}.cs sits at [{string.Join(", ", filesNamedLikeIt)}] instead of only at {expectedFile}. WHY: a unit test belongs beside the layer of the module it tests, and a copy or a stray file splits the context. HOW: git mv the file to {expectedFile}.");
                Assert.That(
                    classesNamedLikeIt.Select(compiled => compiled.FullName),
                    Is.EqualTo(new[] { $"{expectedNamespace}.{name}" }),
                    $"WHAT: the compiled classes of {name}.cs are [{string.Join(", ", classesNamedLikeIt.Select(compiled => compiled.FullName))}] instead of only {expectedNamespace}.{name}. WHY: a moved test keeps its class name and takes the namespace of its folder, so a test filter and a reader still find it under the same name. HOW: change the namespace line to {expectedNamespace} and keep the class name.");
            }
        }

        [Test]
        public void EveryClassUnderTheModuleTestFolder_HasTheNamespaceOfItsFolder_AndTheFolderIsNotEmpty()
        {
            var moduleClasses = CompiledTestClasses.Value.Where(compiled => compiled.Files.Any(file => file.StartsWith($"{ModuleTestFolder}/", StringComparison.Ordinal))).ToList();
            var mismatches = moduleClasses.Where(compiled => !compiled.Files.All(file => compiled.Namespace == FolderNamespace(file))).Select(compiled => compiled.FullName).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(moduleClasses, Is.Not.Empty, $"WHAT: the compiled tests hold no class from a file under {ModuleTestFolder}. WHY: a folder rule over an empty folder passes without looking at anything. HOW: git mv the group's unit tests into {ModuleTestFolder}.");
                Assert.That(mismatches, Is.Empty, $"WHAT: these classes do not have the namespace of their folder: [{string.Join(", ", mismatches)}]. WHY: the namespace equals the folder for every moved file. HOW: change the namespace line to the folder's.");
            }
        }

        [Test]
        public void NoUnitTestOfTheGroup_IsLeftInAnOldFolder()
        {
            var left = TestsLeftInOldFolders(TestSourcePaths.Value);

            Assert.That(
                left,
                Is.Empty,
                $"WHAT: these unit tests of the group still sit in an old folder: [{string.Join(", ", left)}]. WHY: a test left behind splits the context again. HOW: git mv the file into the layer folder of its subject under {ModuleTestFolder} and change its namespace to that folder's.");
        }

        [Test]
        public void AMadeUpUnitTestLeftInAnOldFolder_IsNamed()
        {
            var name = EveryUnitTest().First();
            var paths = new List<string> { string.Join('/', "Services", "Implementation", $"{name.Name}.cs") };

            var left = TestsLeftInOldFolders(paths);

            Assert.That(left, Is.EqualTo(new[] { paths[0] }), "WHAT: a made-up unit test left in an old folder was not named. WHY: the check would pass a half move. HOW: name each file of the group found in an old folder.");
        }

        private static IEnumerable<(string Name, string LayerFolder)> EveryUnitTest()
            => SubjectsOfTheMovingUnitTests.Select(subject => ($"{subject}Test", OrganisationSetupMembership.MovingTypes.Single(type => type.Name == subject).Layer.ToString()));

        private static IEnumerable<TestCaseData> EveryMovingUnitTest()
            => EveryUnitTest().Select(test => new TestCaseData(test.Name, test.LayerFolder).SetArgDisplayNames(test.Name));

        // The old folders are built from segments so that no literal names the place the tests left.
        private static List<string> TestsLeftInOldFolders(IEnumerable<string> sourcePaths)
        {
            var oldFolders = new[] { string.Join('/', "Services"), string.Join('/', "Services", "Implementation") };
            var oldFiles = EveryUnitTest().SelectMany(test => oldFolders.Select(folder => $"{folder}/{test.Name}.cs")).ToHashSet(StringComparer.Ordinal);

            return [.. sourcePaths.Where(oldFiles.Contains)];
        }

        private static string NamespaceOf(OrganisationSetupLayer layer) => Group.First(type => type.Layer == layer).NewNamespace;

        private static List<string> NamesIn(OrganisationSetupLayer layer) => [.. Group.Where(type => type.Layer == layer).Select(type => type.Name)];

        private static List<(string Namespace, string Name)> TypesInTheCode()
        {
            var full = IlDependencies.TypesIn(ProductionModule.Value, ModuleRules.AnyNamespace);
            Assert.That(full, Is.Not.Empty, "WHAT: the production assembly holds no type the rule could read. WHY: a rule over nothing passes unseen. HOW: point the read at the production assembly.");

            return [.. full.Select(fullName => (fullName.Contains('.') ? fullName[..fullName.LastIndexOf('.')] : string.Empty, fullName[(fullName.LastIndexOf('.') + 1)..]))];
        }

        private static List<(string Namespace, string Name)> CodeAfterTheMove()
            => [.. Group.Select(type => (type.NewNamespace, type.Name))];

        private static List<string> WhenTheNamespacesAreChecked(IEnumerable<(string Namespace, string Name)> code, IEnumerable<OrganisationSetupType> group)
        {
            var found = code.ToList();

            return [.. group
                .Select(type => (Type: type, Places: found
                    .Where(candidate => candidate.Name == type.Name && (candidate.Namespace == type.OldNamespace || IsInTheModule(candidate.Namespace)))
                    .Select(candidate => candidate.Namespace)
                    .Order(StringComparer.Ordinal)
                    .ToList()))
                .Where(entry => !entry.Places.SequenceEqual([entry.Type.NewNamespace]))
                .Select(entry => $"{entry.Type.Name}: expected only {entry.Type.NewNamespace}, found in [{string.Join(", ", entry.Places)}]")];
        }

        private static void ThenThereAreNoProblems(List<string> problems, string what, string why, string how)
            => Assert.That(problems, Is.Empty, $"WHAT: {what}: {string.Join("; ", problems)}. WHY: {why} HOW: {how}");

        private static void ThenTheProblemsNameOnly(List<string> problems, string subject)
        {
            Assert.That(problems, Has.Count.EqualTo(1), $"WHAT: exactly one problem about {subject} was expected, got [{string.Join("; ", problems)}]. WHY: a check that names more or less than the one wrong thing cannot guide the move. HOW: make the check name each wrong placement once and nothing else.");
            Assert.That(problems[0], Does.StartWith(subject), $"WHAT: the problem names another subject than {subject}: {problems[0]}. WHY: the move owner must see which type is wrong. HOW: start each problem with the type name.");
        }

        private static string FixtureApplication => $"{FixtureLayerRoot}.Application";

        private static string FixtureDomain => $"{FixtureLayerRoot}.Domain";

        private static bool IsInTheModule(string ns) => ns == OrganisationSetupType.NewRootNamespace || ns.StartsWith(OrganisationSetupType.NewRootNamespace + ".", StringComparison.Ordinal);

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
            var assemblyPath = typeof(CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                classes.Where(compiled => compiled.Files.Contains(ThisFile)).Select(compiled => compiled.FullName),
                Does.Contain(typeof(CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest).FullName),
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

namespace Lighthouse.Backend.Modules.OrganisationSetupFixtures.CalendarAndOrdering.Application
{
    internal sealed class AWellPlacedService
    {
        public string? Label { get; init; }
    }

    internal sealed class AMisplacedDomainCandidate
    {
        public string? Label { get; init; }
    }
}

namespace Lighthouse.Backend.Modules.OrganisationSetupFixtures.CalendarAndOrdering.Services
{
    internal sealed class ALeftBehindService
    {
        public string? Label { get; init; }
    }
}
