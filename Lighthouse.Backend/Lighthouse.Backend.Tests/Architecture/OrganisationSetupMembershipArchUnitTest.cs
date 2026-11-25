using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.OrganisationSetupMembershipFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Organisation set-up types are about to move out of the loose Services namespaces into
    /// Lighthouse.Backend.Modules.OrganisationSetup. Before anything moves, the rules see the move: the membership is measured
    /// from the compiled code, every rule pattern that matches a moving type's old name carries a declared role (a part of the
    /// code that must follow the type, or an old location that must not), and every string of the test project that names a
    /// moving type by its old Services path is on a fixed list of strings the move renames.
    /// <para>
    /// The loose namespaces hold types that stay, so a pattern on them is ambiguous: it may stand for set-up or for the folder.
    /// An undeclared pattern that sees a moving type fails until its role is declared.
    /// </para>
    /// </summary>
    [TestFixture]
    public class OrganisationSetupMembershipArchUnitTest
    {
        private const string TheArea = "OrganisationSetup";

        private const string TheParentNamespace = "Lighthouse.Backend.Modules";

        private static readonly string TheModuleNamespace = @"^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)";

        // Marks OrganisationSetup named right after Modules\. in a rule pattern, directly or as one choice of a group, so
        // that an alternative written for the move can be told from the old ones.
        private static readonly Regex ModuleToken = new(@"(?<=Modules\\\.(?:\((?:[A-Za-z]+\|)*)?)OrganisationSetup(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout);

        private static readonly string FixtureNamespace = typeof(MadeUpSetUpRuleKeys).Namespace!;

        private static readonly Type[] TheCheckItself =
        [
            typeof(OrganisationSetupMembershipArchUnitTest),
            typeof(OrganisationSetupMembership),
            typeof(OrganisationSetupType),
            typeof(OrganisationSetupPatternRoles),
            typeof(OrganisationSetupStringsTheMoveRenames),
            typeof(NamespaceMoveLaw),
        ];

        // The map, the roles and this check name the set-up types by their old Services path on purpose, so they hold
        // before, during and after the move; they are the measurement, not rules the move could leave behind.
        private static readonly string[] TheMeasurementFiles =
        [
            $"{nameof(OrganisationSetupMembership)}.cs",
            $"{nameof(OrganisationSetupPatternRoles)}.cs",
            $"{nameof(OrganisationSetupStringsTheMoveRenames)}.cs",
            $"{nameof(OrganisationSetupMembershipArchUnitTest)}.cs",
            $"{nameof(NamespaceMoveLaw)}.cs",
        ];

        private static readonly NamespaceMove TheSetUpMove = MoveWith(OrganisationSetupStringsTheMoveRenames.Entries);

        private static readonly NamespaceMoveLaw TheLaw = new(TheSetUpMove, new PatternRoleMap(OrganisationSetupPatternRoles.PatternRoles, OrganisationSetupPatternRoles.FolderOnlyMatches));

        private static readonly NamespaceMoveLaw TheLawOverTheMadeUpKeys = new(TheSetUpMove, new PatternRoleMap(MadeUpSetUpRuleRoles.Roles, new Dictionary<(string Field, string Type), string>()));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(OrganisationSetupMembershipArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryMovingSetUpType_IsFoundInItsOldOrItsNewNamespace()
        {
            var map = GivenTheSetUpMap();

            var notFound = WhenTheMapIsLookedUpInTheProductionCode(map);

            ThenEveryListedTypeIsFound(notFound);
        }

        [Test]
        public void TheMembershipCheck_NamesAListedTypeFoundInNeitherNamespace()
        {
            var map = GivenTheSetUpMapWithAMadeUpType();

            var notFound = WhenTheMapIsLookedUpInTheProductionCode(map);

            ThenOnlyTheMadeUpTypeIsNamedAsNotFound(notFound);
        }

        [Test]
        public void EachDomainCandidate_ReachesOnlyTheKernelTheBaseLibraryAndTheOtherDomainCandidates()
        {
            var candidates = GivenTheDomainCandidates();

            var check = WhenTheDomainCandidatesAreRead(ProductionModule.Value, candidates);

            ThenEveryCandidateIsFoundAndReachesNothingElse(check);
        }

        [Test]
        public void TheDomainCheck_NamesAMadeUpCandidateThatReachesTheDatabase()
        {
            var candidates = GivenAMadeUpDomainCandidateThatReachesTheDatabase();

            var check = WhenTheDomainCandidatesAreRead(TestModule.Value, candidates);

            ThenTheMadeUpCandidateIsNamedWithTheDatabase(check);
        }

        [Test]
        public void EveryStayingSetUpFile_IsNamedWithItsOwnerAndReason_AndExists()
        {
            var staying = OrganisationSetupMembership.FilesThatStay;

            var unsound = WhenTheStayingFilesAreLookedUp(staying);

            ThenEveryStayingFileIsSound(unsound);
        }

        [Test]
        public void TheStayingCheck_NamesAMadeUpFileThatDoesNotExistAndOneWithoutAnOwner()
        {
            var staying = GivenMadeUpStayingFiles();

            var unsound = WhenTheStayingFilesAreLookedUp(staying);

            ThenBothMadeUpStayingFilesAreNamed(unsound);
        }

        [Test]
        public void TheReferencesTheMoveTakesOutOfNamespaceScope_AreExactlyTheListedOnes()
        {
            var listed = OrganisationSetupMembership.ImplicitEdges;

            var measured = WhenTheImplicitEdgesAreMeasured();

            ThenTheMeasuredEdgesAreExactlyTheListedOnes(measured, listed);
        }

        [Test]
        public void EveryRulePatternThatSeesAMovingSetUpType_IsDeclared_AndKeepsItsRoleOnTheSubjectItsRuleReads()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var check = WhenTheRoleCheckRuns(TheLaw, patterns, OrganisationSetupPatternRoles.WholeParentPatterns, OrganisationSetupPatternRoles.NamedByTheModuleOnly);

            ThenNoPatternBreaksItsRole(check.Breaks);
            ThenEveryWholeParentPatternTakesInAllOfModules(check.NarrowerThanTheParent);
            ThenEveryPatternNamedByTheModuleOnlySeesSetUpOnlyInItsModule(check.UnsoundModuleOnlyPatterns);
        }

        [Test]
        public void TheRoleCheck_NamesEachMadeUpPatternThatBreaksItsRole_AndPassesTheOnesThatKeepIt()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var breaks = WhenTheRoleCheckRuns(TheLawOverTheMadeUpKeys, patterns, new Dictionary<string, string>(), new Dictionary<string, string>()).Breaks;

            ThenTheMadeUpBreaksNamedAre(
                breaks,
                [
                    ($"{nameof(MadeUpSetUpRuleKeys)}.{nameof(MadeUpSetUpRuleKeys.AContextThatSeesOnlyTheOldPlace)}", RoleBreak.StopsSeeingTheType),
                    ($"{nameof(MadeUpSetUpRuleKeys)}.{nameof(MadeUpSetUpRuleKeys.APlaceWidenedIntoTheNewPlace)}", RoleBreak.SeesTheTypeInItsNewPlace),
                    ($"{nameof(MadeUpSetUpRuleKeys)}.{nameof(MadeUpSetUpRuleKeys.APlaceWidenedIntoTheNewPlace)}", RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation),
                    ($"{nameof(MadeUpSetUpRuleKeys)}.{nameof(MadeUpSetUpRuleKeys.Nested)}.{nameof(MadeUpSetUpRuleKeys.Nested.UndeclaredButSeesTheOldPlace)}", RoleBreak.NotDeclared),
                    ($"{nameof(MadeUpSetUpRuleKeys)}.{nameof(MadeUpSetUpRuleKeys.Nested)}.{nameof(MadeUpSetUpRuleKeys.Nested.ACompositeOfTheBlindContext)}", RoleBreak.StopsSeeingTheType),
                ]);
        }

        [Test]
        public void NoModulesOrganisationSetupAlternativeOfARulePattern_MatchesAProductionNamespaceOrTypeOutsideTheModule()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var reachingOutside = TheLaw.AlternativesReachingOutside(patterns, TheLaw.NamesOutsideTheNewPlace(ProductionModule.Value));

            Assert.That(
                reachingOutside,
                Is.Empty,
                "These rule patterns match a production namespace or type outside Lighthouse.Backend.Modules.OrganisationSetup only through their OrganisationSetup alternative:" + Environment.NewLine +
                string.Join(Environment.NewLine, reachingOutside) + Environment.NewLine +
                @"Why it matters: an alternative added for the move must see the moved types and nothing else. How to fix: write it as ^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$), anchored and ending at a namespace boundary.");
        }

        [Test]
        public void EveryTestStringThatNamesAMovingSetUpTypeByItsOldPath_IsOnTheRenameList_InItsOldOrItsNewForm()
        {
            var sources = GivenTheSourcesOfTheTestProject();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(TheLaw, OrganisationSetupStringsTheMoveRenames.Entries, sources, GivenThePatternValuesTheRoleCheckGoverns());

            ThenTheOldPathStringsAreExactlyTheList(comparison);
        }

        [Test]
        public void TheRenameListCheck_NamesAnUnlistedOldPathString_AndAListedStringFoundInNeitherForm()
        {
            var sources = GivenAMadeUpRuleFile();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(MadeUpRenameList.TheLaw, MadeUpRenameList.Entries, sources, []);

            ThenTheMadeUpFileIsComparedBothWays(comparison);
        }

        private static NamespaceMove MoveWith(IEnumerable<OrganisationSetupRename> renames)
            => new(
                [.. OrganisationSetupMembership.MovingTypes.Select(type => new MovingType(type.Name, TheArea, type.OldNamespace, type.NewNamespace))],
                [.. renames.Select(entry => entry.AsRenamedString())],
                TheModuleNamespace,
                ModuleToken,
                ["Connections", "OptionalFeatures"]);

        private static IReadOnlyList<OrganisationSetupType> GivenTheSetUpMap() => OrganisationSetupMembership.MovingTypes;

        private static IReadOnlyList<OrganisationSetupType> GivenTheSetUpMapWithAMadeUpType()
            => [.. OrganisationSetupMembership.MovingTypes, new OrganisationSetupType(MadeUpSetUpRuleKeys.AMadeUpTypeName, OrganisationSetupLayer.Application, "Lighthouse.Backend.Services.Interfaces")];

        private static IReadOnlyList<string> GivenTheDomainCandidates()
            => [.. OrganisationSetupMembership.MovingTypes
                .Where(type => OrganisationSetupMembership.DomainCandidates.Contains(type.Name, StringComparer.Ordinal))
                .SelectMany(type => new[] { type.OldFullName, type.NewFullName })];

        private static IReadOnlyList<string> GivenAMadeUpDomainCandidateThatReachesTheDatabase()
            => [typeof(Lighthouse.Backend.Modules.OrganisationSetupFixtures.Domain.AMadeUpDomainCandidate).FullName!];

        private static IReadOnlyList<OrganisationSetupFileThatStays> GivenMadeUpStayingFiles()
            =>
            [
                OrganisationSetupMembership.FilesThatStay[0],
                new("Services/Implementation/AMadeUpFileThatIsNotThere.cs", "Organisation set-up", "a made-up file that does not exist"),
                OrganisationSetupMembership.FilesThatStay[0] with { Owner = " " },
            ];

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var patterns = TheLaw.RulePatternsOf(typeof(OrganisationSetupMembershipArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type) && !TheCheckItself.Contains(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every class, nested ones included.");
            return patterns;
        }

        private static List<RulePattern> GivenTheMadeUpRulePatterns()
            => TheLaw.RulePatternsOf(typeof(OrganisationSetupMembershipArchUnitTest).Assembly.GetTypes().Where(type => type.Namespace == FixtureNamespace));

        private static HashSet<string> GivenThePatternValuesTheRoleCheckGoverns()
            => TheLaw.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly().Where(pattern => OrganisationSetupPatternRoles.PatternRoles.ContainsKey(pattern.Field)));

        private static List<(string RelativePath, string Source)> GivenTheSourcesOfTheTestProject()
        {
            var root = RepositoryRoot();
            var testProject = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests");
            var buildOutput = new[] { Path.Combine(testProject, "bin") + Path.DirectorySeparatorChar, Path.Combine(testProject, "obj") + Path.DirectorySeparatorChar };

            var files = Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                .Where(path => !buildOutput.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Where(path => !TheMeasurementFiles.Contains(Path.GetFileName(path), StringComparer.Ordinal))
                .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(500), $"Only {files.Count} source files were found under {testProject}, so the scan would pass without reading the test project.");
            return files;
        }

        private static List<(string RelativePath, string Source)> GivenAMadeUpRuleFile()
            => [(MadeUpRenameList.TheFile, MadeUpRenameList.TheSource)];

        private static List<string> WhenTheMapIsLookedUpInTheProductionCode(IReadOnlyList<OrganisationSetupType> map)
        {
            var module = ProductionModule.Value;
            return [.. map
                .Where(type => module.GetType(type.OldFullName) is null && module.GetType(type.NewFullName) is null)
                .Select(type => $"{type.Name}: neither {type.OldFullName} nor {type.NewFullName}")];
        }

        private static DomainCheck WhenTheDomainCandidatesAreRead(ModuleDefinition module, IReadOnlyList<string> candidates)
        {
            var namespaces = candidates.Select(name => Regex.Escape(name[..name.LastIndexOf('.')])).Distinct(StringComparer.Ordinal);
            var pattern = $"^({string.Join('|', namespaces)})$";
            var found = candidates.Where(name => module.GetType(name) is not null).ToList();
            var offenders = IlDependencies.DependentsIn(module, pattern, target => !MayBeReachedFromTheDomain(target, candidates))
                .Where(dependent => candidates.Contains(TopLevelOf(dependent.TypeName), StringComparer.Ordinal))
                .Select(dependent => $"{dependent.TypeName} reaches {string.Join(", ", dependent.Targets)}")
                .ToList();

            return new DomainCheck(found, offenders);
        }

        private static List<string> WhenTheStayingFilesAreLookedUp(IReadOnlyList<OrganisationSetupFileThatStays> staying)
        {
            var production = Path.Combine(RepositoryRoot(), "Lighthouse.Backend", "Lighthouse.Backend");
            return [.. staying
                .Where(file => string.IsNullOrWhiteSpace(file.Owner) || string.IsNullOrWhiteSpace(file.Reason) || !File.Exists(Path.Combine(production, file.File)))
                .Select(file => $"{file.File} (owner \"{file.Owner}\", reason \"{file.Reason}\")")];
        }

        private static List<string> WhenTheImplicitEdgesAreMeasured()
        {
            var movers = OrganisationSetupMembership.MovingTypes;
            OrganisationSetupType? MoverOf(string fullName)
                => movers.FirstOrDefault(type => type.OldFullName == fullName || type.NewFullName == fullName);

            return [.. IlDependencies.DependentsIn(ProductionModule.Value, @"^Lighthouse\.Backend(\.|$)", target => MoverOf(OutermostFullNameOf(target)) is not null)
                .SelectMany(dependent => dependent.Targets.Select(target => (Origin: TopLevelOf(dependent.TypeName), Target: MoverOf(TopLevelOf(target)))))
                .Where(edge => edge.Target is not null && MoverOf(edge.Origin) != edge.Target)
                .Where(edge =>
                {
                    var originMover = MoverOf(edge.Origin);
                    var oldNamespace = originMover?.OldNamespace ?? edge.Origin[..edge.Origin.LastIndexOf('.')];
                    var newNamespace = originMover?.NewNamespace ?? oldNamespace;
                    return IsInOrBelow(oldNamespace, edge.Target!.OldNamespace) && !IsInOrBelow(newNamespace, edge.Target.NewNamespace);
                })
                .Select(edge => $"{MoverOf(edge.Origin)?.Name ?? edge.Origin} -> {edge.Target!.Name}")
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];
        }

        // A pattern on the whole-parent list sees the moved types for the first time after the move, rightly, so that first
        // sight is not a break, as long as the pattern still takes in all of Modules.
        private static RoleCheck WhenTheRoleCheckRuns(NamespaceMoveLaw law, List<RulePattern> patterns, IReadOnlyDictionary<string, string> wholeParentPatterns, IReadOnlyDictionary<string, string> namedByTheModuleOnly)
        {
            var narrower = patterns
                .Where(pattern => wholeParentPatterns.ContainsKey(pattern.Field) && !TakesInAllOfModules(pattern.Value))
                .Select(pattern => $"{pattern.Field} = {pattern.Value}")
                .ToList();
            var admitted = patterns
                .Where(pattern => wholeParentPatterns.ContainsKey(pattern.Field) && TakesInAllOfModules(pattern.Value))
                .Select(pattern => pattern.Field)
                .ToHashSet(StringComparer.Ordinal);
            var allBreaks = law.RoleBreaksOf(patterns);
            var breaks = allBreaks
                .Where(found => !(found.Break == RoleBreak.SeesAnAreaItNeverSaw && (admitted.Contains(found.Pattern.Field) || namedByTheModuleOnly.ContainsKey(found.Pattern.Field))))
                .ToList();

            return new RoleCheck(breaks, narrower, UnsoundModuleOnlyPatterns(patterns, allBreaks, namedByTheModuleOnly));
        }

        // A pattern named by the module only must exist, see no moved type in its old place, and see at least one moved type
        // only in its new place; an entry that does none of that would admit a break the list was never meant for.
        private static List<string> UnsoundModuleOnlyPatterns(List<RulePattern> patterns, List<PatternBreak> breaks, IReadOnlyDictionary<string, string> namedByTheModuleOnly)
        {
            var seeingOnlyTheNewPlace = breaks.Where(found => found.Break == RoleBreak.SeesAnAreaItNeverSaw).Select(found => found.Pattern.Field).ToHashSet(StringComparer.Ordinal);

            return [.. namedByTheModuleOnly.Keys
                .Select(field => (Field: field, Pattern: patterns.FirstOrDefault(pattern => pattern.Field == field)))
                .Select(entry => entry.Pattern switch
                {
                    null => $"{entry.Field}: not a rule pattern of the test assembly",
                    var pattern when TheSetUpMove.MovingTypes.FirstOrDefault(type => NamespaceMoveLaw.IsMatch(type.OldNamespace, pattern.Value) || NamespaceMoveLaw.IsMatch(type.OldFullName, pattern.Value)) is { } seen
                        => $"{entry.Field} = {pattern.Value}: still sees {seen.Name} in its old place",
                    _ when !seeingOnlyTheNewPlace.Contains(entry.Field) => $"{entry.Field}: sees no moved type only in its new place",
                    _ => null,
                })
                .OfType<string>()];
        }

        private static OldPathComparison WhenTheOldPathStringsAreComparedWithTheList(NamespaceMoveLaw law, IReadOnlyList<OrganisationSetupRename> list, List<(string RelativePath, string Source)> sources, HashSet<string> governed)
        {
            var unlisted = sources
                .SelectMany(file => law.OldPathStringsIn(file.RelativePath, file.Source, governed))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var byFile = sources.ToDictionary(file => file.RelativePath, file => file.Source, StringComparer.Ordinal);
            var missing = list
                .Where(entry => !entry.MayLeave)
                .Where(entry => !byFile.TryGetValue(entry.File, out var source)
                    || (!source.Contains($"\"{entry.OldText}\"", StringComparison.Ordinal) && !source.Contains($"\"{entry.NewText}\"", StringComparison.Ordinal)))
                .Select(entry => $"{entry.File}: \"{entry.OldText}\" / \"{entry.NewText}\"")
                .ToList();

            return new OldPathComparison(unlisted, missing);
        }

        private static void ThenEveryListedTypeIsFound(List<string> notFound)
            => Assert.That(
                notFound,
                Is.Empty,
                "These moving Organisation set-up types exist in neither their old nor their new namespace:" + Environment.NewLine +
                string.Join(Environment.NewLine, notFound) + Environment.NewLine +
                "Why it matters: the rules that follow the move read this map, so a name that matches no type leaves a moved type unwatched. " +
                "How to fix: give the type its current name and old namespace in OrganisationSetupMembership, or move it only to the namespace its layer names.");

        private static void ThenOnlyTheMadeUpTypeIsNamedAsNotFound(List<string> notFound)
            => Assert.That(
                notFound,
                Is.EqualTo(new[] { $"{MadeUpSetUpRuleKeys.AMadeUpTypeName}: neither Lighthouse.Backend.Services.Interfaces.{MadeUpSetUpRuleKeys.AMadeUpTypeName} nor Lighthouse.Backend.Modules.OrganisationSetup.Application.{MadeUpSetUpRuleKeys.AMadeUpTypeName}" }),
                "The membership check should name exactly the made-up type, which exists in neither namespace. Why it matters: a check that names nothing passes a map that has lost a type. How to fix: report every listed type found in neither its old nor its new namespace.");

        private static void ThenEveryCandidateIsFoundAndReachesNothingElse(DomainCheck check)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    check.Found,
                    Has.Count.EqualTo(OrganisationSetupMembership.DomainCandidates.Count),
                    $"Only [{string.Join(", ", check.Found)}] of the Domain candidates {string.Join(", ", OrganisationSetupMembership.DomainCandidates)} were found. Why it matters: a candidate the check cannot find is never examined. How to fix: list the candidate under its current name.");
                Assert.That(
                    check.Offenders,
                    Is.Empty,
                    "These Domain candidates reach code outside the kernel, the base library and the other Domain candidates:" + Environment.NewLine +
                    string.Join(Environment.NewLine, check.Offenders) + Environment.NewLine +
                    "Why it matters: a type goes to the module's Domain only when it is a pure calculation; one that reaches a service, the database or a logger belongs in the Application. " +
                    "How to fix: give the type the Application layer in OrganisationSetupMembership, or hand it what it reaches as a value.");
            }
        }

        private static void ThenTheMadeUpCandidateIsNamedWithTheDatabase(DomainCheck check)
            => Assert.That(
                check.Offenders,
                Is.EqualTo(new[] { $"{typeof(Lighthouse.Backend.Modules.OrganisationSetupFixtures.Domain.AMadeUpDomainCandidate).FullName} reaches {typeof(LighthouseAppContext).FullName}" }),
                "The Domain check should name the made-up candidate and the database context it reaches. Why it matters: a check that names nothing lets a service into the Domain. How to fix: report every reference of a candidate outside the kernel, the base library and the other candidates.");

        private static void ThenEveryStayingFileIsSound(List<string> unsound)
            => Assert.That(
                unsound,
                Is.Empty,
                "These set-up files that stay are missing an owner or a reason, or do not exist:" + Environment.NewLine +
                string.Join(Environment.NewLine, unsound) + Environment.NewLine +
                "Why it matters: a file that stays outside the module is a decision; without its owner and reason, or with a path to nothing, nobody can tell when it moves. " +
                "How to fix: give the entry its owner, its reason and the file's current path.");

        private static void ThenBothMadeUpStayingFilesAreNamed(List<string> unsound)
            => Assert.That(
                unsound,
                Has.Count.EqualTo(2)
                    .And.Some.StartsWith("Services/Implementation/AMadeUpFileThatIsNotThere.cs")
                    .And.Some.Contains("owner \" \""),
                "The staying check should name the made-up file that does not exist and the entry without an owner, and pass the sound one. Why it matters: a check that names nothing keeps a stale list. How to fix: name every entry whose file is missing or whose owner or reason is blank.");

        private static void ThenTheMeasuredEdgesAreExactlyTheListedOnes(List<string> measured, IReadOnlyList<OrganisationSetupImplicitEdge> listed)
        {
            var written = listed.Select(edge => $"{edge.Origin} -> {edge.Target}").ToList();
            var unlisted = measured.Except(written, StringComparer.Ordinal).ToList();
            var gone = written.Except(measured, StringComparer.Ordinal).ToList();

            Assert.That(
                unlisted.Select(edge => $"measured, not listed: {edge}").Concat(gone.Select(edge => $"listed, not measured: {edge}")).ToList(),
                Is.Empty,
                "The references the move takes out of namespace scope differ from OrganisationSetupMembership.ImplicitEdges." + Environment.NewLine +
                $"Measured but not listed: [{string.Join(" | ", unlisted)}]" + Environment.NewLine +
                $"Listed but not measured: [{string.Join(" | ", gone)}]" + Environment.NewLine +
                "Why it matters: each of these references compiles today only because the referring type sits in the moving type's namespace; the move needs a using line there, and the list is how the move finds them. " +
                "How to fix: list every measured reference, and take off a listed one the code no longer has.");
        }

        private static void ThenNoPatternBreaksItsRole(List<PatternBreak> breaks)
            => Assert.That(
                breaks.Select(found => $"{found.Pattern.Field} ({found.Declared}){Environment.NewLine}{found.Describe()}").Distinct().ToList(),
                Is.Empty,
                "These rule patterns would not keep their meaning when the Organisation set-up types move into Lighthouse.Backend.Modules.OrganisationSetup:" + Environment.NewLine +
                string.Join(Environment.NewLine, breaks.Select(found => $"{found.Pattern.Field} = {found.Pattern.Value} ({found.Declared}){Environment.NewLine}{found.Describe()}").Distinct()) + Environment.NewLine +
                "Why it matters: a part of the code that does not match the new namespace stops seeing the type the moment it moves and then passes because it finds nothing; an old location that matches the new place starts judging code that no longer lives there; an undeclared pattern on the loose Services namespaces could stand for either. " +
                @"How to fix: declare the pattern in OrganisationSetupPatternRoles with its subject and role; widen a Context pattern with |^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$) (a full-name pattern with the type's new full name); leave a Place pattern in the old place.");

        private static void ThenEveryPatternNamedByTheModuleOnlySeesSetUpOnlyInItsModule(List<string> unsound)
            => Assert.That(
                unsound,
                Is.Empty,
                "These patterns are on OrganisationSetupPatternRoles.NamedByTheModuleOnly but do not name Organisation set-up by its module only:" + Environment.NewLine +
                string.Join(Environment.NewLine, unsound) + Environment.NewLine +
                "Why it matters: the list lets a finished narrowing pass the role check; an entry that is not a rule pattern, still sees the old place, or sees no moved type would hide a break the list was never meant for. " +
                "How to fix: take the entry off the list, or narrow the pattern to the module and to the full names of the loose types it still needs.");

        private static void ThenEveryWholeParentPatternTakesInAllOfModules(List<string> narrowerThanTheParent)
            => Assert.That(
                narrowerThanTheParent,
                Is.Empty,
                $"These patterns are on OrganisationSetupPatternRoles.WholeParentPatterns but no longer match {TheParentNamespace} itself and the new namespace of every moving type:" + Environment.NewLine +
                string.Join(Environment.NewLine, narrowerThanTheParent) + Environment.NewLine +
                "Why it matters: the list lets a pattern over every module see set-up once it becomes a module; a narrower pattern on it would escape the role check. How to fix: take the pattern off the list, or give it back its sight of all of Modules.");

        private static void ThenTheMadeUpBreaksNamedAre(List<PatternBreak> breaks, (string Field, RoleBreak Break)[] expected)
        {
            var named = breaks.Select(found => (found.Pattern.Field, found.Break)).Distinct().OrderBy(found => found.Field, StringComparer.Ordinal).ThenBy(found => found.Break).ToList();
            var wanted = expected.OrderBy(found => found.Field, StringComparer.Ordinal).ThenBy(found => found.Break).ToList();

            Assert.That(
                named,
                Is.EqualTo(wanted),
                $"The role check named [{string.Join(", ", named)}]. Expected the context that sees only the old place, the old location widened into the new place, the undeclared pattern and the composite of the blind context, and not the widened context or the old location left in place. " +
                "Why it matters: a check that lets an undeclared pattern pass, asks a full-name rule about namespaces, accepts an old location that sees the new place or ignores a composite's component lets a rule go blind or wrong at the move. " +
                "How to fix: evaluate every declared pattern on its subject and role, and every undeclared one on both subjects.");
        }

        private static void ThenTheOldPathStringsAreExactlyTheList(OldPathComparison comparison)
            => Assert.That(
                comparison.Unlisted.Select(found => $"not on the list: {found}").Concat(comparison.Missing.Select(entry => $"in neither form: {entry}")).ToList(),
                Is.Empty,
                "The strings of the test project that name a moving Organisation set-up type by its old Services path differ from OrganisationSetupStringsTheMoveRenames." + Environment.NewLine +
                $"Not on the list: [{string.Join(" | ", comparison.Unlisted)}]" + Environment.NewLine +
                $"On the list, found in neither form: [{string.Join(" | ", comparison.Missing)}]" + Environment.NewLine +
                "Why it matters: a string the list does not know stays behind when the type moves, and a rule or a list that names the old path then checks nothing. " +
                "How to fix: rename the string with the move, or build it at run time; a string the move has not reached yet must be on the list with its new text.");

        private static void ThenTheMadeUpFileIsComparedBothWays(OldPathComparison comparison)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    comparison.Unlisted,
                    Has.Count.EqualTo(1).And.Some.Contains(MadeUpRenameList.TheUnlistedOldName),
                    $"The sweep named [{string.Join(" | ", comparison.Unlisted)}]; it should name only the unlisted old full name. Why it matters: a sweep that names nothing lets an old path stay behind. How to fix: name every literal that names a moving type by its old path and is not on the list for its file.");
                Assert.That(
                    comparison.Missing,
                    Has.Count.EqualTo(1).And.Some.Contains(MadeUpRenameList.TheListedButAbsentOldName),
                    $"The sweep reported [{string.Join(" | ", comparison.Missing)}] as found in neither form; it should report only the listed entry found nowhere, and accept the entry in its new form and the one that may leave. Why it matters: a list that keeps entries for strings that are gone hides a lost rule. How to fix: report a listed entry found in neither its old nor its new form unless it may leave.");
            }
        }

        private static bool MayBeReachedFromTheDomain(TypeReference type, IReadOnlyList<string> candidates)
            => type is GenericParameter
                || (IlDependencies.NamespaceOf(type).Length == 0 && OutermostOf(type).Name.StartsWith('<'))
                || IlDependencies.ResidesInNamespaceMatching(type, KernelRules.TheModels)
                || IlDependencies.ResidesInNamespaceMatching(type, ModuleRules.TheBaseLibrary)
                || candidates.Contains(OutermostFullNameOf(type), StringComparer.Ordinal);

        private static string OutermostFullNameOf(TypeReference type)
        {
            var outermost = OutermostOf(type.GetElementType());
            return $"{outermost.Namespace}.{outermost.Name}";
        }

        private static TypeReference OutermostOf(TypeReference type) => type.DeclaringType is null ? type : OutermostOf(type.DeclaringType);

        private static string TopLevelOf(string typeName) => typeName.Split('+')[0].Split('<')[0];

        private static bool IsInOrBelow(string name, string parent) => name == parent || name.StartsWith(parent + ".", StringComparison.Ordinal);

        private static bool TakesInAllOfModules(string pattern)
            => NamespaceMoveLaw.IsMatch(TheParentNamespace, pattern)
                && OrganisationSetupMembership.MovingTypes.All(type => NamespaceMoveLaw.IsMatch(type.NewNamespace, pattern));

        private static bool InFixtures(Type type) => type.Namespace?.StartsWith(FixtureNamespace, StringComparison.Ordinal) == true;

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root, the folder holding Lighthouse.Backend/Lighthouse.sln, was not found above the test directory.");
        }

        private sealed record DomainCheck(List<string> Found, List<string> Offenders);

        private sealed record RoleCheck(List<PatternBreak> Breaks, List<string> NarrowerThanTheParent, List<string> UnsoundModuleOnlyPatterns);

        private sealed record OldPathComparison(List<string> Unlisted, List<string> Missing);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OrganisationSetupMembershipFixtures
{
    /// <summary>Made-up rule keys the role check is run over, to show it names a key that breaks its role and passes the others.</summary>
    internal static class MadeUpSetUpRuleKeys
    {
        public const string AMadeUpTypeName = "IAMadeUpSetUpServiceThatIsNowhere";

