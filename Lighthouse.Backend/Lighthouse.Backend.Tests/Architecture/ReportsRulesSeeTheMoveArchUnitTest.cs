using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.ReportsRulesSeeTheMoveFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Reports types are about to move out of Services into Lighthouse.Backend.Modules.Reports. Every architecture rule
    /// that finds them by name must keep its meaning after the move, or the move quietly switches the rule off: a rule that
    /// looks only at the old place passes because it no longer finds anything there.
    /// <para>
    /// The same preservation law the platform and Refinement moves keep is asked here with the Reports map: every constant or
    /// static read-only string field of the test assembly that parses as a regular expression and names the backend is asked,
    /// on the subject its rule reads (declared in <see cref="ReportsPatternRoles"/>), about the old and the new name of every
    /// moving Reports type. A Modules.Reports alternative may see nothing outside Modules.Reports.
    /// </para>
    /// <para>
    /// Strings that are not constants escape reflection, so the test project is also read as text. The strings there that
    /// name Reports by its old Services path, plainly, as a folder path, or as one choice of a regular-expression group under
    /// Services, are exactly the ones on <see cref="ReportsStringsTheMoveRenames"/>: none missing from it, and none on it that
    /// no longer exists.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ReportsRulesSeeTheMoveArchUnitTest
    {
        private const string TheArea = "Reports";

        private const string TheParentNamespace = "Lighthouse.Backend.Modules";

        private static readonly string TheModuleNamespace = @"^Lighthouse\.Backend\.Modules\.Reports(\.|$)";

        // Marks the Reports named right after Modules\. in a rule pattern, directly or as one choice of a group such as
        // Modules\.(Forecasting|Reports), so that an alternative written for the move can be told from the old ones.
        private static readonly Regex ModuleToken = new(@"(?<=Modules\\\.(?:\((?:[A-Za-z]+\|)*)?)Reports(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout);

        // A string literal of C# source: a character, a verbatim string or a regular (possibly interpolated) string.
        private static readonly Regex StringLiteral = new(
            @"'(?:[^'\\\r\n]|\\.)'|(?:\$@|@\$|@)""(?<verbatim>(?:[^""]|"""")*)""|\$?""(?<regular>(?:[^""\\\r\n]|\\.)*)""",
            RegexOptions.None,
            NamespaceMoveLaw.PatternTimeout);

        // The old Reports path in any written form, read after backslashes are dropped and slashes read as dots: Services,
        // then Implementation, Interfaces or a group of both, then Reports itself (with or without anything below it) or a
        // group that names Reports among other choices. A test namespace such as Tests.Services... is not the old path.
        private static readonly Regex AnyFormOfTheOldReportsPath = new(
            @"(?<=^|^\.|[^A-Za-z0-9_.]|[^A-Za-z0-9_.]\.|Lighthouse\.Backend\.)Services\.(?:Implementation|Interfaces|\((?:Implementation|Interfaces)(?:\|(?:Implementation|Interfaces))*\))\.(?:Reports(?![A-Za-z0-9_])|\((?:[A-Za-z]+\|)*Reports(?:\|[A-Za-z]+)*\))",
            RegexOptions.None,
            NamespaceMoveLaw.PatternTimeout);

        private static readonly string FixtureNamespace = typeof(MadeUpReportsRuleKeys).Namespace!;

        private static readonly Type[] TheCheckItself =
        [
            typeof(ReportsRulesSeeTheMoveArchUnitTest),
            typeof(ReportsMembership),
            typeof(ReportsType),
            typeof(ReportsPatternRoles),
            typeof(ReportsStringsTheMoveRenames),
            typeof(NamespaceMoveLaw),
        ];

        // The map names Reports by its old namespace on purpose, so it holds before, during and after the move; it and this
        // check are the measurement, not rules the move could leave behind.
        private static readonly string[] TheMeasurementFiles =
        [
            $"{nameof(ReportsMembership)}.cs",
            $"{nameof(ReportsMembershipArchUnitTest)}.cs",
            $"{nameof(ReportsPatternRoles)}.cs",
            $"{nameof(ReportsStringsTheMoveRenames)}.cs",
            $"{nameof(ReportsRulesSeeTheMoveArchUnitTest)}.cs",
            $"{nameof(NamespaceMoveLaw)}.cs",
        ];

        private static readonly NamespaceMove TheReportsMove = MoveWith(ReportsStringsTheMoveRenames.Entries);

        private static readonly NamespaceMoveLaw TheLaw = new(TheReportsMove, new PatternRoleMap(ReportsPatternRoles.PatternRoles, ReportsPatternRoles.FolderOnlyMatches));

        private static readonly NamespaceMoveLaw TheLawOverTheMadeUpKeys = new(TheReportsMove, new PatternRoleMap(MadeUpReportsRuleRoles.Roles, MadeUpReportsRuleRoles.FolderOnlyMatches));

        private static readonly NamespaceMoveLaw TheLawOverTheMadeUpList = new(MoveWith(MadeUpReportsRenameList.Entries), new PatternRoleMap(MadeUpReportsRuleRoles.Roles, MadeUpReportsRuleRoles.FolderOnlyMatches));

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
        public void EveryRulePatternThatSeesAMovingReportsType_IsDeclared_AndKeepsItsRoleOnTheSubjectItsRuleReads()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var check = WhenTheRoleCheckRuns(TheLaw, patterns, ReportsPatternRoles.WholeParentPatterns);

            ThenNoPatternBreaksItsRole(check.Breaks);
            ThenEveryWholeParentPatternTakesInAllOfModules(check.NarrowerThanTheParent);
        }

        [Test]
        public void TheRoleCheck_NamesEachMadeUpPatternThatBreaksItsRole_AndPassesTheOnesThatKeepIt()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var breaks = WhenTheRoleCheckRuns(TheLawOverTheMadeUpKeys, patterns, MadeUpReportsRuleRoles.WholeParentPatterns).Breaks;

            ThenTheBreaksNamedAre(
                breaks,
                [
                    (nameof(MadeUpReportsRuleKeys.AContextThatSeesOnlyTheOldPlace), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpReportsRuleKeys.AnAlternationContextThatSeesOnlyTheOldPlace), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpReportsRuleKeys.AContextNamespaceWidenedByTypeName), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpReportsRuleKeys.AContextWidenedOnlyToTheApplicationNamespace), RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpReportsRuleKeys.APlaceWidenedIntoTheNewPlace), RoleBreak.SeesTheTypeInItsNewPlace),
                    (nameof(MadeUpReportsRuleKeys.APlaceWidenedIntoTheNewPlace), RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation),
                    (nameof(MadeUpReportsRuleKeys.AContextThatNeverSawReportsWidenedIntoIt), RoleBreak.SeesAnAreaItNeverSaw),
                    (nameof(MadeUpReportsRuleKeys.AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace), RoleBreak.FollowsATypeItSawOnlyByFolder),
                    ($"{nameof(MadeUpReportsRuleKeys.Nested)}.{nameof(MadeUpReportsRuleKeys.Nested.UndeclaredButSeesTheOldPlace)}", RoleBreak.NotDeclared),
                    ($"{nameof(MadeUpReportsRuleKeys.Nested)}.{nameof(MadeUpReportsRuleKeys.Nested.UndeclaredButSeesOnlyTheNewPlace)}", RoleBreak.NotDeclared),
                    ($"{nameof(MadeUpReportsRuleKeys.Nested)}.{nameof(MadeUpReportsRuleKeys.Nested.AnInterpolatedReaderOfAContextThatSeesOnlyTheOldPlace)}", RoleBreak.StopsSeeingTheType),
                    (nameof(MadeUpReportsRuleKeys.AListedPatternNarrowerThanAllOfModules), RoleBreak.SeesAnAreaItNeverSaw),
                    (nameof(MadeUpReportsRuleKeys.AnUnlistedPatternOverAllOfModules), RoleBreak.SeesAnAreaItNeverSaw),
                ],
                "a check that asks a namespace rule about full names, lets an undeclared pattern pass, misses Reports as one choice of a group, accepts an old location that sees the new place, admits a part of the code into Reports when it never saw a Reports type, ignores the component an interpolated reader is built from, lets a pattern follow a type it saw only by its folder, or skips static read-only fields and nested classes lets a rule go blind or wrong at the move, and passes the real rules it should name.");
            ThenTheContextWidenedOnlyToTheApplicationNamespace_LosesExactlyTheDomainAndMetricsTypes(breaks);
        }

        [Test]
        public void TheWholeParentList_AdmitsTheNewSightOfAListedPatternOverAllOfModules_AndNoOther()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var check = WhenTheRoleCheckRuns(TheLawOverTheMadeUpKeys, patterns, MadeUpReportsRuleRoles.WholeParentPatterns);

            ThenOnlyTheListedPatternOverAllOfModulesIsAdmitted(check);
        }

        [Test]
        public void NoModulesReportsAlternativeOfARulePattern_MatchesAProductionNamespaceOrTypeOutsideModulesReports()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();
            var outside = GivenTheProductionNamesOutsideModulesReports();

            var reachingOutside = WhenTheModulesReportsAlternativesAreAskedAbout(TheLaw, patterns, outside);

            ThenNoAlternativeReachesOutside(reachingOutside);
        }

        [Test]
        public void TheModulesReportsAlternativeCheck_NamesAMadeUpAlternativeThatReachesALookalike_AndPassesTheAnchoredOnes()
        {
            var patterns = GivenTheMadeUpRulePatterns();
            var outside = GivenTheProductionNamesOutsideModulesReportsAndALookalike();

            var reachingOutside = WhenTheModulesReportsAlternativesAreAskedAbout(TheLawOverTheMadeUpKeys, patterns, outside);

            ThenOnlyTheUnanchoredAlternativeIsNamed(reachingOutside);
        }

        [Test]
        public void NoRuleStringNamesReportsByItsOldPath_OutsideTheRenameList()
        {
            var sources = GivenTheSourcesOfTheTestProject();
            var governed = GivenThePatternValuesTheRoleCheckGoverns();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(TheLaw, ReportsStringsTheMoveRenames.Entries, sources, governed);

            ThenTheOldPathStringsAreExactlyTheList(comparison);
        }

        [Test]
        public void TheRenameListCheck_NamesEachUnlistedOldPathString_AndAListedStringThatNoLongerExists()
        {
            var sources = GivenAMadeUpRuleFile();
            var governed = GivenThePatternValuesTheMadeUpRoleCheckGoverns();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(TheLawOverTheMadeUpList, MadeUpReportsRenameList.Entries, sources, governed);

            ThenTheMadeUpFileIsComparedBothWays(comparison);
        }

        private static NamespaceMove MoveWith(IEnumerable<ReportsRename> renames)
            => new(
                [.. ReportsMembership.MovingTypes.Select(type => new MovingType(type.Name, TheArea, type.OldNamespace, type.NewNamespace))],
                [.. renames.Select(entry => entry.AsRenamedString())],
                TheModuleNamespace,
                ModuleToken,
                [TheArea]);

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var patterns = TheLaw.RulePatternsOf(typeof(ReportsRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type) && !TheCheckItself.Contains(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every class, nested ones included.");
            return patterns;
        }

        private static List<RulePattern> GivenTheMadeUpRulePatterns()
            => TheLaw.RulePatternsOf(typeof(ReportsRulesSeeTheMoveArchUnitTest).Assembly.GetTypes().Where(InFixtures));

        private static OutsideNames GivenTheProductionNamesOutsideModulesReports()
            => TheLaw.NamesOutsideTheNewPlace(ProductionModule.Value);

        private static OutsideNames GivenTheProductionNamesOutsideModulesReportsAndALookalike()
        {
            var production = GivenTheProductionNamesOutsideModulesReports();
            return new OutsideNames(
                [.. production.Namespaces, MadeUpReportsRuleKeys.ALookalikeNamespace],
                [.. production.FullNames, MadeUpReportsRuleKeys.ALookalikeNamespace + ".AMadeUpType"]);
        }

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
            => [(MadeUpReportsRenameList.TheFile, MadeUpReportsRenameList.TheSource)];

        // Only a declared pattern is left to the role check; an undeclared constant naming the old place is a string the
        // move would leave behind, so the text scan names it too.
        private static HashSet<string> GivenThePatternValuesTheRoleCheckGoverns()
            => TheLaw.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly().Where(pattern => ReportsPatternRoles.PatternRoles.ContainsKey(pattern.Field)));

        private static HashSet<string> GivenThePatternValuesTheMadeUpRoleCheckGoverns()
            => TheLawOverTheMadeUpKeys.PatternValuesItGoverns(GivenTheMadeUpRulePatterns().Where(pattern => MadeUpReportsRuleRoles.Roles.ContainsKey(pattern.Field)));

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

        private static List<string> WhenTheModulesReportsAlternativesAreAskedAbout(NamespaceMoveLaw law, List<RulePattern> patterns, OutsideNames outside)
            => law.AlternativesReachingOutside(patterns, outside);

        private static OldPathComparison WhenTheOldPathStringsAreComparedWithTheList(NamespaceMoveLaw law, IReadOnlyList<ReportsRename> list, List<(string RelativePath, string Source)> sources, HashSet<string> governed)
        {
            var unlisted = sources
                .SelectMany(file => law.OldPathStringsIn(file.RelativePath, file.Source, governed)
                    .Concat(OtherFormsOfTheOldPathIn(file.RelativePath, file.Source, list, governed)))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var byFile = sources.ToDictionary(file => file.RelativePath, file => file.Source, StringComparer.Ordinal);
            var missing = list
                .Where(entry => !byFile.TryGetValue(entry.File, out var source) || !source.Contains($"\"{entry.OldText}\"", StringComparison.Ordinal))
                .ToList();

            return new OldPathComparison(unlisted, missing);
        }

        // The shared law reads the old path only as plain dotted text naming the area or a moving type. Reports is also named
        // as a regular expression, as one choice of a group, or by a namespace below it, so those forms are read here too. A
        // literal that is a piece of a governed rule pattern is left to the role check, like the whole pattern is.
        private static IEnumerable<string> OtherFormsOfTheOldPathIn(string relativePath, string source, IReadOnlyList<ReportsRename> list, HashSet<string> governed)
        {
            var renamedHere = list
                .Where(entry => string.Equals(entry.File, relativePath, StringComparison.Ordinal))
                .Select(entry => entry.OldText)
                .OrderByDescending(oldText => oldText.Length)
                .ToList();
            var withoutComments = ProductionSource.WithoutComments(source);

            foreach (Match literal in StringLiteral.Matches(withoutComments))
            {
                if (!literal.Groups["verbatim"].Success && !literal.Groups["regular"].Success)
                {
                    continue;
                }

                var value = literal.Groups["verbatim"].Success
                    ? literal.Groups["verbatim"].Value.Replace("\"\"", "\"", StringComparison.Ordinal)
                    : literal.Groups["regular"].Value.Replace("\\\\", "\\", StringComparison.Ordinal).Replace("\\\"", "\"", StringComparison.Ordinal);

                if (governed.Any(pattern => pattern.Contains(value, StringComparison.Ordinal)))
                {
                    continue;
                }

                var remainder = renamedHere.Aggregate(value, (text, oldText) => text.Replace(oldText, " ", StringComparison.Ordinal));
                var normalised = remainder.Replace("\\", string.Empty, StringComparison.Ordinal).Replace('/', '.');

                if (AnyFormOfTheOldReportsPath.IsMatch(normalised))
                {
                    var line = withoutComments.AsSpan(0, literal.Index).Count('\n') + 1;
                    yield return $"{relativePath}:{line}: {literal.Value}";
                }
            }
        }

        private static bool TakesInAllOfModules(string pattern)
            => NamespaceMoveLaw.IsMatch(TheParentNamespace, pattern)
                && ReportsMembership.MovingTypes.All(type => NamespaceMoveLaw.IsMatch(type.NewNamespace, pattern));

        private static void ThenEveryWholeParentPatternTakesInAllOfModules(List<string> narrowerThanTheParent)
            => Assert.That(
                narrowerThanTheParent,
                Is.Empty,
                $"These patterns are on ReportsPatternRoles.WholeParentPatterns but no longer match {TheParentNamespace} itself and the new namespace of every moving Reports type:" + Environment.NewLine +
                string.Join(Environment.NewLine, narrowerThanTheParent) + Environment.NewLine +
                "Why it matters: the list lets a pattern over every module see Reports once it becomes a module; a narrower pattern on it would see the moved types without the role check judging it. How to fix: take the pattern off the list, or give it back its sight of all of Modules.");

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
                $"{byPattern.Count} rule patterns would not keep their meaning when the Reports types move into Lighthouse.Backend.Modules.Reports:{Environment.NewLine}" +
                string.Join(Environment.NewLine, report) + Environment.NewLine +
                "Why it matters: a rule matches a pattern against the namespace of a type or its full name; a part of the code that does not match the new namespace stops seeing the type the moment it moves and then passes because it finds nothing, and an old location that matches the new place starts judging code that no longer lives there. " +
                @"How to fix: give a part of the code one anchored alternative, ^Lighthouse\.Backend\.Modules\.Reports(\.|$), and keep every old alternative; take every Modules.Reports alternative off an old location; a pattern listed in ReportsPatternRoles.FolderOnlyMatches saw the type only by its folder and must not follow it; declare an undeclared pattern in ReportsPatternRoles with its subject, role and reason.");
        }

        private static void ThenTheBreaksNamedAre(List<PatternBreak> breaks, (string Field, RoleBreak Break)[] expected, string why)
        {
            var named = breaks
                .Select(found => (Field: found.Pattern.Field[(nameof(MadeUpReportsRuleKeys).Length + 1)..], found.Break))
                .Distinct()
                .ToList();

            Assert.That(
                named,
                Is.EquivalentTo(expected),
                $"The role check, run over made-up Reports rule keys found by reflection, named [{string.Join(", ", named)}] and should have named exactly [{string.Join(", ", expected)}]." + Environment.NewLine +
                string.Join(Environment.NewLine, breaks.Take(12).Select(found => found.Describe())) + Environment.NewLine +
                $"Why it matters: {why} How to fix: run the same check the real rules get over these made-up keys, on the subject and role each is declared with.");
        }

        private static void ThenTheContextWidenedOnlyToTheApplicationNamespace_LosesExactlyTheDomainAndMetricsTypes(List<PatternBreak> breaks)
        {
            var lost = breaks
                .Where(found => found.Pattern.Field == $"{nameof(MadeUpReportsRuleKeys)}.{nameof(MadeUpReportsRuleKeys.AContextWidenedOnlyToTheApplicationNamespace)}")
                .Select(found => found.Type.Name)
                .ToList();
            var outsideTheApplicationNamespace = ReportsMembership.MovingTypes.Where(type => type.Layer != ReportsLayer.Application).Select(type => type.Name).ToList();

            Assert.That(
                lost,
                Is.EquivalentTo(outsideTheApplicationNamespace).And.Not.Empty,
                $"A pattern that selects the old Reports namespaces exactly and only the new Application namespace was named as losing [{string.Join(", ", lost)}], and should lose exactly the Domain and Metrics types of the map [{string.Join(", ", outsideTheApplicationNamespace)}]. Why it matters: a part of the code must keep seeing every Reports type once the module splits into Domain, Application and Application.Metrics; a selection by one exact namespace keeps working until the move and then quietly drops the others. How to fix: ask each pattern about the new namespace of every moving type, in every layer.");
        }

        private static void ThenOnlyTheListedPatternOverAllOfModulesIsAdmitted(RoleCheck check)
        {
            var named = check.Breaks.Select(found => found.Pattern.Field).Distinct().ToList();
            var allOfModules = $"{nameof(MadeUpReportsRuleKeys)}.{nameof(MadeUpReportsRuleKeys.AListedPatternOverAllOfModules)}";
            var narrower = $"{nameof(MadeUpReportsRuleKeys)}.{nameof(MadeUpReportsRuleKeys.AListedPatternNarrowerThanAllOfModules)}";
            var unlisted = $"{nameof(MadeUpReportsRuleKeys)}.{nameof(MadeUpReportsRuleKeys.AnUnlistedPatternOverAllOfModules)}";
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    named,
                    Does.Not.Contain(allOfModules).And.Contain(narrower).And.Contain(unlisted),
                    $"The role check named [{string.Join(", ", named)}]. Expected it to pass {allOfModules}, which is on the whole-parent list and takes in all of Modules, and to name {narrower}, listed but narrower than Modules, and {unlisted}, which takes in all of Modules but is not on the list. Why it matters: the list exists so one pattern over every module may see Reports once it becomes a module; a check that admits any pattern seeing the new place, or any listed pattern, lets a rule quietly start judging the moved types. How to fix: admit a pattern's first sight of the moved types only when it is on the whole-parent list and matches Lighthouse.Backend.Modules itself and every new namespace.");
                Assert.That(
                    check.NarrowerThanTheParent.Select(finding => finding.Split(' ')[0]),
                    Is.EqualTo(new[] { narrower }),
                    $"The whole-parent check named [{string.Join(" | ", check.NarrowerThanTheParent)}] as narrower than Modules. Expected only {narrower}. Why it matters: a listed pattern that no longer takes in all of Modules is no longer the pattern the list was granted for. How to fix: ask each listed pattern about Lighthouse.Backend.Modules itself and about the new namespace of every moving type.");
            }
        }

        private static void ThenNoAlternativeReachesOutside(List<string> reachingOutside)
            => Assert.That(
                reachingOutside,
                Is.Empty,
                "These rule patterns match a production namespace or type outside Lighthouse.Backend.Modules.Reports only through their Modules.Reports alternative:" + Environment.NewLine +
                string.Join(Environment.NewLine, reachingOutside) + Environment.NewLine +
                @"Why it matters: an alternative added for the move must see the moved Reports types and nothing else; one that reaches outside Modules.Reports puts types under a rule that was never about them. How to fix: write the alternative as ^Lighthouse\.Backend\.Modules\.Reports(\.|$), anchored and ending at a namespace boundary.");

        private static void ThenOnlyTheUnanchoredAlternativeIsNamed(List<string> reachingOutside)
        {
            var named = reachingOutside.Select(finding => finding.Split(' ')[0]).Distinct().ToList();
            Assert.That(
                named,
                Is.EquivalentTo(new[] { $"{nameof(MadeUpReportsRuleKeys)}.{nameof(MadeUpReportsRuleKeys.AnUnanchoredModulesReportsAlternative)}" }),
                $"The Modules.Reports-alternative check, asked about the production names plus a made-up lookalike ({MadeUpReportsRuleKeys.ALookalikeNamespace}), named [{string.Join(", ", reachingOutside)}]. Expected only the made-up key whose unanchored Modules.Reports alternative matches the lookalike, and none of the keys anchored on Lighthouse.Backend.Modules.Reports at a namespace boundary. Why it matters: a check that names compliant keys gets switched off, and one that misses a too-wide alternative lets a rule take in types it was never about. How to fix: ask each Modules.Reports alternative about the production namespaces or full names outside Modules.Reports, on the subject the pattern is declared with.");
        }

        private static void ThenTheOldPathStringsAreExactlyTheList(OldPathComparison comparison)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    comparison.Unlisted,
                    Is.Empty,
                    "These strings in the test project name Reports by its old Services path and are not on ReportsStringsTheMoveRenames:" + Environment.NewLine +
                    string.Join(Environment.NewLine, comparison.Unlisted) + Environment.NewLine +
                    "Why it matters: a string that is not a governed rule pattern escapes the preservation check, so a selection, comparison or message built on it keeps pointing at the old place after the move and finds nothing there. How to fix: select by the type itself (typeof) or over ReportsMembership, or word the message without the path, and remove the string. The list is fixed and only shrinks: do not add to it.");
                Assert.That(
                    comparison.Missing.Select(entry => $"{entry.File}: \"{entry.OldText}\""),
                    Is.Empty,
                    "These entries of ReportsStringsTheMoveRenames name a string their file no longer holds:" + Environment.NewLine +
                    string.Join(Environment.NewLine, comparison.Missing.Select(entry => $"{entry.File}: \"{entry.OldText}\"")) + Environment.NewLine +
                    "Why it matters: an entry for a string that is gone says the move still has to rename it, so the list stops telling what is left to do. How to fix: take the entry off the list in the same commit that renames or deletes its string.");
            }
        }

        private static void ThenTheMadeUpFileIsComparedBothWays(OldPathComparison comparison)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    comparison.Unlisted.Select(finding => finding.Split(": ", 2)[0]),
                    Is.EquivalentTo(new[] { $"{MadeUpReportsRenameList.TheFile}:1", $"{MadeUpReportsRenameList.TheFile}:2", $"{MadeUpReportsRenameList.TheFile}:3", $"{MadeUpReportsRenameList.TheFile}:4" }),
                    $"The rename-list check named [{string.Join(" | ", comparison.Unlisted)}] as unlisted. Expected the old implementation namespace, the old path of a port file, the old Metrics namespace below Reports and a regular expression naming Reports as one choice of a group under Services (lines 1 to 4), and not the listed string, a namespace that only starts like Reports, a test class's namespace, a governed rule pattern or a piece of one (lines 5 to 9). Why it matters: a check that names everything gets switched off, and one that misses a written form lets a string keep pointing at the old place after the move. How to fix: name a string when it names Reports or a moving Reports type by its old Services path in any written form, after taking out what the list renames for that file.");
                Assert.That(
                    comparison.Missing.Select(entry => entry.OldText),
                    Is.EqualTo(new[] { MadeUpReportsRenameList.AStringThatNoLongerExists }),
                    $"The rename-list check named [{string.Join(" | ", comparison.Missing.Select(entry => entry.OldText))}] as listed but gone. Expected only {MadeUpReportsRenameList.AStringThatNoLongerExists}, which the made-up file no longer holds, and not {MadeUpReportsRenameList.AStringThatStillExists}, which it does. Why it matters: a list compared one way only keeps entries for strings the move already renamed, so it never shrinks and stops saying what is left to do. How to fix: name every entry whose file no longer holds its old text as a string.");
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

        private sealed record OldPathComparison(List<string> Unlisted, List<ReportsRename> Missing);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ReportsRulesSeeTheMoveFixtures
{
    /// <summary>Made-up rule keys the checks above are run over, to show they name a key that breaks its role and pass the others.</summary>
    internal static class MadeUpReportsRuleKeys
    {
        public const string SeesBothPlaces = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Reports|Modules\.Reports)(\.|$)";

