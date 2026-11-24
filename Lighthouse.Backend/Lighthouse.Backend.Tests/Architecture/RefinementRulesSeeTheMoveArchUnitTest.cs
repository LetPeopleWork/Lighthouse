using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.RefinementRulesSeeTheMoveFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Refinement types are about to move out of Services into Lighthouse.Backend.Modules.Refinement. Every architecture
    /// rule that finds them by name must keep its meaning after the move, or the move quietly switches the rule off: a rule
    /// that looks only at the old place passes because it no longer finds anything there.
    /// <para>
    /// The same preservation law the platform move keeps is asked here with the Refinement map: every constant or static
    /// read-only string field of the test assembly that parses as a regular expression and names the backend is asked, on
    /// the subject its rule reads (declared in <see cref="RefinementPatternRoles"/>), about the old and the new name of every
    /// moving Refinement type. A Modules.Refinement alternative may see nothing outside Modules.Refinement.
    /// </para>
    /// <para>
    /// Strings that are not constants escape reflection, so the test project is also read as text. The strings there that
    /// name Refinement by its old Services path are exactly the ones on <see cref="RefinementStringsTheMoveRenames"/>: none
    /// missing from it, and none on it that no longer exists.
    /// </para>
    /// </summary>
    [TestFixture]
    public class RefinementRulesSeeTheMoveArchUnitTest
    {
        private const string TheArea = "Refinement";

        private const string TheParentNamespace = "Lighthouse.Backend.Modules";

        private static readonly string TheModuleNamespace = @"^Lighthouse\.Backend\.Modules\.Refinement(\.|$)";

        // Marks the Refinement named right after Modules\. in a rule pattern, directly or as one choice of a group such as
        // Modules\.(Forecasting|Refinement), so that an alternative written for the move can be told from the old ones.
        private static readonly Regex ModuleToken = new(@"(?<=Modules\\\.(?:\((?:[A-Za-z]+\|)*)?)Refinement(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout);

        private static readonly string FixtureNamespace = typeof(MadeUpRefinementRuleKeys).Namespace!;

        private static readonly Type[] TheCheckItself =
        [
            typeof(RefinementRulesSeeTheMoveArchUnitTest),
            typeof(RefinementMembership),
            typeof(RefinementType),
            typeof(RefinementPatternRoles),
            typeof(RefinementStringsTheMoveRenames),
            typeof(NamespaceMoveLaw),
        ];

        // The map names Refinement by its old namespace on purpose, so it holds before, during and after the move; it and
        // this check are the measurement, not rules the move could leave behind.
        private static readonly string[] TheMeasurementFiles =
        [
            $"{nameof(RefinementMembership)}.cs",
            $"{nameof(RefinementMembershipArchUnitTest)}.cs",
            $"{nameof(RefinementPatternRoles)}.cs",
            $"{nameof(RefinementStringsTheMoveRenames)}.cs",
            $"{nameof(RefinementRulesSeeTheMoveArchUnitTest)}.cs",
            $"{nameof(NamespaceMoveLaw)}.cs",
        ];

        private static readonly NamespaceMove TheRefinementMove = MoveWith(RefinementStringsTheMoveRenames.Entries);

        private static readonly NamespaceMoveLaw TheLaw = new(TheRefinementMove, new PatternRoleMap(RefinementPatternRoles.PatternRoles, RefinementPatternRoles.FolderOnlyMatches));

        private static readonly NamespaceMoveLaw TheLawOverTheMadeUpKeys = new(TheRefinementMove, new PatternRoleMap(MadeUpRefinementRuleRoles.Roles, MadeUpRefinementRuleRoles.FolderOnlyMatches));

        private static readonly NamespaceMoveLaw TheLawOverTheMadeUpList = new(MoveWith(MadeUpRenameList.Entries), new PatternRoleMap(RefinementPatternRoles.PatternRoles, RefinementPatternRoles.FolderOnlyMatches));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void EveryRulePatternThatSeesAMovingRefinementType_IsDeclared_AndKeepsItsRoleOnTheSubjectItsRuleReads()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var check = WhenTheRoleCheckRuns(TheLaw, patterns, RefinementPatternRoles.WholeParentPatterns);

            ThenNoPatternBreaksItsRole(check.Breaks);
            ThenEveryWholeParentPatternTakesInAllOfModules(check.NarrowerThanTheParent);
        }

        [Test]
        public void TheRoleCheck_NamesEachMadeUpPatternThatBreaksItsRole_AndPassesTheOnesThatKeepIt()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var breaks = WhenTheRoleCheckRuns(TheLawOverTheMadeUpKeys, patterns, MadeUpRefinementRuleRoles.WholeParentPatterns).Breaks;

            ThenTheBreaksNamedAre(
                breaks,
                [
                    (nameof(MadeUpRefinementRuleKeys.AContextThatSeesOnlyTheOldPlace), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpRefinementRuleKeys.AContextNamespaceWidenedByTypeName), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpRefinementRuleKeys.AReadPathWidenedOnlyToTheApplicationLayer), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpRefinementRuleKeys.APlaceWidenedIntoTheNewPlace), RoleBreak.SeesTheTypeInItsNewPlace),
                    (nameof(MadeUpRefinementRuleKeys.APlaceWidenedIntoTheNewPlace), RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation),
                    (nameof(MadeUpRefinementRuleKeys.AContextThatNeverSawRefinementWidenedIntoIt), RoleBreak.SeesAnAreaItNeverSaw),
                    (nameof(MadeUpRefinementRuleKeys.AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace), RoleBreak.FollowsATypeItSawOnlyByFolder),
                    ($"{nameof(MadeUpRefinementRuleKeys.Nested)}.{nameof(MadeUpRefinementRuleKeys.Nested.UndeclaredButSeesTheOldPlace)}", RoleBreak.NotDeclared),
                    ($"{nameof(MadeUpRefinementRuleKeys.Nested)}.{nameof(MadeUpRefinementRuleKeys.Nested.UndeclaredButSeesOnlyTheNewPlace)}", RoleBreak.NotDeclared),
                    ($"{nameof(MadeUpRefinementRuleKeys.Nested)}.{nameof(MadeUpRefinementRuleKeys.Nested.ACompositeOfAContextThatSeesOnlyTheOldPlace)}", RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpRefinementRuleKeys.AListedPatternNarrowerThanAllOfModules), RoleBreak.SeesAnAreaItNeverSaw),
                    (nameof(MadeUpRefinementRuleKeys.AnUnlistedPatternOverAllOfModules), RoleBreak.SeesAnAreaItNeverSaw),
                ],
                "a check that asks a namespace rule about full names, lets an undeclared pattern pass, accepts an old location that sees the new place, admits a part of the code into Refinement when it never saw a Refinement type, ignores a composite's component, lets a pattern follow a type it saw only by its folder, or skips static read-only fields and nested classes lets a rule go blind or wrong at the move, and passes the real rules it should name.");
            ThenTheReadPathWidenedOnlyToTheApplicationLayer_LosesExactlyTheDomainTypes(breaks);
        }

        [Test]
        public void TheWholeParentList_AdmitsTheNewSightOfAListedPatternOverAllOfModules_AndNoOther()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var check = WhenTheRoleCheckRuns(TheLawOverTheMadeUpKeys, patterns, MadeUpRefinementRuleRoles.WholeParentPatterns);

            var named = check.Breaks.Select(found => found.Pattern.Field).Distinct().ToList();
            var allOfModules = $"{nameof(MadeUpRefinementRuleKeys)}.{nameof(MadeUpRefinementRuleKeys.AListedPatternOverAllOfModules)}";
            var narrower = $"{nameof(MadeUpRefinementRuleKeys)}.{nameof(MadeUpRefinementRuleKeys.AListedPatternNarrowerThanAllOfModules)}";
            var unlisted = $"{nameof(MadeUpRefinementRuleKeys)}.{nameof(MadeUpRefinementRuleKeys.AnUnlistedPatternOverAllOfModules)}";
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    named,
                    Does.Not.Contain(allOfModules).And.Contain(narrower).And.Contain(unlisted),
                    $"The role check named [{string.Join(", ", named)}]. Expected it to pass {allOfModules}, which is on the whole-parent list and takes in all of Modules, and to name {narrower}, listed but narrower than Modules, and {unlisted}, which takes in all of Modules but is not on the list. Why it matters: the list exists so one pattern over every module may see Refinement once it becomes a module; a check that admits any pattern seeing the new place, or any listed pattern, lets a rule quietly start judging the moved types. How to fix: admit a pattern's first sight of the moved types only when it is on the whole-parent list and matches Lighthouse.Backend.Modules itself and every new namespace.");
                Assert.That(
                    check.NarrowerThanTheParent.Select(finding => finding.Split(' ')[0]),
                    Is.EqualTo(new[] { narrower }),
                    $"The whole-parent check named [{string.Join(" | ", check.NarrowerThanTheParent)}] as narrower than Modules. Expected only {narrower}. Why it matters: a listed pattern that no longer takes in all of Modules is no longer the pattern the list was granted for. How to fix: ask each listed pattern about Lighthouse.Backend.Modules itself and about the new namespace of every moving type.");
            }
        }

        [Test]
        public void NoModulesRefinementAlternativeOfARulePattern_MatchesAProductionNamespaceOrTypeOutsideModulesRefinement()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var outside = GivenTheProductionNamesOutsideModulesRefinement();

            var reachingOutside = TheLaw.AlternativesReachingOutside(patterns, outside);

            Assert.That(
                reachingOutside,
                Is.Empty,
                "These rule patterns match a production namespace or type outside Lighthouse.Backend.Modules.Refinement only through their Modules.Refinement alternative:" + Environment.NewLine +
                string.Join(Environment.NewLine, reachingOutside) + Environment.NewLine +
                @"Why it matters: an alternative added for the move must see the moved Refinement types and nothing else; one that reaches outside Modules.Refinement puts types under a rule that was never about them. How to fix: write the alternative as ^Lighthouse\.Backend\.Modules\.Refinement(\.|$), or one layer of it such as ^Lighthouse\.Backend\.Modules\.Refinement\.Domain(\.|$), anchored and ending at a namespace boundary.");
        }

        [Test]
        public void TheModulesRefinementAlternativeCheck_NamesAMadeUpAlternativeThatReachesALookalike_AndPassesTheAnchoredOnes()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var production = GivenTheProductionNamesOutsideModulesRefinement();
            const string aLookalikeNamespace = "Lighthouse.Backend.Modules.RefinementLookalike";
            var outside = new OutsideNames(
                [.. production.Namespaces, aLookalikeNamespace],
                [.. production.FullNames, aLookalikeNamespace + ".AMadeUpType"]);

            var reachingOutside = TheLawOverTheMadeUpKeys.AlternativesReachingOutside(patterns, outside);

            var named = reachingOutside.Select(finding => finding.Split(' ')[0]).Distinct().ToList();
            Assert.That(
                named,
                Is.EquivalentTo(new[] { $"{nameof(MadeUpRefinementRuleKeys)}.{nameof(MadeUpRefinementRuleKeys.AnUnanchoredModulesRefinementAlternative)}" }),
                $"The Modules.Refinement-alternative check, asked about the production names plus a made-up lookalike ({aLookalikeNamespace}), named [{string.Join(", ", reachingOutside)}]. Expected only the made-up key whose unanchored Modules.Refinement alternative matches the lookalike, and none of the keys anchored on Lighthouse.Backend.Modules.Refinement at a namespace boundary. Why it matters: a check that names compliant keys gets switched off, and one that misses a too-wide alternative lets a rule take in types it was never about. How to fix: ask each Modules.Refinement alternative about the production namespaces or full names outside Modules.Refinement, on the subject the pattern is declared with.");
        }

        [Test]
        public void TheStringsInTheTestProjectThatNameRefinementByItsOldPath_AreExactlyTheFixedRenameList()
        {
            var sources = GivenTheSourcesOfTheTestProject();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(TheLaw, RefinementStringsTheMoveRenames.Entries, sources);

            ThenTheOldPathStringsAreExactlyTheList(comparison);
        }

        [Test]
        public void TheRenameListCheck_NamesAnUnlistedOldPathString_AndAListedStringThatNoLongerExists()
        {
            var sources = GivenAMadeUpRuleFile();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(TheLawOverTheMadeUpList, MadeUpRenameList.Entries, sources);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    comparison.Unlisted.Select(finding => finding.Split(": ", 2)[0]),
                    Is.EqualTo(new[] { $"{MadeUpRenameList.TheFile}:1", $"{MadeUpRenameList.TheFile}:2", $"{MadeUpRenameList.TheFile}:3" }),
                    $"The rename-list check named [{string.Join(" | ", comparison.Unlisted)}] as unlisted. Expected the exact old implementation namespace, the old path of a port file and the old full name of a moving type (lines 1 to 3), and not the listed string, a namespace that only starts like Refinement's, a test class's namespace or a constant rule pattern the role check governs (lines 4 to 7). Why it matters: a check that names everything gets switched off, and one that names nothing lets a string keep pointing at the old place after the move. How to fix: name a string when it names Refinement or a moving Refinement type by its old Services path, after taking out what the list renames for that file.");
                Assert.That(
                    comparison.Missing.Select(entry => entry.OldText),
                    Is.EqualTo(new[] { MadeUpRenameList.AStringThatNoLongerExists }),
                    $"The rename-list check named [{string.Join(" | ", comparison.Missing.Select(entry => entry.OldText))}] as listed but gone. Expected only {MadeUpRenameList.AStringThatNoLongerExists}, which the made-up file no longer holds, and not {MadeUpRenameList.AStringThatStillExists}, which it does. Why it matters: a list compared one way only keeps entries for strings the move already renamed, so it never shrinks and stops saying what is left to do. How to fix: name every entry whose file no longer holds its old text as a string.");
            }
        }

        private static NamespaceMove MoveWith(IEnumerable<RefinementRename> renames)
            => new(
                [.. RefinementMembership.MovingTypes.Select(type => new MovingType(type.Name, TheArea, type.OldNamespace, type.NewNamespace))],
                [.. renames.Select(entry => entry.AsRenamedString())],
                TheModuleNamespace,
                ModuleToken,
                [TheArea]);

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var patterns = TheLaw.RulePatternsOf(typeof(RefinementRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type) && !TheCheckItself.Contains(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every class, nested ones included.");
            return patterns;
        }

        private static List<RulePattern> GivenTheMadeUpRulePatterns()
            => TheLaw.RulePatternsOf(typeof(RefinementRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(InFixtures));

        private static OutsideNames GivenTheProductionNamesOutsideModulesRefinement()
            => TheLaw.NamesOutsideTheNewPlace(ProductionModule.Value);

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

        private static OldPathComparison WhenTheOldPathStringsAreComparedWithTheList(NamespaceMoveLaw law, IReadOnlyList<RefinementRename> list, List<(string RelativePath, string Source)> sources)
        {
            var governed = GivenThePatternValuesTheRoleCheckGoverns();
            var unlisted = sources.SelectMany(file => law.OldPathStringsIn(file.RelativePath, file.Source, governed)).ToList();
            var byFile = sources.ToDictionary(file => file.RelativePath, file => file.Source, StringComparer.Ordinal);
            var missing = list
                .Where(entry => !byFile.TryGetValue(entry.File, out var source) || !source.Contains($"\"{entry.OldText}\"", StringComparison.Ordinal))
                .ToList();

            return new OldPathComparison(unlisted, missing);
        }

        // Only a declared pattern is left to the role check; an undeclared constant naming the old place is a string the
        // move would leave behind, so the text scan names it too.
        private static HashSet<string> GivenThePatternValuesTheRoleCheckGoverns()
            => TheLaw.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly().Where(pattern => RefinementPatternRoles.PatternRoles.ContainsKey(pattern.Field)));

        // A pattern on the whole-parent list sees the moved types for the first time after the move, rightly, so that first
        // sight is not a break, as long as the pattern still takes in all of Modules.
        private static RoleCheck WhenTheRoleCheckRuns(NamespaceMoveLaw law, List<RulePattern> patterns, IReadOnlyDictionary<string, string> wholeParentPatterns)
        {
            var narrower = patterns
                .Where(pattern => wholeParentPatterns.ContainsKey(pattern.Field) && !TakesInAllOfModules(pattern.Value))
                .Select(pattern => $"{pattern.Field} = {pattern.Value}")
                .ToList();
            var admitted = patterns
                .Where(pattern => wholeParentPatterns.ContainsKey(pattern.Field) && TakesInAllOfModules(pattern.Value))
                .Select(pattern => pattern.Field)
                .ToHashSet(StringComparer.Ordinal);
            var breaks = law.RoleBreaksOf(patterns)
                .Where(found => !(found.Break == RoleBreak.SeesAnAreaItNeverSaw && admitted.Contains(found.Pattern.Field)))
                .ToList();

            return new RoleCheck(breaks, narrower);
        }

        private static bool TakesInAllOfModules(string pattern)
            => NamespaceMoveLaw.IsMatch(TheParentNamespace, pattern)
                && RefinementMembership.MovingTypes.All(type => NamespaceMoveLaw.IsMatch(type.NewNamespace, pattern));

        private static void ThenEveryWholeParentPatternTakesInAllOfModules(List<string> narrowerThanTheParent)
            => Assert.That(
                narrowerThanTheParent,
                Is.Empty,
                $"These patterns are on RefinementPatternRoles.WholeParentPatterns but no longer match {TheParentNamespace} itself and the new namespace of every moving Refinement type:" + Environment.NewLine +
                string.Join(Environment.NewLine, narrowerThanTheParent) + Environment.NewLine +
                "Why it matters: the list lets a pattern over every module see Refinement once it becomes a module; a narrower pattern on it would see the moved types without the role check judging it. How to fix: take the pattern off the list, or give it back its sight of all of Modules.");

        private static void ThenNoPatternBreaksItsRole(List<PatternBreak> breaks)
        {
            var byPattern = breaks.GroupBy(found => found.Pattern.Field).ToList();
            var report = byPattern.Select(group =>
                $"{group.Key} = {group.First().Pattern.Value} ({group.First().Declared}){Environment.NewLine}" +
                string.Join(Environment.NewLine, group.Take(3).Select(found => found.Describe())) +
                (group.Count() > 3 ? $"{Environment.NewLine}    and {group.Count() - 3} more moving types" : string.Empty));

            Assert.That(
                byPattern.Select(group => group.Key),
                Is.Empty,
                $"{byPattern.Count} rule patterns would not keep their meaning when the Refinement types move into Lighthouse.Backend.Modules.Refinement:{Environment.NewLine}" +
                string.Join(Environment.NewLine, report) + Environment.NewLine +
                "Why it matters: a rule matches a pattern against the namespace of a type or its full name; a part of the code that does not match the new namespace stops seeing the type the moment it moves and then passes because it finds nothing, and an old location that matches the new place starts judging code that no longer lives there. " +
                @"How to fix: give a part of the code one anchored alternative, ^Lighthouse\.Backend\.Modules\.Refinement(\.|$) or a layer of it, or for a pattern on one type's full name that type's new full name; keep every old alternative; take every Modules.Refinement alternative off an old location; a pattern listed in RefinementPatternRoles.FolderOnlyMatches saw the type only by its folder and must not follow it; a constant that names one exact Refinement namespace is replaced by a selection over RefinementMembership and removed; declare an undeclared pattern in RefinementPatternRoles with its subject, role and reason.");
        }

        private static void ThenTheBreaksNamedAre(List<PatternBreak> breaks, (string Field, RoleBreak Break)[] expected, string why)
        {
            var named = breaks
                .Select(found => (Field: found.Pattern.Field[(nameof(MadeUpRefinementRuleKeys).Length + 1)..], found.Break))
                .Distinct()
                .ToList();

            Assert.That(
                named,
                Is.EquivalentTo(expected),
                $"The role check, run over made-up Refinement rule keys found by reflection, named [{string.Join(", ", named)}] and should have named exactly [{string.Join(", ", expected)}]." + Environment.NewLine +
                string.Join(Environment.NewLine, breaks.Take(12).Select(found => found.Describe())) + Environment.NewLine +
                $"Why it matters: {why} How to fix: run the same check the real rules get over these made-up keys, on the subject and role each is declared with.");
        }

        private static void ThenTheReadPathWidenedOnlyToTheApplicationLayer_LosesExactlyTheDomainTypes(List<PatternBreak> breaks)
        {
            var lost = breaks
                .Where(found => found.Pattern.Field == $"{nameof(MadeUpRefinementRuleKeys)}.{nameof(MadeUpRefinementRuleKeys.AReadPathWidenedOnlyToTheApplicationLayer)}")
                .Select(found => found.Type.Name)
                .ToList();
            var domainTypes = RefinementMembership.MovingTypes.Where(type => type.Layer == RefinementLayer.Domain).Select(type => type.Name).ToList();

            Assert.That(
                lost,
                Is.EquivalentTo(domainTypes).And.Not.Empty,
                $"A read path that selects the old Refinement namespaces and only the new Application one was named as losing [{string.Join(", ", lost)}], and should lose exactly the Domain types of the map [{string.Join(", ", domainTypes)}]. Why it matters: the module's read path must keep selecting every Refinement type once the Domain types split off into their own namespace; a selection by one exact namespace keeps working until the move and then quietly drops them. How to fix: ask each pattern about the new namespace of every moving type, Domain and Application alike.");
        }

        private static void ThenTheOldPathStringsAreExactlyTheList(OldPathComparison comparison)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    comparison.Unlisted,
                    Is.Empty,
                    "These strings in the test project name Refinement by its old Services path and are not on RefinementStringsTheMoveRenames:" + Environment.NewLine +
                    string.Join(Environment.NewLine, comparison.Unlisted) + Environment.NewLine +
                    "Why it matters: a string that is not a governed rule pattern escapes the preservation check, so a selection, comparison or message built on it keeps pointing at the old place after the move and finds nothing there. How to fix: select by the type itself (typeof) or over RefinementMembership, or word the message without the path, and remove the string. The list is fixed and only shrinks: do not add to it.");
                Assert.That(
                    comparison.Missing.Select(entry => $"{entry.File}: \"{entry.OldText}\""),
                    Is.Empty,
                    "These entries of RefinementStringsTheMoveRenames name a string their file no longer holds:" + Environment.NewLine +
                    string.Join(Environment.NewLine, comparison.Missing.Select(entry => $"{entry.File}: \"{entry.OldText}\"")) + Environment.NewLine +
                    "Why it matters: an entry for a string that is gone says the move still has to rename it, so the list stops telling what is left to do. How to fix: take the entry off the list in the same commit that renames or deletes its string.");
            }
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Lighthouse.Backend/Lighthouse.sln) above the test directory, so the test project cannot be read.");
            return directory!.FullName;
        }

        private static bool InFixtures(Type type) => type.Namespace?.StartsWith(FixtureNamespace, StringComparison.Ordinal) == true;

        private sealed record RoleCheck(List<PatternBreak> Breaks, List<string> NarrowerThanTheParent);

        private sealed record OldPathComparison(List<string> Unlisted, List<RefinementRename> Missing);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.RefinementRulesSeeTheMoveFixtures
{
    /// <summary>Made-up rule keys the checks above are run over, to show they name a key that breaks its role and pass the others.</summary>
    internal static class MadeUpRefinementRuleKeys
    {
        public const string SeesBothPlaces = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Refinement|Modules\.Refinement)(\.|$)";