        public const string AContextThatSeesOnlyTheOldPlace = @"^Lighthouse\.Backend\.Services\.Interfaces\.IFeatureOrdering$";

        public const string AContextWidenedToTheNewPlace = @"^Lighthouse\.Backend\.(Services\.Interfaces|Modules\.OrganisationSetup\.Application)\.IFeatureOrdering$";

        public const string APlaceWidenedIntoTheNewPlace = AContextWidenedToTheNewPlace;

        public const string APlaceLeftInTheOldPlace = AContextThatSeesOnlyTheOldPlace;

        internal static class Nested
        {
            public static readonly string UndeclaredButSeesTheOldPlace = @"^Lighthouse\.Backend\.Services\.Implementation\.WeeklyRecurrence$";

            public static readonly string ACompositeOfTheBlindContext = AContextThatSeesOnlyTheOldPlace + "|" + @"^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)";
        }
    }

    /// <summary>The subject and role of each made-up key, as the real rules have theirs in OrganisationSetupPatternRoles.</summary>
    internal static class MadeUpSetUpRuleRoles
    {
        public static IReadOnlyDictionary<string, PatternDeclaration> Roles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            ["MadeUpSetUpRuleKeys.AContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Context(PatternSubject.FullName, "a part of the code that was never widened"),
            ["MadeUpSetUpRuleKeys.AContextWidenedToTheNewPlace"] = PatternDeclaration.Context(PatternSubject.FullName, "a part of the code widened with the type's new full name"),
            ["MadeUpSetUpRuleKeys.APlaceWidenedIntoTheNewPlace"] = PatternDeclaration.Place(PatternSubject.FullName, "an old location that was given the new place"),
            ["MadeUpSetUpRuleKeys.APlaceLeftInTheOldPlace"] = PatternDeclaration.Place(PatternSubject.FullName, "an old location left in the old place"),
            ["MadeUpSetUpRuleKeys.Nested.ACompositeOfTheBlindContext"] = PatternDeclaration.Composite("MadeUpSetUpRuleKeys.AContextThatSeesOnlyTheOldPlace", "built from the blind part of the code"),
        };
    }

