using System.Text.RegularExpressions;
using Lighthouse.Backend.Tests.Architecture.OrganisationSetupRulesNameOnlyTheModuleFixtures;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Organisation set-up now lives in Lighthouse.Backend.Modules.OrganisationSetup, so the rules name it by that module and by
    /// nothing else. The membership check widened every rule that sees set-up to its old or its new name while the types moved;
    /// this check runs the same law and sweep with an empty rename list: no rule that names set-up by its module still matches an
    /// old namespace or full name of a moved type, no string of the test project still names a moved type by its old full name
    /// or path, and the rules that stand for an old place still see no moved type in its new place.
    /// <para>
    /// The sweep leaves out, by exact path, the files that must spell the old names to do their job. That list is itself asserted,
    /// so a rule file cannot hide behind it.
    /// </para>
    /// </summary>
    [TestFixture]
    public class OrganisationSetupRulesNameOnlyTheModuleArchUnitTest
    {
        // Properties, not constants: the other moves' checks read every string field of the test assembly as a rule pattern.
        private static string Architecture => "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/";

        // The Context fields that name the whole backend, a project folder or all of Services, modules and platform alike: they see
        // every loose type and every module, so the move leaves them as they are. Every other Context field is judged.
        private static readonly string[] TheBroadContextFields =
        [
            "AccessAndLicensingUnitTestPlacementTest.ProductionProjectDirectory",
            "AccessHttpAdaptersLayoutArchUnitTest.TheWholeAssembly",
            "AdditionalFieldInUseRulePurityArchUnitTest.AnythingOfLighthouseOutsideModels",
            "ApplicationCoreRules.ApplicationCore",
            "CalendarDayAnchorSeamArchUnitTest.ProductionProjectDirectory",
            "CategoryListOwnershipGuardTest.ProductionProject",
            "ConnectorsDeclareNoUnsupportedMembersArchUnitTest.AllOfTheBackend",
            "DependencySingleDecisionArchUnitTest.BackendProjectDirectory",
            "FastWithManyFeaturesArchUnitTest.ProductionProjectDirectory",
            "JiraRouteLiteralsTest.ProductionProjectDirectory",
            "OverTimeReconstructionSeamArchUnitTest.ProductionProjectDirectory",
            "ProductionSource.ProductionProjectDirectory",
            "PromisesLayoutArchUnitTest.TheWholeAssembly",
            "PromisesModuleBoundariesArchUnitTest.OutsideTrackerIntegration",
            "PromisesUnitTestPlacementTest.ProductionProjectDirectory",
            "RealityCheckReadOnlyArchUnitTest.ImplementationPattern",
            "SecretReadPathSourceStructureTest.ProductionProjectDirectory",
            "SecretRotationWriteSurfaceTest.ProductionProjectDirectory",
            "StalenessPolicyPurityArchUnitTest.ServicesPattern",
            "TrackerIntegrationApplicationLayoutArchUnitTest.TheWholeAssembly",
            "TrackerIntegrationUnitTestPlacementTest.ProductionProjectDirectory",
            "UpdateCancellationContextWriterArchUnitTest.ProductionProjectDirectory",
            "ZonedTimestampParsingArchUnitTest.ProductionProjectDirectory",
        ];

        private static string TheModuleNamespace => @"^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)";

        // OrganisationSetup named right after Modules\. in a rule pattern, directly or as one choice of a group.
        private static readonly Regex ModuleToken = new(@"(?<=Modules\\\.(?:\((?:[A-Za-z]+\|)*)?)OrganisationSetup(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout);

        // The files that spell old names on purpose: this check with its planted offenders, the map, the roles, the rename list
        // and the membership check with its made-up types.
        private static readonly string[] TheSweepExclusions =
        [
            Architecture + $"{nameof(OrganisationSetupRulesNameOnlyTheModuleArchUnitTest)}.cs",
            Architecture + $"{nameof(OrganisationSetupMembership)}.cs",
            Architecture + "OrganisationSetupMembershipArchUnitTest.cs",
            Architecture + $"{nameof(OrganisationSetupPatternRoles)}.cs",
            Architecture + $"{nameof(OrganisationSetupStringsTheMoveRenames)}.cs",
        ];

        private static readonly NamespaceMove TheMoveWithNoRenames = new(
            [.. OrganisationSetupMembership.MovingTypes.Select(type => new MovingType(type.Name, "OrganisationSetup", type.OldNamespace, type.NewNamespace))],
            [],
            TheModuleNamespace,
            ModuleToken,
            ["Connections", "OptionalFeatures"]);

        private static readonly NamespaceMoveLaw TheLawWithNoRenames = new(
            TheMoveWithNoRenames,
            new PatternRoleMap(OrganisationSetupPatternRoles.PatternRoles, OrganisationSetupPatternRoles.FolderOnlyMatches));

        [Test]
        public void EveryRuleThatNamesOrganisationSetUpByItsModule_NamesNoOldNamespaceOrFullNameOfAMovedType()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var stillOld = WhenTheContextFieldsAreAskedAboutTheOldNames(patterns, OrganisationSetupPatternRoles.PatternRoles);

            ThenNoContextFieldStillMatchesAnOldName(stillOld);
        }

        [Test]
        public void NoStringOfTheTestProject_StillNamesAMovedTypeByItsOldFullNameOrPath()
        {
            var sources = GivenTheSourcesOfTheTestProject(TheSweepExclusions);
            var governed = TheLawWithNoRenames.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly().Where(pattern => OrganisationSetupPatternRoles.PatternRoles.ContainsKey(pattern.Field)));

            var stillOld = WhenTheSourcesAreSweptForOldNames(sources, governed);

            ThenNoStringStillNamesAnOldFullNameOrPath(stillOld);
        }

        [Test]
        public void TheSweepExclusions_AreExactlyTheFilesThatMustSpellTheOldNamesAndEachExists()
        {
            var problems = WhenTheExclusionsAreComparedWithTheFiles(TheSweepExclusions, GivenTheFilesThatMustSpellTheOldNames());

            ThenTheExclusionsAreExactlyTheFiles(problems);
        }

        [Test]
        public void EveryRuleThatStandsForAnOldPlace_StillSeesNoMovedTypeInItsNewPlace()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var seeingTheNewPlace = WhenThePlaceFieldsAreAskedAboutTheNewNames(patterns, OrganisationSetupPatternRoles.PatternRoles);

            ThenNoPlaceFieldSeesAMovedTypeInItsNewPlace(seeingTheNewPlace);
        }

        [Test]
        public void TheBroadContextFields_AreExactlyTheWholeBackendFieldsAndEachIsADeclaredContextField()
        {
            var expected = GivenTheBroadContextFieldsThatSeeEveryLooseTypeAndEveryModule();

            var problems = WhenTheBroadFieldsAreComparedWithTheDeclaredRoles(TheBroadContextFields, expected, OrganisationSetupPatternRoles.PatternRoles);

            ThenTheBroadFieldsAreExactlyTheExpectedDeclaredContextFields(problems);
        }

        [Test]
        public void TheContextCheck_NamesAFieldThatKeepsAnOldAlternativeAndPassesTheNarrowedOne()
        {
            var patterns = GivenThePlantedContextFields();

            var stillOld = WhenTheContextFieldsAreAskedAboutTheOldNames(patterns, PlantedFields.Roles);

            ThenOnlyThePlantedFieldWithTheOldAlternativeIsNamed(stillOld);
        }

        [Test]
        public void TheLiteralSweep_NamesAnOldFullNameInARuleFileAndPassesTheNewOne()
        {
            var sources = GivenAPlantedRuleFile();

            var stillOld = WhenTheSourcesAreSweptForOldNames(sources, []);

            ThenOnlyThePlantedOldFullNameIsNamed(stillOld);
        }

        [Test]
        public void TheExclusionCheck_NamesAnExtraPathAndAPathThatDoesNotExist()
        {
            var expected = GivenTheFilesThatMustSpellTheOldNames();
            var exclusions = GivenExclusionsWithARealRuleFileAndAMadeUpPath(expected);

            var problems = WhenTheExclusionsAreComparedWithTheFiles(exclusions, expected);

            ThenTheRuleFileAndTheMadeUpPathAreNamed(problems, exclusions);
        }

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var checkItself = new[]
            {
                typeof(OrganisationSetupRulesNameOnlyTheModuleArchUnitTest),
                typeof(OrganisationSetupMembership),
                typeof(OrganisationSetupType),
                typeof(OrganisationSetupPatternRoles),
                typeof(OrganisationSetupStringsTheMoveRenames),
                typeof(NamespaceMoveLaw),
            };
            var patterns = TheLawWithNoRenames.RulePatternsOf(typeof(OrganisationSetupRulesNameOnlyTheModuleArchUnitTest).Assembly.GetTypes()
                .Where(type => !InFixtures(type) && !checkItself.Contains(type) && !checkItself.Contains(type.DeclaringType!)));

            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every class, nested ones included.");
            return patterns;
        }

        private static List<(string RelativePath, string Source)> GivenTheSourcesOfTheTestProject(IReadOnlyCollection<string> excluded)
        {
            var root = RepositoryRoot();
            var testProject = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests");
            var buildOutput = new[] { Path.Combine(testProject, "bin") + Path.DirectorySeparatorChar, Path.Combine(testProject, "obj") + Path.DirectorySeparatorChar };

            var files = Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                .Where(path => !buildOutput.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Select(path => (RelativePath: Path.GetRelativePath(root, path).Replace('\\', '/'), Path: path))
                .Where(file => !excluded.Contains(file.RelativePath, StringComparer.Ordinal))
                .Select(file => (file.RelativePath, File.ReadAllText(file.Path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(500), $"Only {files.Count} source files were found under {testProject}, so the sweep would pass without reading the test project.");
            return files;
        }

        private static string[] GivenTheFilesThatMustSpellTheOldNames()
            =>
            [
                Architecture + "OrganisationSetupRulesNameOnlyTheModuleArchUnitTest.cs",
                Architecture + "OrganisationSetupMembership.cs",
                Architecture + "OrganisationSetupMembershipArchUnitTest.cs",
                Architecture + "OrganisationSetupPatternRoles.cs",
                Architecture + "OrganisationSetupStringsTheMoveRenames.cs",
            ];

        private static string[] GivenTheBroadContextFieldsThatSeeEveryLooseTypeAndEveryModule()
            =>
            [
                "AccessAndLicensingUnitTestPlacementTest.ProductionProjectDirectory",
                "AccessHttpAdaptersLayoutArchUnitTest.TheWholeAssembly",
                "AdditionalFieldInUseRulePurityArchUnitTest.AnythingOfLighthouseOutsideModels",
                "ApplicationCoreRules.ApplicationCore",
                "CalendarDayAnchorSeamArchUnitTest.ProductionProjectDirectory",
                "CategoryListOwnershipGuardTest.ProductionProject",
                "ConnectorsDeclareNoUnsupportedMembersArchUnitTest.AllOfTheBackend",
                "DependencySingleDecisionArchUnitTest.BackendProjectDirectory",
                "FastWithManyFeaturesArchUnitTest.ProductionProjectDirectory",
                "JiraRouteLiteralsTest.ProductionProjectDirectory",
                "OverTimeReconstructionSeamArchUnitTest.ProductionProjectDirectory",
                "ProductionSource.ProductionProjectDirectory",
                "PromisesLayoutArchUnitTest.TheWholeAssembly",
                "PromisesModuleBoundariesArchUnitTest.OutsideTrackerIntegration",
                "PromisesUnitTestPlacementTest.ProductionProjectDirectory",
                "RealityCheckReadOnlyArchUnitTest.ImplementationPattern",
                "SecretReadPathSourceStructureTest.ProductionProjectDirectory",
                "SecretRotationWriteSurfaceTest.ProductionProjectDirectory",
                "StalenessPolicyPurityArchUnitTest.ServicesPattern",
                "TrackerIntegrationApplicationLayoutArchUnitTest.TheWholeAssembly",
                "TrackerIntegrationUnitTestPlacementTest.ProductionProjectDirectory",
                "UpdateCancellationContextWriterArchUnitTest.ProductionProjectDirectory",
                "ZonedTimestampParsingArchUnitTest.ProductionProjectDirectory",
            ];

        private static List<string> WhenTheBroadFieldsAreComparedWithTheDeclaredRoles(string[] broad, string[] expected, IReadOnlyDictionary<string, PatternDeclaration> roles)
            =>
            [
                .. broad.Except(expected, StringComparer.Ordinal).Select(name => $"exempt but not an expected broad field: {name}"),
                .. expected.Except(broad, StringComparer.Ordinal).Select(name => $"an expected broad field that is not exempt: {name}"),
                .. broad.Where(name => Resolve(roles, name, out _) is not { Role: PatternRole.Context }).Select(name => $"exempt but not a declared Context field: {name}"),
            ];

        private static void ThenTheBroadFieldsAreExactlyTheExpectedDeclaredContextFields(List<string> problems)
            => Assert.That(
                problems,
                Is.Empty,
                "The Context fields the check leaves unjudged differ from the expected broad fields, or one is not declared:" + Environment.NewLine +
                string.Join(Environment.NewLine, problems) + Environment.NewLine +
                "Why it matters: an exempt field is never asked about the old names, so a set-up-specific field on the list would keep an old alternative unseen. " +
                "How to fix: list only fields that name the whole backend, a project folder or all of Services and modules, edit both lists together, and give each its declared Context role.");

        private static string[] GivenExclusionsWithARealRuleFileAndAMadeUpPath(string[] expected)
            => [.. expected, Architecture + "WorkItemsModuleBoundariesArchUnitTest.cs", Architecture + "AMadeUpFileThatIsNotThere.cs"];

        private static List<RulePattern> GivenThePlantedContextFields()
            =>
            [
                new(PlantedFields.TheOldAlternativeKept, @"^Lighthouse\.Backend\.(Services\.Interfaces|Modules\.OrganisationSetup\.Application)$"),
                new(PlantedFields.TheTokenlessOldOnly, @"^Lighthouse\.Backend\.Services\.Interfaces$"),
                new(PlantedFields.TheServicesAlternatedWithAModule, @"^Lighthouse\.Backend\.(Services|Modules\.OrganisationSetup)(\.|$)"),
                new(PlantedFields.TheNarrowedToTheModule, @"^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)"),
            ];

        private static List<(string RelativePath, string Source)> GivenAPlantedRuleFile()
            =>
            [(
                Architecture + "APlantedRuleFile.cs",
                string.Join(
                    Environment.NewLine,
                    $"var stillOld = \"{string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IAppSettingService")}\";",
                    $"var alreadyRenamed = \"{string.Join('.', "Lighthouse.Backend.Modules.OrganisationSetup.Application", "IAppSettingService")}\";",
                    $"var aFolderThatStays = \"{string.Join('.', "Lighthouse.Backend.Services.Implementation", "SomethingThatStays")}\";"))
            ];

        private static List<string> WhenTheContextFieldsAreAskedAboutTheOldNames(List<RulePattern> patterns, IReadOnlyDictionary<string, PatternDeclaration> roles)
        {
            var folderOnly = OrganisationSetupPatternRoles.FolderOnlyMatches;
            var found = new List<string>();

            foreach (var pattern in patterns.Where(pattern => !TheBroadContextFields.Contains(pattern.Field, StringComparer.Ordinal)))
            {
                var declared = Resolve(roles, pattern.Field, out var components);
                if (declared is not { Role: PatternRole.Context, Subject: { } subject })
                {
                    continue;
                }

                var old = TheMoveWithNoRenames.MovingTypes
                    .Where(type => !components.Any(component => folderOnly.ContainsKey((component, type.Name))))
                    .Where(type => NamespaceMoveLaw.IsMatch(subject == PatternSubject.Namespace ? type.OldNamespace : type.OldFullName, pattern.Value))
                    .Select(type => subject == PatternSubject.Namespace ? type.OldNamespace : type.OldFullName)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (old.Count > 0)
                {
                    found.Add($"{pattern.Field} = {pattern.Value} still matches the old {subject} {string.Join(", ", old)}");
                }
            }

            return found;
        }

        private static List<string> WhenThePlaceFieldsAreAskedAboutTheNewNames(List<RulePattern> patterns, IReadOnlyDictionary<string, PatternDeclaration> roles)
        {
            var places = patterns
                .Where(pattern => Resolve(roles, pattern.Field, out _) is { Role: PatternRole.Place })
                .ToList();

            Assert.That(places, Is.Not.Empty, "No rule pattern is declared as an old place, so the check would pass without looking at one. How to fix: read the declared roles of the rule patterns.");

            return [.. from pattern in places
                let subject = Resolve(roles, pattern.Field, out _)!.Subject
                from type in TheMoveWithNoRenames.MovingTypes
                let name = subject == PatternSubject.Namespace ? type.NewNamespace : type.NewFullName
                where NamespaceMoveLaw.IsMatch(name, pattern.Value)
                select $"{pattern.Field} = {pattern.Value} sees {type.Name} as {name}"];
        }

        private static List<string> WhenTheSourcesAreSweptForOldNames(List<(string RelativePath, string Source)> sources, HashSet<string> governed)
            => [.. sources.SelectMany(file => TheLawWithNoRenames.OldPathStringsIn(file.RelativePath, file.Source, governed)).Distinct(StringComparer.Ordinal)];

        private static List<string> WhenTheExclusionsAreComparedWithTheFiles(string[] exclusions, string[] expected)
        {
            var root = RepositoryRoot();

            return
            [
                .. exclusions.Except(expected, StringComparer.Ordinal).Select(path => $"excluded but not one of the files that must spell the old names: {path}"),
                .. expected.Except(exclusions, StringComparer.Ordinal).Select(path => $"a file that must spell the old names but is not excluded: {path}"),
                .. exclusions.Where(path => !File.Exists(Path.Combine(root, path))).Select(path => $"excluded but does not exist: {path}"),
            ];
        }

        private static void ThenNoContextFieldStillMatchesAnOldName(List<string> stillOld)
            => Assert.That(
                stillOld,
                Is.Empty,
                "These rule patterns name Organisation set-up by its module and still match an old Services namespace or full name of a moved type:" + Environment.NewLine +
                string.Join(Environment.NewLine, stillOld) + Environment.NewLine +
                "Why it matters: the move is over, so an old alternative keeps a rule naming a place nothing lives in any more, and lets a type put back there pass for set-up. " +
                @"How to fix: narrow the pattern to ^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$) (a full-name pattern to the type's new full name); a loose type that stays and is still reached gets its own constant and list.");

        private static void ThenNoStringStillNamesAnOldFullNameOrPath(List<string> stillOld)
            => Assert.That(
                stillOld,
                Is.Empty,
                "These strings of the test project still name a moved Organisation set-up type by its old full name or path:" + Environment.NewLine +
                string.Join(Environment.NewLine, stillOld) + Environment.NewLine +
                "Why it matters: a rule or list that names the old path checks a place nothing lives in any more, so it passes without looking at the module. " +
                "How to fix: rename the string to the type's new full name or path, or build it at run time from the type.");

        private static void ThenTheExclusionsAreExactlyTheFiles(List<string> problems)
            => Assert.That(
                problems,
                Is.Empty,
                "The files the sweep leaves out differ from the files that must spell the old names:" + Environment.NewLine +
                string.Join(Environment.NewLine, problems) + Environment.NewLine +
                "Why it matters: a rule file on the list is not swept, so a real rule could keep an old name behind it. " +
                "How to fix: leave out only this check, the map, the roles, the rename list and the membership check, and give each its current path.");

        private static void ThenNoPlaceFieldSeesAMovedTypeInItsNewPlace(List<string> seeing)
            => Assert.That(
                seeing,
                Is.Empty,
                "These rule patterns stand for an old place and now match a moved type in its new place:" + Environment.NewLine +
                string.Join(Environment.NewLine, seeing) + Environment.NewLine +
                "Why it matters: a rule that asks whether a type is still left in the old place would then find the module and report it as left behind. " +
                "How to fix: leave the old place as it was; do not add the module to it.");

        private static void ThenOnlyThePlantedFieldWithTheOldAlternativeIsNamed(List<string> stillOld)
            => Assert.That(
                stillOld,
                Has.Count.EqualTo(3).And.Some.Contains(PlantedFields.TheOldAlternativeKept).And.Some.Contains(PlantedFields.TheTokenlessOldOnly).And.Some.Contains(PlantedFields.TheServicesAlternatedWithAModule),
                $"The check named [{string.Join(" | ", stillOld)}]; it should name exactly the three planted fields that still see the old place (an old alternative, only the old place, Services alternated with the module), and pass the narrowed one. Why it matters: a check that names nothing lets an old alternative stay behind, and one that names the narrowed field fails a finished move. How to fix: match every module-naming Context field against the old subject of each moved type.");

        private static void ThenOnlyThePlantedOldFullNameIsNamed(List<string> stillOld)
            => Assert.That(
                stillOld,
                Has.Count.EqualTo(1).And.Some.Contains(string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IAppSettingService")),
                $"The sweep named [{string.Join(" | ", stillOld)}]; it should name only the planted old full name. Why it matters: a sweep that names nothing lets an old path stay behind, and one that names the new form or a folder that stays fails a finished move. How to fix: name every literal that spells a moved type's old full name or path and no other.");

        private static void ThenTheRuleFileAndTheMadeUpPathAreNamed(List<string> problems, string[] exclusions)
            => Assert.That(
                problems,
                Has.Count.EqualTo(3)
                    .And.Some.Contains($"not one of the files that must spell the old names: {exclusions[^2]}")
                    .And.Some.Contains($"not one of the files that must spell the old names: {exclusions[^1]}")
                    .And.Some.Contains($"excluded but does not exist: {exclusions[^1]}"),
                $"The exclusion check named [{string.Join(" | ", problems)}]; it should name the real rule file added to the list, and the made-up path as both extra and missing. Why it matters: a check that accepts an extra path lets a rule file hide from the sweep. How to fix: compare the list with the expected files both ways and test that each exists.");

        private static Declared? Resolve(IReadOnlyDictionary<string, PatternDeclaration> roles, string field, out List<string> components)
        {
            components = [];
            var key = field;
            while (roles.TryGetValue(key, out var declaration) && !components.Contains(key))
            {
                components.Add(key);
                if (declaration.CompositeOf is null)
                {
                    return declaration.Subject is { } subject && declaration.Role is { } role ? new Declared(subject, role) : null;
                }

                key = declaration.CompositeOf;
            }

            return null;
        }

        private static bool InFixtures(Type type) => type.Namespace?.StartsWith(typeof(PlantedFields).Namespace!, StringComparison.Ordinal) == true;

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root, the folder holding Lighthouse.Backend/Lighthouse.sln, was not found above the test directory.");
        }

        private sealed record Declared(PatternSubject Subject, PatternRole Role);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OrganisationSetupRulesNameOnlyTheModuleFixtures
{
    /// <summary>Planted rule patterns the Context check is run over: one that keeps an old alternative, one narrowed to the module.</summary>
    internal static class PlantedFields
    {
        public static string TheOldAlternativeKept => "PlantedFields.TheOldAlternativeKept";

        public static string TheTokenlessOldOnly => "PlantedFields.TheTokenlessOldOnly";

        public static string TheServicesAlternatedWithAModule => "PlantedFields.TheServicesAlternatedWithAModule";

        public static string TheNarrowedToTheModule => "PlantedFields.Nested.TheNarrowedToTheModule";

        public static IReadOnlyDictionary<string, PatternDeclaration> Roles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            [TheOldAlternativeKept] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that kept its old alternative"),
            [TheTokenlessOldOnly] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that never named the module, only the old place"),
            [TheServicesAlternatedWithAModule] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code alternating all of Services with the module, not on the broad list"),
            [TheNarrowedToTheModule] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code narrowed to the module"),
        };
    }
}