        public const string SeesNeitherPlace = @"^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)";

        public const string NotARegularExpression = "(Lighthouse.Backend.Services.Implementation.Refinement";

        public const string AContextThatSeesOnlyTheOldPlace = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Refinement(\.|$)";

        public const string AContextWidenedByTheLayers = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Refinement|Modules\.Refinement\.(Domain|Application))(\.|$)";

        public const string AContextNamespaceWidenedByTypeName = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Refinement(\.|$)|Modules\.Refinement\.(Domain|Application)\.[^.]+$)";

        public const string AFullNameWidenedByTypeName = AContextNamespaceWidenedByTypeName;

        public const string AReadPathWidenedOnlyToTheApplicationLayer = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Refinement|Modules\.Refinement\.Application)$";

        public const string APlaceWidenedIntoTheNewPlace = SeesBothPlaces;

        public const string APlaceLeftInTheOldPlace = @"^Lighthouse\.Backend\.Services\.Implementation\.Refinement(\.|$)";

        public const string AContextThatNeverSawRefinementWidenedIntoIt = @"^Lighthouse\.Backend\.Modules\.(Forecasting|Refinement)(\.|$)";

        public const string AnUnanchoredModulesRefinementAlternative = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Refinement(\.|$)|Modules\.Refinement";

        public const string AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace = @"^Lighthouse\.Backend\.(Services\.Interfaces\.Refinement|Modules\.Refinement\.Application)(\.|$)";

        public const string AListedPatternOverAllOfModules = @"^Lighthouse\.Backend\.Modules(\.|$)";

        public const string AListedPatternNarrowerThanAllOfModules = @"^Lighthouse\.Backend\.Modules\.[A-Z][A-Za-z]*(\.|$)";

        public const string AnUnlistedPatternOverAllOfModules = AListedPatternOverAllOfModules;

        public const string AFolderOnlyMatchLeftInTheOldPlace = @"^Lighthouse\.Backend\.Services\.Interfaces\.Refinement(\.|$)";

        internal static class Nested
        {
            public const string UndeclaredButSeesTheOldPlace = @"^Lighthouse\.Backend\.Services\.Interfaces\.Refinement\.IRefinementCalendar$";

            public static readonly string UndeclaredButSeesOnlyTheNewPlace = @"^Lighthouse\.Backend\.Modules\.Refinement\.Domain(\.|$)";

            public static readonly string ACompositeOfAContextThatSeesOnlyTheOldPlace = AContextThatSeesOnlyTheOldPlace + "|" + SeesNeitherPlace;

            public static readonly string ACompositeOfAContextThatSeesBothPlaces = SeesBothPlaces + "|" + SeesNeitherPlace;
        }
    }

    /// <summary>The subject and role of each made-up key, as the real rules have theirs in RefinementPatternRoles.</summary>
    internal static class MadeUpRefinementRuleRoles
    {
        private const string Keys = nameof(MadeUpRefinementRuleKeys);

        public static IReadOnlyDictionary<string, PatternDeclaration> Roles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            [$"{Keys}.SeesBothPlaces"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that already names the new place"),
            [$"{Keys}.AContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that was never widened"),
            [$"{Keys}.AContextWidenedByTheLayers"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code widened by both layer namespaces of the module"),
            [$"{Keys}.AContextNamespaceWidenedByTypeName"] = PatternDeclaration.Context(PatternSubject.Namespace, "a namespace rule widened by type names, which no namespace matches"),
            [$"{Keys}.AFullNameWidenedByTypeName"] = PatternDeclaration.Context(PatternSubject.FullName, "the same text read on full names, where the type names do match"),
            [$"{Keys}.AReadPathWidenedOnlyToTheApplicationLayer"] = PatternDeclaration.Context(PatternSubject.Namespace, "a read path by exact namespace widened only to the Application layer"),
            [$"{Keys}.APlaceWidenedIntoTheNewPlace"] = PatternDeclaration.Place(PatternSubject.Namespace, "an old location that was given the new place"),
            [$"{Keys}.APlaceLeftInTheOldPlace"] = PatternDeclaration.Place(PatternSubject.Namespace, "an old location that stays where it was"),
            [$"{Keys}.AContextThatNeverSawRefinementWidenedIntoIt"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that saw no Refinement type and was given Modules.Refinement"),
            [$"{Keys}.AnUnanchoredModulesRefinementAlternative"] = PatternDeclaration.Context(PatternSubject.FullName, "a full-name rule with an unanchored Modules.Refinement alternative"),
            [$"{Keys}.AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that saw the Refinement types only by the interfaces folder and was widened into their new place"),
            [$"{Keys}.AFolderOnlyMatchLeftInTheOldPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that saw the Refinement types only by the interfaces folder and stays out of their new place"),
            [$"{Keys}.Nested.ACompositeOfAContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Composite($"{Keys}.AContextThatSeesOnlyTheOldPlace", "built from the blind part of the code"),
            [$"{Keys}.AListedPatternOverAllOfModules"] = PatternDeclaration.Context(PatternSubject.Namespace, "every module, on the whole-parent list"),
            [$"{Keys}.AListedPatternNarrowerThanAllOfModules"] = PatternDeclaration.Context(PatternSubject.Namespace, "every named module but not Modules itself, on the whole-parent list"),
            [$"{Keys}.AnUnlistedPatternOverAllOfModules"] = PatternDeclaration.Context(PatternSubject.Namespace, "every module, not on the whole-parent list"),
            [$"{Keys}.Nested.ACompositeOfAContextThatSeesBothPlaces"] = PatternDeclaration.Composite($"{Keys}.SeesBothPlaces", "built from the part of the code that names both places"),
        };

        /// <summary>The made-up whole-parent list, as the real rules have theirs in RefinementPatternRoles.</summary>
        public static IReadOnlyDictionary<string, string> WholeParentPatterns { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{Keys}.AListedPatternOverAllOfModules"] = "takes in every module",
            [$"{Keys}.AListedPatternNarrowerThanAllOfModules"] = "listed, but takes in the named modules and not Modules itself",
        };

        /// <summary>The made-up folder-only matches: the two interfaces-folder keys saw every moving type only by its folder.</summary>
        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } =
            new[] { $"{Keys}.AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace", $"{Keys}.AFolderOnlyMatchLeftInTheOldPlace" }
                .SelectMany(field => RefinementMembership.MovingTypes.Select(type => (Field: field, Type: type.Name)))
                .ToDictionary(match => match, _ => "saw the type only by its folder");
    }

    /// <summary>A made-up rule file and a made-up rename list, to show the list is compared with the strings both ways.</summary>
    internal static class MadeUpRenameList
    {
        public const string TheFile = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AMadeUpRuleFile.cs";

        public static string AStringThatStillExists => "Lighthouse.Backend.Services.Implementation.Refinement.StageRuleMatcher";

        public static string AStringThatNoLongerExists => "Services/Implementation/Refinement/SizingRefusal.cs";

        public static string TheSource { get; } = string.Join(
            Environment.NewLine,
            "\"Lighthouse.Backend.Services.Implementation.Refinement\"",
            "\"Services/Interfaces/Refinement/IRefinementCalendar.cs\"",
            "\"Lighthouse.Backend.Services.Interfaces.Refinement.RefinementView\"",
            $"\"{AStringThatStillExists}\"",
            "\"Lighthouse.Backend.Services.Implementation.RefinementLookalike\"",
            "\"Lighthouse.Backend.Tests.Services.Implementation.Refinement.StageRuleMatcherTest\"",
            $"@\"{ModuleBoundariesArchUnitTest.RefinementPattern}\"");

        public static IReadOnlyList<RefinementRename> Entries { get; } =
        [
            new(TheFile, AStringThatStillExists, "Lighthouse.Backend.Modules.Refinement.Application.StageRuleMatcher"),
            new(TheFile, AStringThatNoLongerExists, NewText: null),
        ];
    }
}