    /// <summary>A made-up rule file and its rename list, to show the sweep compares the two both ways.</summary>
    internal static class MadeUpRenameList
    {
        public const string TheFile = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AMadeUpRuleFile.cs";

        public static string TheUnlistedOldName => string.Join('.', "Lighthouse.Backend.Services.Interfaces", "ITerminologyService");

        public static string TheListedButAbsentOldName => string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IFeatureRankSeeder");

        public static string TheSource => string.Join(
            Environment.NewLine,
            $"var listedAndStillOld = \"{string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IAppSettingService")}\";",
            $"var listedAndAlreadyRenamed = \"{string.Join('.', "Lighthouse.Backend.Modules.OrganisationSetup.Application", "IFeatureOrdering")}\";",
            $"var unlisted = \"{TheUnlistedOldName}\";",
            "var aTestNamespace = \"Lighthouse.Backend.Tests.Services.Implementation.AppSettingServiceTest\";");

        public static IReadOnlyList<OrganisationSetupRename> Entries { get; } =
        [
            new(TheFile, string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IAppSettingService"), string.Join('.', "Lighthouse.Backend.Modules.OrganisationSetup.Application", "IAppSettingService")),
            new(TheFile, string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IFeatureOrdering"), string.Join('.', "Lighthouse.Backend.Modules.OrganisationSetup.Application", "IFeatureOrdering")),
            new(TheFile, TheListedButAbsentOldName, string.Join('.', "Lighthouse.Backend.Modules.OrganisationSetup.Application", "IFeatureRankSeeder")),
            new(TheFile, string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IFeatureRankingService"), string.Join('.', "Lighthouse.Backend.Modules.OrganisationSetup.Application", "IFeatureRankingService"), MayLeave: true),
        ];

        public static NamespaceMoveLaw TheLaw { get; } = new(
            new NamespaceMove(
                [.. OrganisationSetupMembership.MovingTypes.Select(type => new MovingType(type.Name, "OrganisationSetup", type.OldNamespace, type.NewNamespace))],
                [.. Entries.Select(entry => entry.AsRenamedString())],
                @"^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)",
                new System.Text.RegularExpressions.Regex("(?!)", System.Text.RegularExpressions.RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                ["Connections", "OptionalFeatures"]),
            new PatternRoleMap(new Dictionary<string, PatternDeclaration>(), new Dictionary<(string Field, string Type), string>()));
    }
}

namespace Lighthouse.Backend.Modules.OrganisationSetupFixtures.Domain
{
    /// <summary>A made-up Domain candidate that reaches the database, which the Domain check must name.</summary>
    internal sealed class AMadeUpDomainCandidate
    {
        public Lighthouse.Backend.Data.LighthouseAppContext? Database { get; init; }
    }
}
