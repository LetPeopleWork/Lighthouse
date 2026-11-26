using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures;
using Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures.Contexts.Beta.Application;
using Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures.Contexts.Beta.Domain;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every packaged context is about to publish what other contexts may name in a Contracts namespace of its own. Before
    /// anything moves, the rules know what each context publishes: the published set is measured from the compiled code (the
    /// shapes another context reaches, the events a context publishes, and what their public signatures name); every rule
    /// pattern that sees a published type carries a declared role (a front door that must see it before and after the move, an
    /// old location that must not see it in Contracts, or a part of the code that takes in both); and every string of the test
    /// project that names a published type by its old name, written out or in a grouped alternation, is on a fixed list of
    /// strings the move renames.
    /// </summary>
    [TestFixture]
    public class ContextContractsMembershipArchUnitTest
    {
        private const string TheEventBase = "IDomainEvent";

        private static readonly Type[] TheCheckItself =
        [
            typeof(ContextContractsMembershipArchUnitTest),
            typeof(ContextContractsMembership),
            typeof(ContextContractsPatternRoles),
            typeof(ContextContractsStringsTheMoveRenames),
            typeof(NamespaceMoveLaw),
        ];

        // The map, the roles, the rename list and this check name the published types by their old names on purpose, so they
        // hold before, during and after the moves; they are the measurement, not rules a move could leave behind.
        private static readonly string[] TheMeasurementFiles =
        [
            $"{nameof(ContextContractsMembership)}.cs",
            $"{nameof(ContextContractsPatternRoles)}.cs",
            $"{nameof(ContextContractsStringsTheMoveRenames)}.cs",
            $"{nameof(ContextContractsMembershipArchUnitTest)}.cs",
            $"{nameof(NamespaceMoveLaw)}.cs",

            // The temporary start-up pin names every registered type by its full name today, so it can tell a moved line from a changed one.
            $"{nameof(StartUpRegistrationsPreservationTest)}.cs",
        ];

        private static readonly Regex StringLiteral = new(
            @"'(?:[^'\\\r\n]|\\.)'|(?:\$@|@\$|@)""(?<verbatim>(?:[^""]|"""")*)""|\$?""(?<regular>(?:[^""\\\r\n]|\\.)*)""",
            RegexOptions.None,
            NamespaceMoveLaw.PatternTimeout);

        // A dotted name in which at least one part may be a group of alternatives, such as Modules.X.Application.(A|B) or
        // Backend.(Modules.X.Application|Services.Interfaces).(A|B), read after the backslashes of a regex are taken out.
        private static readonly Regex ADottedNameWithGroups = new(
            @"(?<![A-Za-z0-9_.])(?:[A-Za-z_][A-Za-z0-9_`]*|\((?:\?:)?[A-Za-z0-9_.|`]+\))(?:\.(?:[A-Za-z_][A-Za-z0-9_`]*|\((?:\?:)?[A-Za-z0-9_.|`]+\)))+",
            RegexOptions.None,
            NamespaceMoveLaw.PatternTimeout);

        private static readonly NamespaceMoveLaw TheLaw = new(
            TheContractsMove(ContextContractsMembership.Entries, ContextContractsStringsTheMoveRenames.Entries),
            new PatternRoleMap(ContextContractsPatternRoles.PatternRoles, ContextContractsPatternRoles.FolderOnlyMatches));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ContextContractsMembershipArchUnitTest).Assembly));

        private static string FixtureNamespace => typeof(MadeUpContractRules).Namespace!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(ThePublishedTypes))]
        public void ThePublishedType_IsFoundAsATopLevelTypeInExactlyItsOldOrItsContractsNamespace(PublishedType entry)
        {
            var module = ProductionModule.Value;

            var places = WhenTheEntryIsLookedUpInTheCompiledCode(module, entry);

            ThenItIsFoundInExactlyOnePlace(entry, places);
        }

        [Test]
        public void ThePublishedSetOfEveryContext_EqualsTheClosureMeasuredFromTheCompiledCode_BothWays()
        {
            var listed = GivenTheMembershipMap();

            var measured = WhenThePublishedSetIsMeasured(ProductionModule.Value, ContextContractsMembership.TheModulesRoot, ContextContractsMembership.TheContexts, OwnerOfAnEventByTheMap(listed.Entries), withEvents: true);

            ThenTheMapEqualsTheMeasurement(listed, measured);
        }

        [Test]
        public void TheClosureCheck_NamesASeedThatHoldsBehaviour_AndAClosureMemberMissingFromTheMap()
        {
            var listed = GivenAMadeUpMapListingABehaviourClassAndMissingAClosureMember();

            var measured = WhenThePublishedSetIsMeasured(TestModule.Value, MadeUpContexts.TheRoot, MadeUpContexts.TheContexts, OwnerOfAnEventByTheMap(listed.Entries), withEvents: false);

            ThenOnlyTheMadeUpDifferencesAreNamed(listed, measured);
        }

        [TestCaseSource(nameof(TheDeclaredPatterns))]
        public void TheRulePattern_KeepsItsDeclaredRole_WhenThePublishedTypesMoveIntoContracts(DeclaredPattern declared)
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var breaks = WhenTheRoleOfThePatternIsChecked(TheLaw, patterns, declared.Field);

            ThenThePatternKeepsItsRole(declared, breaks);
        }

        [Test]
        public void EveryRulePatternThatSeesAPublishedType_IsDeclared_AndEveryDeclaredPatternIsARulePattern()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var undeclared = WhenTheUndeclaredAndTheStaleDeclarationsAreFound(TheLaw, patterns, ContextContractsPatternRoles.PatternRoles);

            ThenEveryPatternIsDeclaredOnce(undeclared);
        }

        [Test]
        public void TheRoleCheck_NamesAnUndeclaredPattern_AFrontDoorNotWidened_AndAnOldLocationSeeingTheContractsName()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var breaks = WhenTheMadeUpRolesAreChecked(patterns);

            ThenTheMadeUpBreaksAreNamed(breaks);
        }

        [Test]
        public void EveryTestStringThatNamesAPublishedTypeByItsOldName_WrittenOutOrGrouped_IsOnTheRenameList_InExactlyOneOfItsForms()
        {
            var sources = GivenTheSourcesOfTheTestProject();

            var comparison = WhenTheOldNameStringsAreComparedWithTheList(TheLaw, ContextContractsMembership.Entries, ContextContractsStringsTheMoveRenames.Entries, sources, GivenThePatternValuesTheRoleCheckGoverns());

            ThenTheOldNameStringsAreExactlyTheList(comparison);
        }

        [Test]
        public void TheRenameListCheck_NamesAWrittenOutAndAGroupedOldName_AListedStringFoundInNeitherForm_AndOneFoundInBoth()
        {
            var sources = GivenAMadeUpRuleFile();

            var comparison = WhenTheOldNameStringsAreComparedWithTheList(MadeUpRenameList.TheLaw, MadeUpRenameList.TheMovingTypes, MadeUpRenameList.Entries, sources, []);

            ThenTheMadeUpFileIsComparedBothWays(comparison);
        }

        private static IEnumerable<TestCaseData> ThePublishedTypes()
            => ContextContractsMembership.Entries.Select(entry => new TestCaseData(new PublishedType(entry.Type.Name, entry.Type.Area, entry.Type.OldNamespace, entry.Type.NewNamespace, entry.Admission.ToString())));

        private static IEnumerable<TestCaseData> TheDeclaredPatterns()
            => ContextContractsPatternRoles.PatternRoles
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new TestCaseData(new DeclaredPattern(entry.Key, RoleOf(entry.Key, ContextContractsPatternRoles.PatternRoles)?.ToString() ?? "no role")));

        private static NamespaceMove TheContractsMove(IReadOnlyList<(MovingType Type, ContractAdmission Admission)> entries, IReadOnlyList<RenamedString> renames)
            => new(
                [.. entries.Select(entry => entry.Type)],
                renames,
                $@"^{Regex.Escape(ContextContractsMembership.TheModulesRoot)}\.[A-Za-z0-9_]+\.Contracts(\.|$)",
                new Regex(@"(?<![A-Za-z0-9_])Contracts(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []);

        private static PublishedMap GivenTheMembershipMap()
        {
            var map = new PublishedMap(ContextContractsMembership.Entries, ContextContractsMembership.ReachedButNotPublished);
            Assert.That(map.Entries, Is.Not.Empty, "The membership map lists no published type, so the comparison would pass without measuring anything. How to fix: list what each context publishes, as measured from the compiled code.");
            return map;
        }

        private static PublishedMap GivenAMadeUpMapListingABehaviourClassAndMissingAClosureMember()
            => new(
                [
                    (new MovingType(nameof(IBetaQuery), MadeUpContexts.Beta, MadeUpContexts.LayerOf(MadeUpContexts.Beta, "Application"), MadeUpContexts.ContractsOf(MadeUpContexts.Beta)), ContractAdmission.Seed),
                    (new MovingType(nameof(BetaCalculator), MadeUpContexts.Beta, MadeUpContexts.LayerOf(MadeUpContexts.Beta, "Application"), MadeUpContexts.ContractsOf(MadeUpContexts.Beta)), ContractAdmission.Seed),
                ],
                []);

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var patterns = TheLaw.RulePatternsOf(typeof(ContextContractsMembershipArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type) && !TheCheckItself.Contains(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every rule class.");
            return patterns;
        }

        private static List<RulePattern> GivenTheMadeUpRulePatterns()
            => MadeUpContractRules.TheLaw.RulePatternsOf([typeof(MadeUpContractRules)]);

        private static HashSet<string> GivenThePatternValuesTheRoleCheckGoverns()
            => TheLaw.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly().Where(pattern => ContextContractsPatternRoles.PatternRoles.ContainsKey(pattern.Field)));

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

        private static List<string> WhenTheEntryIsLookedUpInTheCompiledCode(ModuleDefinition module, PublishedType entry)
            => [.. new[] { entry.OldFullName, entry.NewFullName }.Where(fullName => module.GetType(fullName) is { DeclaringType: null })];

        // The published set of a context X is the least set holding every type of X that a type of another context reaches in
        // the IL and that is a shape (an interface, an enum, a record or struct holding no service, a class of constants), every
        // event type, and every type of the same context that the public signature of a published type names. Everything
        // reached that holds behaviour stays apart.
        private static PublishedMeasurement WhenThePublishedSetIsMeasured(ModuleDefinition module, string root, IReadOnlyList<string> contexts, Func<string, string?> ownerOfAnEvent, bool withEvents)
        {
            var contextRoot = new Regex($@"^{Regex.Escape(root)}\.(?<context>[A-Za-z0-9_]+)(\.|$)", RegexOptions.None, NamespaceMoveLaw.PatternTimeout);
            string? ContextOf(string ns) => contextRoot.Match(ns) is { Success: true } found && contexts.Contains(found.Groups["context"].Value) ? found.Groups["context"].Value : null;

            var reached = contexts
                .SelectMany(context => IlDependencies.DependentsIn(
                    module,
                    $@"^{Regex.Escape(root)}\.{Regex.Escape(context)}(\.|$)",
                    target => ContextOf(IlDependencies.NamespaceOf(target)) is { } other && other != context))
                .SelectMany(dependent => dependent.Targets)
                .Select(TopLevelOf)
                .Distinct(StringComparer.Ordinal)
                .Select(name => module.GetType(name))
                .OfType<TypeDefinition>()
                .ToList();

            var published = new Dictionary<string, (string Context, ContractAdmission Admission)>(StringComparer.Ordinal);
            var queue = new Queue<TypeDefinition>();
            foreach (var seed in reached.Where(type => IsAShape(module, type)).Concat(module.Types.Where(type => withEvents && IsAnEvent(type))))
            {
                var context = ContextOf(seed.Namespace) ?? ownerOfAnEvent(seed.Name) ?? "no owner";
                if (published.TryAdd(seed.FullName, (context, ContractAdmission.Seed)))
                {
                    queue.Enqueue(seed);
                }
            }

            while (queue.TryDequeue(out var type))
            {
                var context = published[type.FullName].Context;
                foreach (var named in PublicSignatureOf(type)
                    .Select(reference => module.GetType(TopLevelOf(reference.FullName.Replace('/', '+'))))
                    .OfType<TypeDefinition>()
                    .Where(named => ContextOf(named.Namespace) == context && !published.ContainsKey(named.FullName)))
                {
                    published[named.FullName] = (context, ContractAdmission.Closure);
                    queue.Enqueue(named);
                }
            }

            var behaviour = reached.Where(type => !published.ContainsKey(type.FullName)).Select(type => type.FullName).ToList();

            return new PublishedMeasurement(
                [.. published.Select(entry => Describe(entry.Value.Context, NameOf(entry.Key), entry.Value.Admission))],
                behaviour);
        }

        private static List<string> WhenTheRoleOfThePatternIsChecked(NamespaceMoveLaw law, List<RulePattern> patterns, string field)
        {
            var pattern = patterns.FirstOrDefault(candidate => candidate.Field == field);
            return pattern is null
                ? [$"{field} is declared, but is no rule pattern of the test assembly"]
                : [.. law.RoleBreaksOf([pattern]).Select(found => found.Describe().Trim()).Distinct(StringComparer.Ordinal)];
        }

        private static List<string> WhenTheUndeclaredAndTheStaleDeclarationsAreFound(NamespaceMoveLaw law, List<RulePattern> patterns, IReadOnlyDictionary<string, PatternDeclaration> roles)
        {
            var undeclared = law.RoleBreaksOf(patterns)
                .Where(found => found.Break == RoleBreak.NotDeclared)
                .Select(found => $"{found.Pattern.Field} = {found.Pattern.Value}{Environment.NewLine}{found.Describe()}")
                .Distinct(StringComparer.Ordinal);
            var fields = patterns.Select(pattern => pattern.Field).ToHashSet(StringComparer.Ordinal);
            var stale = roles.Keys.Where(field => !fields.Contains(field)).Select(field => $"{field}: declared, but no rule pattern of the test assembly");

            return [.. undeclared, .. stale];
        }

        private static List<string> WhenTheMadeUpRolesAreChecked(List<RulePattern> patterns)
            => [.. MadeUpContractRules.TheLaw.RoleBreaksOf(patterns).Select(found => $"{found.Pattern.Field}: {found.Break}").Distinct(StringComparer.Ordinal)];

        private static OldNameComparison WhenTheOldNameStringsAreComparedWithTheList(NamespaceMoveLaw law, IReadOnlyList<(MovingType Type, ContractAdmission Admission)> moving, IReadOnlyList<RenamedString> list, List<(string RelativePath, string Source)> sources, HashSet<string> governed)
        {
            var unlisted = sources
                .SelectMany(file => law.OldPathStringsIn(file.RelativePath, file.Source, governed).Concat(GroupedOldNamesIn(file.RelativePath, file.Source, moving, list, governed)))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var byFile = sources.ToDictionary(file => file.RelativePath, file => file.Source, StringComparer.Ordinal);
            var formsFound = list
                .Select(entry => (Entry: entry, Found: byFile.TryGetValue(entry.File, out var source)
                    ? new[] { entry.OldText, entry.NewText }.Count(text => source.Contains(text, StringComparison.Ordinal))
                    : 0))
                .ToList();

            return new OldNameComparison(
                unlisted,
                [.. formsFound.Where(found => found.Found == 0).Select(found => $"{found.Entry.File}: \"{found.Entry.OldText}\" / \"{found.Entry.NewText}\"")],
                [.. formsFound.Where(found => found.Found == 2).Select(found => $"{found.Entry.File}: \"{found.Entry.OldText}\" / \"{found.Entry.NewText}\"")]);
        }

        // A regex may list several names after one place, as in Modules.X.Application.(A|B), or several places before one
        // name; the written-out reference misses that form, so each alternative is read as if written out in full.
        private static IEnumerable<string> GroupedOldNamesIn(string relativePath, string source, IReadOnlyList<(MovingType Type, ContractAdmission Admission)> moving, IReadOnlyList<RenamedString> list, HashSet<string> governed)
        {
            var renamedHere = list.Where(entry => entry.File == relativePath).Select(entry => entry.OldText).OrderByDescending(text => text.Length).ToList();
            var oldNames = moving.Select(entry => (entry.Type.OldFullName, Reference: new Regex($@"(?<![A-Za-z0-9_])(?:Lighthouse\.Backend\.)?{Regex.Escape(entry.Type.OldNamespace["Lighthouse.Backend.".Length..])}\.{Regex.Escape(entry.Type.Name.Split('`')[0])}(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout))).ToList();
            var withoutComments = ProductionSource.WithoutComments(source);

            foreach (Match literal in StringLiteral.Matches(withoutComments))
            {
                var value = literal.Groups["verbatim"].Success ? literal.Groups["verbatim"].Value.Replace("\"\"", "\"", StringComparison.Ordinal) : literal.Groups["regular"].Value;
                if ((!literal.Groups["verbatim"].Success && !literal.Groups["regular"].Success) || governed.Contains(value))
                {
                    continue;
                }

                var remainder = renamedHere.Aggregate(value, (text, oldText) => text.Replace(oldText, " ", StringComparison.Ordinal)).Replace("\\", string.Empty, StringComparison.Ordinal).Replace('/', '.');
                var spelled = ADottedNameWithGroups.Matches(remainder).Where(name => name.Value.Contains('(')).SelectMany(name => Expanded(name.Value)).ToList();
                var line = withoutComments.AsSpan(0, literal.Index).Count('\n') + 1;
                foreach (var oldName in oldNames.Where(oldName => spelled.Exists(name => oldName.Reference.IsMatch(name))))
                {
                    yield return $"{relativePath}:{line}: {oldName.OldFullName} in the grouped alternation {literal.Value}";
                }
            }
        }

        private static List<string> Expanded(string dottedName)
        {
            var parts = new List<List<string>>();
            var depth = 0;
            var start = 0;
            for (var index = 0; index <= dottedName.Length; index++)
            {
                if (index == dottedName.Length || (dottedName[index] == '.' && depth == 0))
                {
                    var part = dottedName[start..index];
                    parts.Add(part.StartsWith('(') ? [.. part.Trim('(', ')').Replace("?:", string.Empty, StringComparison.Ordinal).Split('|')] : [part]);
                    start = index + 1;
                }
                else
                {
                    depth += dottedName[index] switch { '(' => 1, ')' => -1, _ => 0 };
                }
            }

            return parts.Aggregate(new List<string> { string.Empty }, (spelled, alternatives) => [.. spelled.SelectMany(prefix => alternatives.Select(alternative => prefix.Length == 0 ? alternative : $"{prefix}.{alternative}")).Take(4096)]);
        }

        private static void ThenItIsFoundInExactlyOnePlace(PublishedType entry, List<string> places)
            => Assert.That(
                places,
                Has.Count.EqualTo(1),
                $"{entry.Name} of {entry.Context} was found as a top-level type in [{string.Join(", ", places)}]; it must be in exactly one of {entry.OldFullName} and {entry.NewFullName}. " +
                "Why it matters: the map is the measured list of what a context publishes; an entry found nowhere names a type that no longer exists, and one found in both places is half moved. " +
                "How to fix: correct the entry's old namespace, or finish the move so the type lives in its context's Contracts only.");

        private static void ThenTheMapEqualsTheMeasurement(PublishedMap listed, PublishedMeasurement measured)
        {
            Assert.That(measured.Published, Is.Not.Empty, "Nothing was measured as published, so the comparison would pass without reading the compiled code. How to fix: read the production assembly and the ten contexts.");
            var problems = DifferencesBetween(listed, measured);
            Assert.That(
                problems,
                Is.Empty,
                "The membership map differs from the published set measured from the compiled code:" + Environment.NewLine + string.Join(Environment.NewLine, problems) + Environment.NewLine +
                "Why it matters: a type another context reaches that is missing from the map stays behind when its context publishes its Contracts, and a behaviour class on the map would publish an algorithm every caller then depends on. " +
                "How to fix: list each measured type with its context and admission, or move a type that holds behaviour to the list of types reached but not published.");
        }

        private static void ThenOnlyTheMadeUpDifferencesAreNamed(PublishedMap listed, PublishedMeasurement measured)
        {
            var problems = DifferencesBetween(listed, measured);
            string[] expected =
            [
                $"listed as published, but holds behaviour: {MadeUpContexts.Beta}:{nameof(BetaCalculator)} (Seed)",
                $"measured as published, missing from the map: {MadeUpContexts.Beta}:{nameof(BetaAnswer)} (Closure)",
                $"reached by another context and holds behaviour, missing from the list of types not published: {typeof(BetaCalculator).FullName}",
            ];
            Assert.That(
                problems,
                Is.EquivalentTo(expected),
                $"The closure check named [{string.Join(" | ", problems)}]. Why it matters: a check that does not name a published behaviour class or a missing closure member lets either pass unseen. How to fix: compare the map with the measured seeds, their public-signature closure and the reached behaviour classes, both ways.");
        }

        private static void ThenThePatternKeepsItsRole(DeclaredPattern declared, List<string> breaks)
            => Assert.That(
                breaks,
                Is.Empty,
                $"{declared.Field}, declared {declared.Role}, would not keep its meaning when the published types move into their context's Contracts:" + Environment.NewLine + string.Join(Environment.NewLine, breaks) + Environment.NewLine +
                "Why it matters: a front door that does not match Contracts stops seeing a published type the moment it moves and then passes because it finds nothing; an old location that matches Contracts starts judging code that has left it. " +
                @"How to fix: widen a front door from Modules\.X\.Application to Modules\.X\.(Application|Contracts) for its own context only, or keep an old location off Contracts.");

        private static void ThenEveryPatternIsDeclaredOnce(List<string> problems)
            => Assert.That(
                problems,
                Is.Empty,
                "These rule patterns see a published type and carry no declared role, or are declared and are no rule pattern:" + Environment.NewLine + string.Join(Environment.NewLine, problems) + Environment.NewLine +
                "Why it matters: a pattern with no declared role has no known meaning to keep when the type moves, and a declaration that names nothing hides a renamed rule. " +
                "How to fix: declare the pattern in ContextContractsPatternRoles as a front door, an old location or a part of the code, or delete a declaration whose pattern is gone.");

        private static void ThenTheMadeUpBreaksAreNamed(List<string> breaks)
        {
            string[] expected =
            [
                $"{nameof(MadeUpContractRules)}.{nameof(MadeUpContractRules.TheWholeContext)}: {RoleBreak.NotDeclared}",
                $"{nameof(MadeUpContractRules)}.{nameof(MadeUpContractRules.AFrontDoorNotWidened)}: {RoleBreak.StopsSeeingTheType}",
                $"{nameof(MadeUpContractRules)}.{nameof(MadeUpContractRules.AnOldLocationSeeingContracts)}: {RoleBreak.SeesTheTypeInItsNewPlace}",
            ];
            Assert.That(
                breaks.Where(found => !found.EndsWith(RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation.ToString(), StringComparison.Ordinal)),
                Is.EquivalentTo(expected),
                $"The role check named [{string.Join(" | ", breaks)}]; it should name the undeclared pattern, the front door that was not widened and the old location that sees Contracts, and pass the widened front door and the old location that keeps off Contracts. " +
                "Why it matters: a role check that misses one of these lets a rule stop seeing a published type, or start judging code that left it, unseen. How to fix: evaluate every declared pattern on the old and the new name of every moving type.");
        }

        private static void ThenTheOldNameStringsAreExactlyTheList(OldNameComparison comparison)
            => Assert.That(
                comparison.Unlisted.Select(found => $"not on the list: {found}")
                    .Concat(comparison.Missing.Select(entry => $"in neither form: {entry}"))
                    .Concat(comparison.Both.Select(entry => $"in both forms: {entry}"))
                    .ToList(),
                Is.Empty,
                "The strings of the test project that name a published type by its old name differ from ContextContractsStringsTheMoveRenames." + Environment.NewLine +
                $"Not on the list: [{string.Join(" | ", comparison.Unlisted)}]" + Environment.NewLine +
                $"On the list, found in neither form: [{string.Join(" | ", comparison.Missing)}]" + Environment.NewLine +
                $"On the list, found in both forms: [{string.Join(" | ", comparison.Both)}]" + Environment.NewLine +
                "Why it matters: a string the list does not know stays behind when the type moves into Contracts, and a rule or a list that names the old name then checks nothing; a file holding both forms is half renamed. " +
                "How to fix: put the string on the list with its Contracts text, or build it at run time from its parts.");

        private static void ThenTheMadeUpFileIsComparedBothWays(OldNameComparison comparison)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    comparison.Unlisted,
                    Has.Count.EqualTo(2).And.Some.Contains(MadeUpRenameList.TheWrittenOutOldName).And.Some.Contains($"{MadeUpRenameList.TheGroupedOldName} in the grouped alternation"),
                    $"The sweep named [{string.Join(" | ", comparison.Unlisted)}]; it should name the written-out old name and the old name inside the grouped alternation, and nothing else. Why it matters: a sweep that misses either form lets an old name stay behind. How to fix: read both forms in every literal not on the list.");
                Assert.That(
                    comparison.Missing,
                    Has.Count.EqualTo(1).And.Some.Contains(MadeUpRenameList.TheListedButAbsentOldName),
                    $"The sweep reported [{string.Join(" | ", comparison.Missing)}] as found in neither form; it should report only the listed entry found nowhere. Why it matters: a list that keeps a string nobody holds hides a rename that went wrong. How to fix: report every entry found in neither form.");
                Assert.That(
                    comparison.Both,
                    Has.Count.EqualTo(1).And.Some.Contains(MadeUpRenameList.TheHalfRenamedOldName),
                    $"The sweep reported [{string.Join(" | ", comparison.Both)}] as found in both forms; it should report only the half-renamed entry. Why it matters: a file holding both forms keeps a rule that reads the old name. How to fix: report every entry whose file holds its old and its new text.");
            }
        }

        private static List<string> DifferencesBetween(PublishedMap listed, PublishedMeasurement measured)
        {
            var onTheMap = listed.Entries.Select(entry => Describe(entry.Type.Area, entry.Type.Name, entry.Admission)).ToHashSet(StringComparer.Ordinal);
            var measuredSet = measured.Published.ToHashSet(StringComparer.Ordinal);
            var behaviourNames = measured.Behaviour.Select(NameOf).ToHashSet(StringComparer.Ordinal);

            return
            [
                .. onTheMap.Except(measuredSet).Order(StringComparer.Ordinal).Select(entry => behaviourNames.Contains(entry.Split(':')[1].Split(' ')[0])
                    ? $"listed as published, but holds behaviour: {entry}"
                    : $"listed as published, but not measured so: {entry}"),
                .. measuredSet.Except(onTheMap).Order(StringComparer.Ordinal).Select(entry => $"measured as published, missing from the map: {entry}"),
                .. measured.Behaviour.Except(listed.NotPublished, StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(name => $"reached by another context and holds behaviour, missing from the list of types not published: {name}"),
                .. listed.NotPublished.Except(measured.Behaviour, StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(name => $"on the list of types not published, but not reached as a behaviour class: {name}"),
            ];
        }

        private static Func<string, string?> OwnerOfAnEventByTheMap(IReadOnlyList<(MovingType Type, ContractAdmission Admission)> entries)
            => name => entries.Where(entry => entry.Type.Name == name).Select(entry => entry.Type.Area).FirstOrDefault();

        private static bool IsAnEvent(TypeDefinition type)
            => !type.IsInterface && type.Interfaces.Any(implemented => implemented.InterfaceType.Name == TheEventBase && implemented.InterfaceType.Namespace.StartsWith("Lighthouse.Backend", StringComparison.Ordinal));

        private static bool IsAShape(ModuleDefinition module, TypeDefinition type)
            => type.IsInterface || type.IsEnum || IsARecordOrStructHoldingNoService(module, type) || IsAClassOfConstants(type);

        private static bool IsARecordOrStructHoldingNoService(ModuleDefinition module, TypeDefinition type)
            => (type.IsValueType || type.Methods.Any(method => method.Name == "<Clone>$"))
                && !type.Fields.Any(field => !field.IsStatic && IsAService(module, field.FieldType));

        private static bool IsAClassOfConstants(TypeDefinition type)
            => type is { IsAbstract: true, IsSealed: true, Fields.Count: > 0 }
                && type.Fields.All(field => field.IsStatic && (field.IsLiteral || field.IsInitOnly))
                && type.Methods.All(method => method.IsConstructor || method.IsGetter);

        private static bool IsAService(ModuleDefinition module, TypeReference type)
            => module.GetType(type.GetElementType().FullName) is { IsInterface: true };

        private static IEnumerable<TypeReference> PublicSignatureOf(TypeDefinition type)
            => new[] { type.BaseType }
                .Concat(type.Interfaces.Select(implemented => implemented.InterfaceType))
                .Concat(type.GenericParameters.SelectMany(parameter => parameter.Constraints.Select(constraint => constraint.ConstraintType)))
                .Concat(type.Methods.Where(method => method.IsPublic || method.IsFamily).SelectMany(method => method.Parameters.Select(parameter => parameter.ParameterType).Prepend(method.ReturnType)))
                .Concat(type.Properties.Where(property => property.GetMethod is { IsPublic: true } || property.SetMethod is { IsPublic: true }).Select(property => property.PropertyType))
                .Concat(type.Fields.Where(field => field.IsPublic).Select(field => field.FieldType))
                .Concat(type.NestedTypes.Where(nested => nested.IsNestedPublic).SelectMany(PublicSignatureOf))
                .OfType<TypeReference>()
                .SelectMany(Unwrapped);

        private static IEnumerable<TypeReference> Unwrapped(TypeReference type)
            => type switch
            {
                GenericInstanceType generic => [generic.ElementType, .. generic.GenericArguments.SelectMany(Unwrapped)],
                TypeSpecification wrapper => Unwrapped(wrapper.ElementType),
                _ => [type],
            };

        private static string TopLevelOf(string fullName) => fullName.Split('+')[0];

        private static string NameOf(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];

        private static string Describe(string context, string name, ContractAdmission admission) => $"{context}:{name} ({admission})";

        private static PatternRole? RoleOf(string field, IReadOnlyDictionary<string, PatternDeclaration> roles)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var declaration = roles.GetValueOrDefault(field);
            while (declaration?.CompositeOf is { } component && seen.Add(component))
            {
                declaration = roles.GetValueOrDefault(component);
            }

            return declaration?.Role;
        }

        // The made-up rule keys and types of this check live in a fixtures namespace under the architecture tests; they stand for no rule.
        private static bool InFixtures(Type type)
            => type.Namespace?.StartsWith(FixtureNamespace, StringComparison.Ordinal) == true
                || (type.Namespace?.StartsWith(typeof(ContextContractsMembershipArchUnitTest).Namespace + ".", StringComparison.Ordinal) == true && type.Namespace.EndsWith("Fixtures", StringComparison.Ordinal));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root, the folder holding Lighthouse.Backend/Lighthouse.sln, was not found above the test directory.");
        }

        public sealed record PublishedType(string Name, string Context, string OldNamespace, string NewNamespace, string Admission)
        {
            public string OldFullName => $"{OldNamespace}.{Name}";

            public string NewFullName => $"{NewNamespace}.{Name}";

            public override string ToString() => $"{Name} of {Context}: {OldNamespace} -> {NewNamespace}, {Admission}";
        }

        public sealed record DeclaredPattern(string Field, string Role)
        {
            public override string ToString() => $"{Field}: {Role}";
        }

        private sealed record PublishedMap(IReadOnlyList<(MovingType Type, ContractAdmission Admission)> Entries, IReadOnlyList<string> NotPublished);

        private sealed record PublishedMeasurement(IReadOnlyList<string> Published, IReadOnlyList<string> Behaviour);

        private sealed record OldNameComparison(List<string> Unlisted, List<string> Missing, List<string> Both);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures
{
    internal static class MadeUpContexts
    {
        public const string Alpha = "Alpha";

        public const string Beta = "Beta";

        public static string TheRoot => string.Join('.', typeof(MadeUpContexts).Namespace, "Contexts");

        public static IReadOnlyList<string> TheContexts { get; } = [Alpha, Beta];

        public static string ContractsOf(string context) => LayerOf(context, "Contracts");

        public static string LayerOf(string context, string layer) => string.Join('.', TheRoot, context, layer);
    }

    /// <summary>Made-up rule patterns over a made-up context Alpha whose one published type moves from its Application into its Contracts.</summary>
    internal static class MadeUpContractRules
    {
        public const string AFrontDoorNotWidened = @"^Lighthouse\.Backend\.Modules\.Alpha\.Application(\.|$)";

        public const string AWidenedFrontDoor = @"^Lighthouse\.Backend\.Modules\.Alpha\.(Application|Contracts)(\.|$)";

        public const string AnOldLocationSeeingContracts = @"^Lighthouse\.Backend\.Modules\.Alpha\.(Application|Contracts)$";

        public const string AnOldLocationKeepingOffContracts = @"^Lighthouse\.Backend\.Modules\.Alpha\.Application$";

        public const string TheWholeContext = @"^Lighthouse\.Backend\.Modules\.Alpha(\.|$)";

        private const string Rules = nameof(MadeUpContractRules);

        public static NamespaceMoveLaw TheLaw { get; } = new(
            new NamespaceMove(
                [new MovingType("IMadeUpQuery", MadeUpContexts.Alpha, "Lighthouse.Backend.Modules.Alpha.Application", "Lighthouse.Backend.Modules.Alpha.Contracts")],
                [],
                @"^Lighthouse\.Backend\.Modules\.[A-Za-z0-9_]+\.Contracts(\.|$)",
                new Regex(@"(?<![A-Za-z0-9_])Contracts(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []),
            new PatternRoleMap(
                new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
                {
                    [$"{Rules}.{nameof(AFrontDoorNotWidened)}"] = PatternDeclaration.FrontDoor(PatternSubject.Namespace, "the made-up front door before it is widened"),
                    [$"{Rules}.{nameof(AWidenedFrontDoor)}"] = PatternDeclaration.FrontDoor(PatternSubject.Namespace, "the made-up front door after it is widened"),
                    [$"{Rules}.{nameof(AnOldLocationSeeingContracts)}"] = PatternDeclaration.Place(PatternSubject.Namespace, "a made-up layer location that wrongly takes in Contracts"),
                    [$"{Rules}.{nameof(AnOldLocationKeepingOffContracts)}"] = PatternDeclaration.Place(PatternSubject.Namespace, "a made-up layer location that keeps off Contracts"),
                },
                new Dictionary<(string Field, string Type), string>()));
    }

    /// <summary>A made-up rule file, its moving types and its rename list, for the sweep's own check.</summary>
    internal static class MadeUpRenameList
    {
        public const string TheFile = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AMadeUpContractsRule.cs";

        public const string TheWrittenOutOldName = "Lighthouse.Backend.Modules.Alpha.Application.IMadeUpQuery";

        public const string TheGroupedOldName = "Lighthouse.Backend.Modules.Alpha.Application.IMadeUpAnswer";

        public const string TheListedButAbsentOldName = "Lighthouse.Backend.Modules.Alpha.Application.IMadeUpGone";

        public const string TheHalfRenamedOldName = "Lighthouse.Backend.Modules.Alpha.Application.IMadeUpHalf";

        public static IReadOnlyList<(MovingType Type, ContractAdmission Admission)> TheMovingTypes { get; } =
        [
            Moving("IMadeUpQuery"),
            Moving("IMadeUpAnswer"),
            Moving("IMadeUpGone"),
            Moving("IMadeUpHalf"),
            Moving("IMadeUpRenamed"),
        ];

        public static IReadOnlyList<RenamedString> Entries { get; } =
        [
            new(TheFile, TheListedButAbsentOldName, "Lighthouse.Backend.Modules.Alpha.Contracts.IMadeUpGone"),
            new(TheFile, TheHalfRenamedOldName, "Lighthouse.Backend.Modules.Alpha.Contracts.IMadeUpHalf"),
            new(TheFile, "Lighthouse.Backend.Modules.Alpha.Application.IMadeUpRenamed", "Lighthouse.Backend.Modules.Alpha.Contracts.IMadeUpRenamed"),
        ];

        public static string TheSource { get; } = string.Join(
            Environment.NewLine,
            "var flat = \"" + TheWrittenOutOldName + "\";",
            "var grouped = @\"" + @"^Lighthouse\.Backend\.Modules\.Alpha\.Application\.(IMadeUpOther|IMadeUpAnswer)$" + "\";",
            "var half = \"" + TheHalfRenamedOldName + "\";",
            "var halfDone = \"Lighthouse.Backend.Modules.Alpha.Contracts.IMadeUpHalf\";",
            "var renamed = \"Lighthouse.Backend.Modules.Alpha.Contracts.IMadeUpRenamed\";");

        public static NamespaceMoveLaw TheLaw { get; } = new(
            new NamespaceMove(
                [.. TheMovingTypes.Select(entry => entry.Type)],
                Entries,
                @"^Lighthouse\.Backend\.Modules\.[A-Za-z0-9_]+\.Contracts(\.|$)",
                new Regex("(?!)", RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []),
            new PatternRoleMap(new Dictionary<string, PatternDeclaration>(), new Dictionary<(string Field, string Type), string>()));

        private static (MovingType Type, ContractAdmission Admission) Moving(string name)
            => (new MovingType(name, MadeUpContexts.Alpha, "Lighthouse.Backend.Modules.Alpha.Application", "Lighthouse.Backend.Modules.Alpha.Contracts"), ContractAdmission.Seed);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures.Contexts.Beta.Domain
{
    public sealed record BetaAnswer(int Value);
}

namespace Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures.Contexts.Beta.Application
{
    using Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures.Contexts.Beta.Domain;

    public interface IBetaQuery
    {
        BetaAnswer Ask();
    }

    public sealed class BetaCalculator
    {
        public int Add(int left, int right) => left + right;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures.Contexts.Alpha.Application
{
    using Lighthouse.Backend.Tests.Architecture.ContextContractsMembershipFixtures.Contexts.Beta.Application;

    public sealed class AlphaReader(IBetaQuery query)
    {
        public IBetaQuery Query { get; } = query;

        public int Read() => new BetaCalculator().Add(1, 2);
    }
}
