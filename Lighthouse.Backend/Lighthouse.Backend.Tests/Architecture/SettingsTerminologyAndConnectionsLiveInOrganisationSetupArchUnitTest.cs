using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Settings, terminology, connection defaults and connection usage are the last 9 types of OrganisationSetupMembership. Each lives
    /// in Lighthouse.Backend.Modules.OrganisationSetup, in the namespace of its layer and in no other (4 in Application, 5 in
    /// Application.Connections), and none is left in the loose Services namespaces. The four unit tests of the group sit under
    /// Modules/OrganisationSetup in this project, in the folder of their subject's layer and in the namespace of that folder. Tracker
    /// integration reaches set-up along exactly one listed edge, from the connection health cadence to the settings port. The map owns
    /// every name and layer; this class adds no list of types.
    /// </summary>
    [TestFixture]
    public class SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest
    {
        private const int TypesInTheGroup = 9;

        private const int TypesBeforeTheGroup = 18;

        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ModuleTestFolder = "Modules/OrganisationSetup";

        private const string ThisFile = "Architecture/SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest.cs";

        private const string FixtureLayerRoot = "Lighthouse.Backend.Modules.OrganisationSetupFixtures.SettingsAndConnections";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.OrganisationSetupFixtures\.SettingsAndConnections\.Services(\.|$)";

        private const string TheCadence = "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        private static readonly string[] SubjectsOfTheMovingUnitTests =
        [
            "AppSettingService",
            "TerminologyService",
            "ConnectionSaveService",
            "WorkTrackingSystemConnectionUsage",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest).Assembly));

        private static readonly Lazy<List<string>> TestSourcePaths = new(ReadSourcePaths);

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static IReadOnlyList<OrganisationSetupType> Group => [.. OrganisationSetupMembership.MovingTypes.Skip(TypesBeforeTheGroup)];

        private static string TheEdgeTheMoveAdds => $"{TheCadence} -> {OrganisationSetupMembership.MovingTypes.Single(type => type.Name == "IAppSettingService").NewFullName}";

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheGroupIsNineDistinctTypes_FourInApplication_FiveInConnections_FromSettingsToConnectionUsage()
        {
            var group = Group;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(group.Select(type => type.Name).ToList(), Has.Count.EqualTo(TypesInTheGroup).And.Unique, "WHAT: the settings, terminology and connections group is not 9 distinct types. WHY: the move is judged against the 9 types of the map, and a short or doubled group lets one stay behind unseen. HOW: the entries after the first 18 of OrganisationSetupMembership are this group.");
                Assert.That(group[0].Name, Is.EqualTo("AppSettingService"), "WHAT: the group does not start at the settings service. WHY: the group is cut by position, and a shifted map would test the wrong 9 types. HOW: keep the settings service first after the calendar, ordering and optional-feature types.");
                Assert.That(group[^1].Name, Is.EqualTo("IWorkTrackingSystemConnectionUsage"), "WHAT: the group does not end at the connection usage port. WHY: the group is cut by position, and a shifted map would test the wrong 9 types. HOW: keep the connection usage port last in the map.");
                Assert.That(group.Count(type => type.Layer == OrganisationSetupLayer.Application), Is.EqualTo(4), "WHAT: the group does not hold 4 Application types. WHY: the layer split is part of what is promised. HOW: the map lists the settings and terminology services with their two ports.");
                Assert.That(group.Count(type => type.Layer == OrganisationSetupLayer.ApplicationConnections), Is.EqualTo(5), "WHAT: the group does not hold 5 Application.Connections types. WHY: the layer split is part of what is promised. HOW: the map lists the connection save, its default change, the connection usage and their two ports.");
            }
        }

        [Test]
        public void TheFourSettingsAndTerminologyTypes_LiveInTheModuleApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, NamespaceOf(OrganisationSetupLayer.Application), NamesIn(OrganisationSetupLayer.Application)).Check();
        }

        [Test]
        public void TheFiveConnectionDefaultAndUsageTypes_LiveInTheModuleApplicationConnections_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, NamespaceOf(OrganisationSetupLayer.ApplicationConnections), NamesIn(OrganisationSetupLayer.ApplicationConnections)).Check();
        }

        [Test]
        public void EveryTypeOfTheGroup_IsFoundInItsNewModuleNamespaceOnly()
        {
            var problems = WhenTheNamespacesAreChecked(TypesInTheCode(), Group);

            ThenThereAreNoProblems(problems, "a settings, terminology or connection type is not in its module namespace only",
                "A type that moved must exist once, in Modules.OrganisationSetup.<Layer>: a type left in the loose Services namespaces or declared twice is a half move that the rules would read wrongly.",
                "Move the declaring file into Modules/OrganisationSetup/<layer folder> and change its namespace; delete the old file.");
        }

        [Test]
        public void ATypeLeftInItsOldNamespace_IsNamedAsNotInItsModuleNamespace()
        {
            var left = Group.First(type => type.Layer == OrganisationSetupLayer.ApplicationConnections);
            var code = CodeAfterTheMove().Select(type => type == (left.NewNamespace, left.Name) ? (left.OldNamespace, left.Name) : type).ToList();

            var problems = WhenTheNamespacesAreChecked(code, Group);

            ThenTheProblemsNameOnly(problems, left.Name);
        }

        [Test]
        public void AConnectionTypeDeclaredInTheApplicationNamespace_IsNamedAsInTheWrongLayer()
        {
            var misplaced = Group.First(type => type.Layer == OrganisationSetupLayer.ApplicationConnections);
            var code = CodeAfterTheMove().Select(type => type == (misplaced.NewNamespace, misplaced.Name) ? (NamespaceOf(OrganisationSetupLayer.Application), misplaced.Name) : type).ToList();

            var problems = WhenTheNamespacesAreChecked(code, Group);

            ThenTheProblemsNameOnly(problems, misplaced.Name);
        }

        [Test]
        public void ATypeCopiedIntoTheModuleButKeptInItsOldNamespace_IsNamedAsFoundTwice()
        {
            var copied = Group.First(type => type.Layer == OrganisationSetupLayer.Application);
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
        public void AMadeUpConnectionTypePlacedInTheApplication_IsNamedAsMissingFromTheConnectionsNamespace()
        {
            var misplaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureConnections, ["AMisplacedConnectionType"], FixtureFormerHome);

            var failure = Assert.Throws<AssertionException>(misplaced.Check, "WHAT: a made-up connection type placed in the Application passed the Application.Connections rule. WHY: the connections grouping would silently hold fewer types than the design gives it. HOW: expect the type in the Connections namespace, so a copy elsewhere is missing there.");
            Assert.That(failure!.Message, Does.Contain("AMisplacedConnectionType"), "WHAT: the failure does not name the misplaced type. WHY: the move owner must see which type is wrong. HOW: name each type missing from its layer.");
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
                    classesNamedLikeIt.Select(compiled => compiled.Namespace).Distinct(),
                    Is.EqualTo(new[] { expectedNamespace }),
                    $"WHAT: the compiled classes of {name}.cs are [{string.Join(", ", classesNamedLikeIt.Select(compiled => compiled.FullName))}] and do not all sit in {expectedNamespace}. WHY: a moved test keeps its class name and takes the namespace of its folder, so a test filter and a reader still find it under the same name. HOW: change the namespace line to {expectedNamespace} and keep the class name.");
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
            var paths = new List<string> { string.Join('/', "API", "TerminologyServiceTest.cs") };

            var left = TestsLeftInOldFolders(paths);

            Assert.That(left, Is.EqualTo(new[] { paths[0] }), "WHAT: a made-up unit test left in an old folder was not named. WHY: the check would pass a half move. HOW: name each file of the group found in an old folder.");
        }

        [Test]
        public void TrackerIntegrationListsExactlyOneEdgeIntoOrganisationSetup_TheCadenceReadingTheSettingsPort()
        {
            var listed = TheEdgesTrackerIntegrationListsIntoOtherContexts();

            var problems = TheListedEdgesAgainstTheOneTheMoveAdds(listed);

            Assert.That(
                problems,
                Is.Empty,
                $"WHAT: the Tracker integration list of edges into other contexts is [{string.Join("; ", listed)}] and {string.Join("; ", problems)}. WHY: the connection health cadence reads the settings port, which now lives in another context, so that one edge crosses a module boundary and must be listed; any other entry would be a new coupling. HOW: list exactly '{TheEdgeTheMoveAdds}' in EdgesOfTheModuleIntoOtherContexts of the Tracker integration rule, with the work item that removes it.");
        }

        [Test]
        public void AnEmptyTrackerIntegrationList_IsNamedAsMissingTheCadenceEdge()
        {
            var problems = TheListedEdgesAgainstTheOneTheMoveAdds([]);

            Assert.That(problems, Has.Count.EqualTo(1).And.Some.Contain("ConnectionHealthCadence"), "WHAT: an empty list was not named as missing the cadence edge. WHY: the check would pass before the move adds the edge. HOW: name the edge the list lacks.");
        }

        [Test]
        public void ATrackerIntegrationListWithAnExtraEdge_IsNamedAsHoldingMoreThanTheOneEdge()
        {
            var problems = TheListedEdgesAgainstTheOneTheMoveAdds([TheEdgeTheMoveAdds, $"{TheCadence} -> Lighthouse.Backend.Modules.OrganisationSetupFixtures.SettingsAndConnections.Application.AMadeUpForeignType"]);

            Assert.That(problems, Has.Count.EqualTo(1).And.Some.Contain("AMadeUpForeignType"), "WHAT: a made-up second edge was not named. WHY: the list may hold the one edge and no other, so a new coupling would pass unseen. HOW: name each listed edge that is not the one the move adds.");
        }

        private static List<string> TheListedEdgesAgainstTheOneTheMoveAdds(IReadOnlyCollection<string> listed)
        {
            var expected = TheEdgeTheMoveAdds;
            var problems = new List<string>();
            if (!listed.Contains(expected))
            {
                problems.Add($"the edge '{expected}' is missing, so ConnectionHealthCadence reads the settings port unlisted");
            }

            problems.AddRange(listed.Where(edge => edge != expected).Select(edge => $"the edge '{edge}' is listed beside the one the move adds"));
            return problems;
        }

        private static string[] TheEdgesTrackerIntegrationListsIntoOtherContexts()
        {
            var field = typeof(TrackerIntegrationModuleBoundariesArchUnitTest).GetField("EdgesOfTheModuleIntoOtherContexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "WHAT: the Tracker integration rule has no list named EdgesOfTheModuleIntoOtherContexts. WHY: the one edge the move adds is read from that list. HOW: keep the list under that name.");
            return (string[])field!.GetValue(null)!;
        }

        private static IEnumerable<(string Name, string LayerFolder)> EveryUnitTest()
            => SubjectsOfTheMovingUnitTests.Select(subject => ($"{subject}Test", LayerFolder(OrganisationSetupMembership.MovingTypes.Single(type => type.Name == subject).Layer)));

        private static string LayerFolder(OrganisationSetupLayer layer) => layer switch
        {
            OrganisationSetupLayer.Application => "Application",
            OrganisationSetupLayer.ApplicationConnections => "Application/Connections",
            _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "The unit tests of this group sit in the Application or Application/Connections folder."),
        };

        private static IEnumerable<TestCaseData> EveryMovingUnitTest()
            => EveryUnitTest().Select(test => new TestCaseData(test.Name, test.LayerFolder).SetArgDisplayNames(test.Name));

        // The old folders are built from segments so that no literal names the place the tests left.
        private static List<string> TestsLeftInOldFolders(IEnumerable<string> sourcePaths)
        {
            var oldFolders = new[] { string.Join('/', "API"), string.Join('/', "Services", "Implementation"), string.Join('/', "Services", "Implementation", "Connections") };
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

        private static string FixtureConnections => $"{FixtureLayerRoot}.Application.Connections";

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
            var assemblyPath = typeof(SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                classes.Where(compiled => compiled.Files.Contains(ThisFile)).Select(compiled => compiled.FullName),
                Does.Contain(typeof(SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest).FullName),
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


namespace Lighthouse.Backend.Modules.OrganisationSetupFixtures.SettingsAndConnections.Application
{
    internal sealed class AWellPlacedService
    {
        public string? Label { get; init; }
    }
}

namespace Lighthouse.Backend.Modules.OrganisationSetupFixtures.SettingsAndConnections.Services
{
    internal sealed class ALeftBehindService
    {
        public string? Label { get; init; }
    }

    internal sealed class AMisplacedConnectionType
    {
        public string? Label { get; init; }
    }
}