        public const string SeesNeitherPlace = @"^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)";

        public const string NotARegularExpression = "(Lighthouse.Backend.Services.Implementation.Reports";

        public const string AContextThatSeesOnlyTheOldPlace = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Reports(\.|$)";

        public const string AnAlternationContextThatSeesOnlyTheOldPlace = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Reports|Licensing)(\.|$)";

        public const string AnAlternationContextWidenedByTheModule = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Reports|Auth)(\.|$)|^Lighthouse\.Backend\.Modules\.Reports(\.|$)";

        public const string AContextNamespaceWidenedByTypeName = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Reports(\.|$)|Modules\.Reports\.(Domain|Application|Application\.Metrics)\.[^.]+$)";

        public const string AFullNameWidenedByTypeName = AContextNamespaceWidenedByTypeName;

        public const string AContextWidenedOnlyToTheApplicationNamespace = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Reports(\.Metrics)?|Modules\.Reports\.Application)$";

        public const string APlaceWidenedIntoTheNewPlace = SeesBothPlaces;

        public const string APlaceLeftInTheOldPlace = @"^Lighthouse\.Backend\.Services\.Implementation\.Reports(\.|$)";

        public const string AContextThatNeverSawReportsWidenedIntoIt = @"^Lighthouse\.Backend\.Modules\.(Forecasting|Reports)(\.|$)";

        public const string AnUnanchoredModulesReportsAlternative = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Reports(\.|$)|Modules\.Reports";

        public const string AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace = @"^Lighthouse\.Backend\.(Services\.Interfaces\.Reports|Modules\.Reports\.Application)(\.|$)";

        public const string AFolderOnlyMatchLeftInTheOldPlace = @"^Lighthouse\.Backend\.Services\.Interfaces\.Reports(\.|$)";

        public const string AListedPatternOverAllOfModules = @"^Lighthouse\.Backend\.Modules(\.|$)";

        public const string AListedPatternNarrowerThanAllOfModules = @"^Lighthouse\.Backend\.Modules\.[A-Z][A-Za-z]*(\.|$)";

        public const string AnUnlistedPatternOverAllOfModules = AListedPatternOverAllOfModules;

        // Not a pattern: a name the alternative check is asked about besides the production names.
        public static string ALookalikeNamespace => "Lighthouse.Backend.Modules.ReportsLookalike";

        internal static class Nested
        {
            public const string UndeclaredButSeesTheOldPlace = @"^Lighthouse\.Backend\.Services\.Interfaces\.Reports\.IReportWriter$";

            public static readonly string UndeclaredButSeesOnlyTheNewPlace = @"^Lighthouse\.Backend\.Modules\.Reports\.Domain(\.|$)";

            public static readonly string AnInterpolatedReaderOfAContextThatSeesOnlyTheOldPlace = $"(?:{AContextThatSeesOnlyTheOldPlace})|(?:{SeesNeitherPlace})";

            public static readonly string AnInterpolatedReaderOfAContextThatSeesBothPlaces = $"(?:{SeesBothPlaces})|(?:{SeesNeitherPlace})";
        }
    }

    /// <summary>The subject and role of each made-up key, as the real rules have theirs in ReportsPatternRoles.</summary>
    internal static class MadeUpReportsRuleRoles
    {
        private const string Keys = nameof(MadeUpReportsRuleKeys);

        public static IReadOnlyDictionary<string, PatternDeclaration> Roles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            [$"{Keys}.SeesBothPlaces"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that already names the new place"),
            [$"{Keys}.AContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that was never widened"),
            [$"{Keys}.AnAlternationContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that names Reports as one choice of a group and was never widened"),
            [$"{Keys}.AnAlternationContextWidenedByTheModule"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that names Reports as one choice of a group, widened by the anchored module alternative"),
            [$"{Keys}.AContextNamespaceWidenedByTypeName"] = PatternDeclaration.Context(PatternSubject.Namespace, "a namespace rule widened by type names, which no namespace matches"),
            [$"{Keys}.AFullNameWidenedByTypeName"] = PatternDeclaration.Context(PatternSubject.FullName, "the same text read on full names, where the type names do match"),
            [$"{Keys}.AContextWidenedOnlyToTheApplicationNamespace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a selection by exact namespace widened only to the Application namespace"),
            [$"{Keys}.APlaceWidenedIntoTheNewPlace"] = PatternDeclaration.Place(PatternSubject.Namespace, "an old location that was given the new place"),
            [$"{Keys}.APlaceLeftInTheOldPlace"] = PatternDeclaration.Place(PatternSubject.Namespace, "an old location that stays where it was"),
            [$"{Keys}.AContextThatNeverSawReportsWidenedIntoIt"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that saw no Reports type and was given Modules.Reports"),
            [$"{Keys}.AnUnanchoredModulesReportsAlternative"] = PatternDeclaration.Context(PatternSubject.FullName, "a full-name rule with an unanchored Modules.Reports alternative"),
            [$"{Keys}.AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that saw the Reports types only by the interfaces folder and was widened into their new place"),
            [$"{Keys}.AFolderOnlyMatchLeftInTheOldPlace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a part of the code that saw the Reports types only by the interfaces folder and stays out of their new place"),
            [$"{Keys}.AListedPatternOverAllOfModules"] = PatternDeclaration.Context(PatternSubject.Namespace, "every module, on the whole-parent list"),
            [$"{Keys}.AListedPatternNarrowerThanAllOfModules"] = PatternDeclaration.Context(PatternSubject.Namespace, "every named module but not Modules itself, on the whole-parent list"),
            [$"{Keys}.AnUnlistedPatternOverAllOfModules"] = PatternDeclaration.Context(PatternSubject.Namespace, "every module, not on the whole-parent list"),
            [$"{Keys}.Nested.AnInterpolatedReaderOfAContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Composite($"{Keys}.AContextThatSeesOnlyTheOldPlace", "interpolated from the blind part of the code"),
            [$"{Keys}.Nested.AnInterpolatedReaderOfAContextThatSeesBothPlaces"] = PatternDeclaration.Composite($"{Keys}.SeesBothPlaces", "interpolated from the part of the code that names both places"),
        };

        /// <summary>The made-up whole-parent list, as the real rules have theirs in ReportsPatternRoles.</summary>
        public static IReadOnlyDictionary<string, string> WholeParentPatterns { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{Keys}.AListedPatternOverAllOfModules"] = "takes in every module",
            [$"{Keys}.AListedPatternNarrowerThanAllOfModules"] = "listed, but takes in the named modules and not Modules itself",
        };

        /// <summary>The made-up folder-only matches: the two interfaces-folder keys saw every moving type only by its folder.</summary>
        public static IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches { get; } =
            new[] { $"{Keys}.AFolderOnlyMatchThatFollowsTheTypesIntoTheirNewPlace", $"{Keys}.AFolderOnlyMatchLeftInTheOldPlace" }
                .SelectMany(field => ReportsMembership.MovingTypes.Select(type => (Field: field, Type: type.Name)))
                .ToDictionary(match => match, _ => "saw the type only by its folder");
    }

    /// <summary>A made-up rule file and a made-up rename list, to show the list is compared with the strings both ways.</summary>
    internal static class MadeUpReportsRenameList
    {
        public const string TheFile = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AMadeUpRuleFile.cs";

        public static string AStringThatStillExists => "Lighthouse.Backend.Services.Implementation.Reports.ReportQueries";

        public static string AStringThatNoLongerExists => "Services/Implementation/Reports/ReportBulkRemover.cs";

        public static string TheSource { get; } = string.Join(
            Environment.NewLine,
            "\"Lighthouse.Backend.Services.Implementation.Reports\"",
            "\"Services/Interfaces/Reports/IReportWriter.cs\"",
            "\"Lighthouse.Backend.Services.Implementation.Reports.Metrics\"",
            @"@""^Lighthouse\.Backend\.Services\.Implementation\.(Licensing|Reports)(\.|$)""",
            $"\"{AStringThatStillExists}\"",
            "\"Lighthouse.Backend.Services.Implementation.ReportsLookalike\"",
            "\"Lighthouse.Backend.Tests.Services.Implementation.Reports.ReportQueriesTest\"",
            $"@\"{MadeUpReportsRuleKeys.AnAlternationContextWidenedByTheModule}\"",
            @"@""^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Reports|Auth)(\.|$)""");

        public static IReadOnlyList<ReportsRename> Entries { get; } =
        [
            new(TheFile, AStringThatStillExists, "Lighthouse.Backend.Modules.Reports.Application.ReportQueries"),
            new(TheFile, AStringThatNoLongerExists, NewText: null),
        ];
    }
}
