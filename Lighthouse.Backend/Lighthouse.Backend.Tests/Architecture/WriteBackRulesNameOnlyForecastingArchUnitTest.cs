using System.Text.RegularExpressions;
using Lighthouse.Backend.Tests.Architecture.WriteBackRulesNameOnlyFixtures;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The write-back round now lives in Forecasting's Application, under WriteBack, and every rule of the test project names it
    /// there and nowhere else. No rule pattern that matches by full name still sees a round type in its old Services place, the
    /// round pattern takes in the new namespace and nothing else of Forecasting, no rule file keeps its own list of the round's
    /// types (the round pattern is their one owner), and no rule text guards the round by a name prefix or by a newcomer in the
    /// old Services folders. Five files name the old places on purpose, as the measurement of the move, and are skipped by
    /// exact path; that list is checked to be exactly those five. Each check also runs on made-up input, one per way a rule can
    /// keep the old way, so a check that names nothing cannot pass.
    /// </summary>
    [TestFixture]
    public class WriteBackRulesNameOnlyForecastingArchUnitTest
    {
        private const string TheOldNameKind = "names a round type by its old Services place";

        private const string TheGroupedKind = "names a round type in a grouped alternation after its old Services place";

        private const string TheInlineListKind = "holds its own list of the round's types";

        private const string ThePrefixKind = "guards the round by a type-name prefix";

        private const string TheNewcomerKind = "guards the old Services folders against a round newcomer";

        private static readonly NamespaceMoveLaw TheSweepLaw = new(
            new NamespaceMove(
                WriteBackMembership.MovingTypes,
                [],
                $"^{Regex.Escape(WriteBackMembership.TheNewNamespace)}(\\.|$)",
                new Regex("(?!)", RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []),
            new PatternRoleMap(new Dictionary<string, PatternDeclaration>(), new Dictionary<(string Field, string Type), string>()));

        private static readonly Regex TheGroupedAlternation = new(
            @"Services\.(?<area>Implementation|Interfaces|\((?:\?:)?Implementation\|Interfaces\))\.\((?:\?:)?(?<names>[A-Za-z0-9_|`]+)\)",
            RegexOptions.None,
            NamespaceMoveLaw.PatternTimeout);

        private static readonly Regex TheGroupedAfterTheBackend = new(
            @"Backend\.\((?:\?:)?(?<areas>Services\.(?:Implementation|Interfaces)(?:\|Services\.(?:Implementation|Interfaces))*)\)\.\((?:\?:)?(?<names>[A-Za-z0-9_|`]+)\)",
            RegexOptions.None,
            NamespaceMoveLaw.PatternTimeout);

        private static readonly Regex TheOldFolderNewcomer = new(
            @"(?<![A-Za-z0-9_])Services/(Implementation|Interfaces)/I?WriteBack",
            RegexOptions.None,
            NamespaceMoveLaw.PatternTimeout);

        private static readonly Regex ThePrefixPair = new("\"WriteBack\"\\s*,\\s*\"IWriteBack\"", RegexOptions.None, NamespaceMoveLaw.PatternTimeout);

        private static string TheArchitectureFolder => "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/";

        private static string[] TheFilesTheSweepSkips =>
        [
            $"{TheArchitectureFolder}{nameof(WriteBackRulesNameOnlyForecastingArchUnitTest)}.cs",
            $"{TheArchitectureFolder}{nameof(WriteBackMembership)}.cs",
            $"{TheArchitectureFolder}{nameof(WriteBackPatternRoles)}.cs",
            $"{TheArchitectureFolder}{nameof(WriteBackStringsTheMoveRenames)}.cs",
            $"{TheArchitectureFolder}{nameof(WriteBackMembershipArchUnitTest)}.cs",
        ];

        private static Type[] TheMeasurementTypes =>
        [
            typeof(WriteBackRulesNameOnlyForecastingArchUnitTest),
            typeof(WriteBackMembershipArchUnitTest),
            typeof(WriteBackMembership),
            typeof(WriteBackNeighbourThatStays),
            typeof(WriteBackPatternRoles),
            typeof(WriteBackStringsTheMoveRenames),
            typeof(WriteBackRename),
            typeof(NamespaceMoveLaw),
        ];

        [Test]
        public void NoRuleTextOfTheTestProject_GuardsTheWriteBackRoundByAnythingButItsForecastingNamespace()
        {
            var sources = GivenTheSourcesOfTheTestProjectExceptTheMeasurement();

            var found = WhenTheSweepReads(sources);

            ThenTheSweepFoundNothing(found);
        }

        [Test]
        public void TheSweep_NamesEachRuleFileThatKeepsTheOldWay_AndPassesAFileThatNamesTheRoundByItsNewNamespace()
        {
            var sources = GivenMadeUpRuleFilesThatKeepTheOldWayAndOneThatDoesNot();

            var found = WhenTheSweepReads(sources);

            ThenOnlyTheMadeUpFilesThatKeepTheOldWayAreNamed(found);
        }

        [Test]
        public void EveryRulePatternMatchedByFullName_AndEveryPatternThatMustSeeTheRoundInForecasting_SeesNoRoundTypeInItsOldPlace()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var breaks = WhenThePatternsAreReadAgainstTheOldPlaces(patterns, WriteBackPatternRoles.PatternRoles, [.. WriteBackPatternRoles.SeeTheRoundInItsNewPlace.Keys, .. WriteBackPatternRoles.NameTheRoundByItsNewFullNames.Keys]);

            ThenNoPatternSeesTheRoundInItsOldPlace(breaks);
        }

        [Test]
        public void TheOldPlaceCheck_NamesEachMadeUpPatternThatStillSeesTheRoundInItsOldPlace_AndPassesTheOnesThatDoNot()
        {
            var patterns = MadeUpRoundPatterns.Patterns;

            var breaks = WhenThePatternsAreReadAgainstTheOldPlaces(patterns, MadeUpRoundPatterns.Roles, MadeUpRoundPatterns.SeeTheRoundInItsNewPlace);

            ThenOnlyTheMadeUpPatternsThatSeeTheOldPlaceAreNamed(breaks);
        }

        [Test]
        public void TheRoundPattern_TakesInEveryRoundTypeByItsNewFullName_AndNoOtherTypeOfForecasting()
        {
            var judged = WhenTheRoundPatternJudgesTheNewNamesAndTheirNeighbours();

            ThenTheRoundPatternTakesInExactlyTheRound(judged);
        }

        [Test]
        public void TheSweepSkipsExactlyTheFiveNamedFiles_AndNamesAnExtraPathAndAPathThatIsNowhere()
        {
            var onDisk = GivenThePathsOfTheTestProject();

            var real = WhenTheSkippedFilesAreComparedWithTheNamedFive(onDisk, TheFilesTheSweepSkips);
            var withAnExtra = WhenTheSkippedFilesAreComparedWithTheNamedFive(onDisk, [.. TheFilesTheSweepSkips, MadeUpRuleFiles.AnExtraExistingPath(onDisk), MadeUpRuleFiles.APathThatIsNowhere]);

            ThenTheSkippedFilesAreExactlyTheNamedFive(real, withAnExtra, MadeUpRuleFiles.AnExtraExistingPath(onDisk));
        }

        private static List<(string RelativePath, string Source)> GivenTheSourcesOfTheTestProjectExceptTheMeasurement()
        {
            var skipped = TheFilesTheSweepSkips;
            var sources = ReadTheTestProject().Where(file => !skipped.Contains(file.RelativePath, StringComparer.Ordinal)).ToList();
            Assert.That(sources, Has.Count.GreaterThan(500), $"Only {sources.Count} source files were read from the test project, so the sweep would pass without reading its rules.");
            return sources;
        }

        private static List<string> GivenThePathsOfTheTestProject() => [.. ReadTheTestProject().Select(file => file.RelativePath)];

        private static List<(string RelativePath, string Source)> GivenMadeUpRuleFilesThatKeepTheOldWayAndOneThatDoesNot() => MadeUpRuleFiles.Files;

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var types = typeof(WriteBackRulesNameOnlyForecastingArchUnitTest).Assembly.GetTypes()
                .Where(type => !TheMeasurementTypes.Contains(type) && !InFixtures(type));
            var patterns = TheSweepLaw.RulePatternsOf(types);
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules.");
            return patterns;
        }

        private static List<Finding> WhenTheSweepReads(IReadOnlyList<(string RelativePath, string Source)> sources)
        {
            var findings = new List<Finding>();
            foreach (var (path, source) in sources)
            {
                findings.AddRange(TheSweepLaw.OldPathStringsIn(path, source, []).Select(found => new Finding(path, TheOldNameKind, found)));

                findings.AddRange(GroupedRoundNamesIn(source).Select(found => new Finding(path, TheGroupedKind, found)));

                var namedByNewFullName = WriteBackMembership.MovingTypes
                    .Where(type => source.Contains($"\"{type.NewFullName}\"", StringComparison.Ordinal))
                    .Select(type => type.Name)
                    .ToList();
                if (namedByNewFullName.Count >= 3)
                {
                    findings.Add(new Finding(path, TheInlineListKind, string.Join(", ", namedByNewFullName)));
                }

                if (ThePrefixPair.IsMatch(source))
                {
                    findings.Add(new Finding(path, ThePrefixKind, "WriteBack / IWriteBack"));
                }

                var newcomer = TheOldFolderNewcomer.Match(ProductionSource.WithoutComments(source));
                if (newcomer.Success)
                {
                    findings.Add(new Finding(path, TheNewcomerKind, newcomer.Value));
                }
            }

            return findings;
        }

        // A regex lists several names after one old place, as in Services.Implementation.(A|B|C); the contiguous old-path
        // reference misses that form, so each alternative is read as if written out in full.
        private static List<string> GroupedRoundNamesIn(string source)
        {
            var text = ProductionSource.WithoutComments(source).Replace("\\", string.Empty, StringComparison.Ordinal);
            var found = new List<string>();
            foreach (Match group in TheGroupedAlternation.Matches(text))
            {
                var area = group.Groups["area"].Value;
                var names = group.Groups["names"].Value.Split('|');
                found.AddRange(WriteBackMembership.MovingTypes
                    .Where(type => names.Contains(type.Name, StringComparer.Ordinal) && (area.StartsWith('(') || type.OldNamespace.EndsWith($".{area}", StringComparison.Ordinal)))
                    .Select(type => type.OldFullName));
            }

            foreach (Match group in TheGroupedAfterTheBackend.Matches(text))
            {
                var areas = group.Groups["areas"].Value.Split('|').Select(area => area[(area.LastIndexOf('.') + 1)..]).ToList();
                var names = group.Groups["names"].Value.Split('|');
                found.AddRange(WriteBackMembership.MovingTypes
                    .Where(type => names.Contains(type.Name, StringComparer.Ordinal) && areas.Any(area => type.OldNamespace.EndsWith($".{area}", StringComparison.Ordinal)))
                    .Select(type => type.OldFullName));
            }

            return found;
        }

        private static List<string> WhenThePatternsAreReadAgainstTheOldPlaces(IEnumerable<RulePattern> patterns, IReadOnlyDictionary<string, PatternDeclaration> roles, IEnumerable<string> mustSeeTheRoundInForecasting)
        {
            var admitted = mustSeeTheRoundInForecasting.ToHashSet(StringComparer.Ordinal);
            var breaks = new List<string>();

            foreach (var pattern in patterns)
            {
                var subject = roles.GetValueOrDefault(pattern.Field)?.Subject;
                if (!admitted.Contains(pattern.Field))
                {
                    continue;
                }

                var read = subject ?? PatternSubject.Namespace;
                breaks.AddRange(WriteBackMembership.MovingTypes
                    .Where(type => NamespaceMoveLaw.IsMatch(OldSubjectOf(type, read), pattern.Value))
                    .Select(type => $"{pattern.Field} = {pattern.Value} still sees {type.Name} in its old place {OldSubjectOf(type, read)}"));
            }

            return breaks;
        }

        private static IReadOnlyList<(string Name, bool TakenIn)> WhenTheRoundPatternJudgesTheNewNamesAndTheirNeighbours()
        {
            var newNames = WriteBackMembership.MovingTypes.Select(type => type.NewFullName);
            var neighbours = new[]
            {
                string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Application", "ForecastFilterRuleService"),
                string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Application", "WriteBackLookalike"),
                string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Adapters.Background", "ForecastUpdater"),
            };

            return [.. newNames.Concat(neighbours).Select(name => (name, NamespaceMoveLaw.IsMatch(name, ModuleRules.TheWriteBackRound)))];
        }

        private static (List<string> Skipped, List<string> Nowhere) WhenTheSkippedFilesAreComparedWithTheNamedFive(List<string> onDisk, IReadOnlyCollection<string> exclusions)
            => ([.. onDisk.Where(path => exclusions.Contains(path, StringComparer.Ordinal)).Order(StringComparer.Ordinal)],
                [.. exclusions.Where(path => !onDisk.Contains(path, StringComparer.Ordinal)).Order(StringComparer.Ordinal)]);

        private static void ThenTheSweepFoundNothing(List<Finding> found)
            => Assert.That(
                found.Select(finding => finding.Describe()),
                Is.Empty,
                "These rule texts of the test project still guard the write-back round the old way:" + Environment.NewLine +
                string.Join(Environment.NewLine, found.Select(finding => finding.Describe())) + Environment.NewLine +
                "Why it matters: the round lives in Forecasting's Application under WriteBack, and ModuleRules.TheWriteBackRound is its one owner; " +
                "an old full name, a list of the round's types that a rule keeps by itself, or a name prefix says the round is somewhere it is not, " +
                "so a later change to the round has to be made in several places and one of them is missed. " +
                "How to fix: delete the list or the prefix and let the rule read ModuleRules.TheWriteBackRound; name no round type by its old Services place.");

        private static void ThenOnlyTheMadeUpFilesThatKeepTheOldWayAreNamed(List<Finding> found)
            => Assert.That(
                found.Select(finding => (finding.Path, finding.Kind)).Distinct().Order().ToList(),
                Is.EqualTo(MadeUpRuleFiles.TheFindingsTheSweepMustMake),
                $"The sweep named [{string.Join(" | ", found.Select(finding => finding.Describe()))}]. Expected the file naming a round type by its old place, the file with its own list of the round, " +
                "the file with the name prefixes and the old-folder newcomer, and not the file that names the round by its Forecasting namespace and one new full name. " +
                "Why it matters: a sweep that names nothing, or names a clean file, cannot tell a rule that kept the old way from one that did not. " +
                "How to fix: report every old place, every list of three or more round types and every name prefix or old-folder newcomer, and nothing else.");

        private static void ThenNoPatternSeesTheRoundInItsOldPlace(List<string> breaks)
            => Assert.That(
                breaks,
                Is.Empty,
                "These rule patterns still see the write-back round in its old Services place:" + Environment.NewLine +
                string.Join(Environment.NewLine, breaks) + Environment.NewLine +
                "Why it matters: a pattern that names an old full name keeps judging code that left; a round pattern with an old alternative lets a new type " +
                "of the old place join the round unseen. " +
                "How to fix: narrow ModuleRules.TheWriteBackRound to ^Lighthouse\\.Backend\\.Modules\\.Forecasting\\.Application\\.WriteBack\\. only, and take any other old full name of the round out of the pattern.");

        private static void ThenOnlyTheMadeUpPatternsThatSeeTheOldPlaceAreNamed(List<string> breaks)
        {
            var named = breaks.Select(found => found.Split(" = ")[0]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            Assert.That(
                named,
                Is.EqualTo(MadeUpRoundPatterns.TheKeysThatMustBeNamed),
                $"The old-place check named [{string.Join(", ", named)}]. Expected the round pattern that kept an old alternative, the old location by full name that still names a round type, " +
                "and the Forecasting pattern that takes in the old namespace, and not the round pattern of the new place only or the old location that names only types that stay. " +
                "Why it matters: a check that passes a pattern with an old alternative leaves the round half moved. " +
                "How to fix: read every full-name pattern and every pattern that must see the round in Forecasting against the old full name or old namespace of each round type.");
        }

        private static void ThenTheRoundPatternTakesInExactlyTheRound(IReadOnlyList<(string Name, bool TakenIn)> judged)
            => Assert.That(
                judged.Select(item => $"{item.Name}: {(item.TakenIn ? "taken in" : "left out")}").ToList(),
                Is.EqualTo(WriteBackMembership.MovingTypes.Select(type => $"{type.NewFullName}: taken in")
                    .Concat(
                    [
                        $"{string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Application", "ForecastFilterRuleService")}: left out",
                        $"{string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Application", "WriteBackLookalike")}: left out",
                        $"{string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Adapters.Background", "ForecastUpdater")}: left out",
                    ]).ToList()),
                "ModuleRules.TheWriteBackRound must take in each of the eight round types by its full name in the WriteBack namespace of Forecasting's Application, " +
                "and no other Forecasting type, not even one whose name only starts like the namespace. " +
                "Why it matters: Promises, Access and Licensing refuse the round by this one pattern, so a gap lets a type join the round unseen and an excess refuses an edge they may make by right. " +
                "How to fix: make the pattern ^Lighthouse\\.Backend\\.Modules\\.Forecasting\\.Application\\.WriteBack\\. and nothing else.");

        private static void ThenTheSkippedFilesAreExactlyTheNamedFive((List<string> Skipped, List<string> Nowhere) real, (List<string> Skipped, List<string> Nowhere) withAnExtra, string theExtra)
        {
            var theFive = new[]
            {
                "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WriteBackMembership.cs",
                "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WriteBackMembershipArchUnitTest.cs",
                "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WriteBackPatternRoles.cs",
                "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WriteBackRulesNameOnlyForecastingArchUnitTest.cs",
                "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/WriteBackStringsTheMoveRenames.cs",
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    real.Skipped,
                    Is.EqualTo(theFive),
                    "The sweep must skip exactly the oracle, the membership map, the pattern roles, the rename list and the membership check, by exact path, because they name the old places on purpose. " +
                    "Why it matters: every other file skipped is a rule file the sweep never reads. How to fix: skip those five paths and no other.");
                Assert.That(real.Nowhere, Is.Empty, $"The skip list names paths that are not in the test project: {string.Join(", ", real.Nowhere)}. How to fix: name only files that exist.");
                Assert.That(
                    withAnExtra.Skipped.Except(real.Skipped).ToList(),
                    Is.EqualTo(new[] { theExtra }),
                    "The comparison must name an extra path added to the skip list. Why it matters: a skip list that grows hides a rule file from the sweep. How to fix: compare the skipped files with the five named.");
                Assert.That(
                    withAnExtra.Nowhere,
                    Is.EqualTo(new[] { MadeUpRuleFiles.APathThatIsNowhere }),
                    "The comparison must name a skipped path that is nowhere in the test project. Why it matters: a mistyped path skips nothing and looks like a skipped file. How to fix: report every skip-list path with no file behind it.");
            }
        }

        private static string OldSubjectOf(MovingType type, PatternSubject subject)
            => subject == PatternSubject.Namespace ? type.OldNamespace : type.OldFullName;

        private static bool InFixtures(Type type)
            => type.Namespace?.StartsWith(typeof(WriteBackRulesNameOnlyForecastingArchUnitTest).Namespace + ".", StringComparison.Ordinal) == true
                && type.Namespace.EndsWith("Fixtures", StringComparison.Ordinal);

        private static List<(string RelativePath, string Source)> ReadTheTestProject()
        {
            var root = RepositoryRoot();
            var testProject = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests");
            var buildOutput = new[] { Path.Combine(testProject, "bin") + Path.DirectorySeparatorChar, Path.Combine(testProject, "obj") + Path.DirectorySeparatorChar };

            return [.. Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                .Where(path => !buildOutput.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))];
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root, the folder holding Lighthouse.Backend/Lighthouse.sln, was not found above the test directory.");
        }

        private sealed record Finding(string Path, string Kind, string Detail)
        {
            public string Describe() => $"{Path} {Kind}: {Detail}";
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackRulesNameOnlyFixtures
{
    /// <summary>Made-up rule files, one per way a rule keeps the old way, and one that names the round by its new place.</summary>
    internal static class MadeUpRuleFiles
    {
        private const string Folder = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/";

        public static string APathThatIsNowhere => $"{Folder}AMadeUpRuleFileThatIsNowhere.cs";

        public static IReadOnlyList<(string Path, string Kind)> TheFindingsTheSweepMustMake { get; } =
        [
            ($"{Folder}AMadeUpGroupedAfterTheBackendRule.cs", "names a round type in a grouped alternation after its old Services place"),
            ($"{Folder}AMadeUpGroupedRule.cs", "names a round type in a grouped alternation after its old Services place"),
            ($"{Folder}AMadeUpListingTest.cs", "holds its own list of the round's types"),
            ($"{Folder}AMadeUpOldNameRule.cs", "names a round type by its old Services place"),
            ($"{Folder}AMadeUpPlacementTest.cs", "guards the old Services folders against a round newcomer"),
            ($"{Folder}AMadeUpPlacementTest.cs", "guards the round by a type-name prefix"),
        ];

        public static List<(string RelativePath, string Source)> Files =>
        [
            ($"{Folder}AMadeUpOldNameRule.cs", Source($"var left = \"{string.Join('.', "Lighthouse.Backend.Services.Implementation", "WriteBackCollector")}\";")),
            ($"{Folder}AMadeUpListingTest.cs", Source(string.Join(Environment.NewLine, WriteBackMembership.MovingTypes.Select(type => $"    \"{type.NewFullName}\",")))),
            ($"{Folder}AMadeUpPlacementTest.cs", Source($"var prefixes = new[] {{ \"WriteBack\", \"IWriteBack\" }};{Environment.NewLine}var newcomer = \"{string.Join('/', "Services", "Interfaces", "IWriteBackMadeUp.cs")}\";")),
            ($"{Folder}AMadeUpGroupedRule.cs", Source("var round = @\"" + string.Join(string.Empty, @"^Lighthouse\.Backend\.Services\.Implementation\.", "(FeatureListService|WriteBackService|WriteBackRound)$") + "\";")),
            ($"{Folder}AMadeUpGroupedAfterTheBackendRule.cs", Source("var round = @\"" + string.Join(string.Empty, @"^Lighthouse\.Backend\.(Services\.Implementation|Services\.Interfaces)\.", "(IFeatureListService|IWriteBackService)$") + "\";")),
            ($"{Folder}ACleanRuleFile.cs", Source("var stays = @\"" + string.Join(string.Empty, @"^Lighthouse\.Backend\.Services\.Implementation\.", "(FeatureListService|EstimateNormalizer)$") + "\";" + Environment.NewLine + $"var round = \"{WriteBackMembership.TheNewNamespace}.WriteBackService\";{Environment.NewLine}var edge = \"{WriteBackMembership.TheNewNamespace}.WriteBackTriggerService -> Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService\";")),
        ];

        public static string AnExtraExistingPath(IReadOnlyList<string> onDisk) => onDisk.First(path => path.EndsWith("ModuleRules.cs", StringComparison.Ordinal));

        private static string Source(string body) => $"internal static class AMadeUpRuleFile{Environment.NewLine}{{{Environment.NewLine}{body}{Environment.NewLine}}}";
    }

    /// <summary>Made-up rule patterns with their declared roles, to show the old-place check names the ones that still see the round's old place.</summary>
    internal static class MadeUpRoundPatterns
    {
        public static IReadOnlyList<string> TheKeysThatMustBeNamed { get; } =
        [
            "MadeUpRoundRules.AForecastingPatternTakingInTheOldNamespace",
            "MadeUpRoundRules.AnOldLocationByFullNameNamingARoundType",
            "MadeUpRoundRules.TheRoundPatternWithAnOldAlternative",
        ];

        public static IReadOnlyList<string> SeeTheRoundInItsNewPlace { get; } =
        [
            "MadeUpRoundRules.TheRoundPatternWithAnOldAlternative",
            "MadeUpRoundRules.TheRoundPatternOfTheNewPlaceOnly",
            "MadeUpRoundRules.AForecastingPatternTakingInTheOldNamespace",
            "MadeUpRoundRules.AForecastingPatternOfForecastingOnly",
            "MadeUpRoundRules.AnOldLocationByFullNameNamingARoundType",
            "MadeUpRoundRules.AnOldLocationByFullNameNamingOnlyTypesThatStay",
        ];

        public static IReadOnlyDictionary<string, PatternDeclaration> Roles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            ["MadeUpRoundRules.TheRoundPatternWithAnOldAlternative"] = PatternDeclaration.Context(PatternSubject.FullName, "the round with an old alternative left"),
            ["MadeUpRoundRules.TheRoundPatternOfTheNewPlaceOnly"] = PatternDeclaration.Context(PatternSubject.FullName, "the round by its new place only"),
            ["MadeUpRoundRules.AForecastingPatternTakingInTheOldNamespace"] = PatternDeclaration.Context(PatternSubject.Namespace, "a pattern that takes in Forecasting and the old namespace"),
            ["MadeUpRoundRules.AForecastingPatternOfForecastingOnly"] = PatternDeclaration.Context(PatternSubject.Namespace, "a pattern that takes in Forecasting only"),
            ["MadeUpRoundRules.AnOldLocationByFullNameNamingARoundType"] = PatternDeclaration.Place(PatternSubject.FullName, "an old location that still names a round type"),
            ["MadeUpRoundRules.AnOldLocationByFullNameNamingOnlyTypesThatStay"] = PatternDeclaration.Place(PatternSubject.FullName, "an old location that names only types that stay"),
        };

        public static IReadOnlyList<RulePattern> Patterns { get; } =
        [
            new("MadeUpRoundRules.TheRoundPatternWithAnOldAlternative", @"^Lighthouse\.Backend\.(Services\.Implementation\.(WriteBackService|WriteBackRound)|Modules\.Forecasting\.Application\.WriteBack\..*)$"),
            new("MadeUpRoundRules.TheRoundPatternOfTheNewPlaceOnly", @"^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack\."),
            new("MadeUpRoundRules.AForecastingPatternTakingInTheOldNamespace", @"^Lighthouse\.Backend\.(Services\.Implementation|Modules\.Forecasting)(\.|$)"),
            new("MadeUpRoundRules.AForecastingPatternOfForecastingOnly", @"^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)"),
            new("MadeUpRoundRules.AnOldLocationByFullNameNamingARoundType", @"^Lighthouse\.Backend\.Services\.Interfaces\.(IFeatureListService|IWriteBackTriggerService)$"),
            new("MadeUpRoundRules.AnOldLocationByFullNameNamingOnlyTypesThatStay", @"^Lighthouse\.Backend\.Services\.Interfaces\.(IFeatureListService|IOrphanedFeatureCleanupService)$"),
        ];
    }
}
