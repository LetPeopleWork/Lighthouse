using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The write-back round, five classes and three ports, lives in Forecasting's Application, in the WriteBack namespace, and
    /// nowhere else: not in the loose Services namespaces it came from, not in both places, not in Forecasting's flat
    /// Application, and nothing but the round in the new namespace. Its eight unit-test files sit in the matching test folder.
    /// The types are read from the compiled code by full name, so this check compiles before and after the move and a type
    /// that has not moved yet is a failure that names it. The placement judgements also run on made-up inputs, one per way the
    /// move can go wrong, so a judgement that names nothing cannot pass.
    /// </summary>
    [TestFixture]
    public class WriteBackRoundLivesInForecastingArchUnitTest
    {
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
        public void EveryRoundTypeLivesInForecastingsWriteBackNamespaceAndNowhereElse()
        {
            var compiled = GivenTheTopLevelTypesOfTheCompiledCode();

            var findings = WhenThePlacementIsJudged(compiled);

            ThenThePlacementIsSound(findings);
        }

        [TestCaseSource(nameof(MadeUpWaysTheMoveGoesWrong))]
        public void ThePlacementCheckNamesEachWayTheMoveGoesWrong(string label, string[] compiledTypes, string[] expectedFindings)
        {
            var compiled = compiledTypes.ToList();

            var findings = WhenThePlacementIsJudged(compiled);

            ThenExactlyTheExpectedFindingsAreNamed(label, findings, [.. expectedFindings]);
        }

        [Test]
        public void TheEightRoundUnitTestFilesSitBesideTheirSubjectAndDeclareTheNamespaceOfTheirFolder()
        {
            var files = GivenTheSourceFilesOfTheTestProject();

            var findings = WhenTheTestFilePlacementIsJudged(files.Select(file => file.RelativePath).ToList());
            var undeclared = WhenTheNamespaceOfEachMovedTestFileIsRead(files);

            ThenTheTestFilesAreBesideTheirSubject(findings, undeclared);
        }

        [TestCaseSource(nameof(MadeUpWaysATestFileIsLeftBehind))]
        public void TheTestFileCheckNamesEachTestFileLeftBehind(string label, string[] projectFiles, string[] expectedFindings)
        {
            var files = projectFiles.ToList();

            var findings = WhenTheTestFilePlacementIsJudged(files);

            ThenExactlyTheExpectedFindingsAreNamed(label, findings, [.. expectedFindings]);
        }

        [Test]
        public void WorkItemsStillRefusesTheRoundByItsNewFullNamesAndNoLongerNamesItsOldOnes()
        {
            var pattern = WorkItemsModuleBoundariesArchUnitTest.StrayLooseServices;

            var findings = WhenTheWorkItemsStrayListIsJudged(pattern);

            ThenWorkItemsRefusesTheRoundWhereItLives(findings);
        }

        [Test]
        public void TheWorkItemsCheckNamesARoundTypeItsListDoesNotSeeInItsNewPlace()
        {
            var listNamingOnlyTheOldPlace = $"^{Regex.Escape(TheLooseImplementation())}\\.WriteBackRound$";

            var findings = WhenTheWorkItemsStrayListIsJudged(listNamingOnlyTheOldPlace);

            ThenOnlyTheSeenOldNameAndTheSevenUnseenNewNamesAreNamed(findings);
        }

        private static IEnumerable<TestCaseData> MadeUpWaysTheMoveGoesWrong()
        {
            var round = TheRoundInItsNewPlace();
            var without = (string name) => round.Where(type => !type.EndsWith("." + name, StringComparison.Ordinal));
            var extra = $"{WriteBackMembership.TheNewNamespace}.AnExtraTypeThatIsNotTheRound";
            var newcomer = $"{TheLooseInterfaces()}.IWriteBackUnknownNewcomer";

            yield return Case("a type still in the old namespace", [.. without("WriteBackRound"), TheOldFullNameOf("WriteBackRound")], [$"still in its old namespace: {TheOldFullNameOf("WriteBackRound")}", $"missing from the new namespace: {TheNewFullNameOf("WriteBackRound")}"])
                .SetName("TypeStillInOldNamespace_IsNamed");
            yield return Case("a type in both namespaces", [.. round, TheOldFullNameOf("IWriteBackService")], [$"in both namespaces: {TheOldFullNameOf("IWriteBackService")}"])
                .SetName("TypeInBothNamespaces_IsNamed");
            yield return Case("an extra type in the new namespace", [.. round, extra], [$"not part of the round: {extra}"])
                .SetName("ExtraTypeInTheWriteBackNamespace_IsNamed");
            yield return Case("a type moved to the flat Application namespace", [.. without("WriteBackCollector"), $"{TheForecastingApplication()}.WriteBackCollector"], [$"missing from the new namespace: {TheNewFullNameOf("WriteBackCollector")}"])
                .SetName("TypeInTheFlatApplication_IsNamed");
            yield return Case("a ninth type named after the round left under Services", [.. round, newcomer], [$"left under Services: {newcomer}"])
                .SetName("NewcomerNamedAfterTheRoundUnderServices_IsNamed");
            yield return Case("a round that moved cleanly", [.. round], [])
                .SetName("RoundMovedCleanly_IsNotNamed");
        }

        private static IEnumerable<TestCaseData> MadeUpWaysATestFileIsLeftBehind()
        {
            var inPlace = TheRoundUnitTestFilesInTheirNewFolder();
            var without = (string name) => inPlace.Where(path => !path.EndsWith("/" + name, StringComparison.Ordinal));

            yield return Case("a test file still in the old test folder", [.. without("WriteBackServiceTest.cs"), $"{TheOldTestFolder()}/WriteBackServiceTest.cs"], [$"missing from the new test folder: {TheNewTestFolder()}/WriteBackServiceTest.cs", $"still in the old test folder: {TheOldTestFolder()}/WriteBackServiceTest.cs"])
                .SetName("TestFileStillInOldFolder_IsNamed");
            yield return Case("a test file in both folders", [.. inPlace, $"{TheOldTestFolder()}/WriteBackRoundTest.cs"], [$"still in the old test folder: {TheOldTestFolder()}/WriteBackRoundTest.cs"])
                .SetName("TestFileInBothFolders_IsNamed");
            yield return Case("a test file nowhere", [.. without("WriteBackTriggerServiceTest.StartDates.cs")], [$"missing from the new test folder: {TheNewTestFolder()}/WriteBackTriggerServiceTest.StartDates.cs"])
                .SetName("TestFileNowhere_IsNamed");
            yield return Case("all test files in place", [.. inPlace], [])
                .SetName("TestFilesInPlace_AreNotNamed");
        }

        private static TestCaseData Case(string label, string[] input, string[] expected) => new(label, input, expected);

        private static List<string> TheRoundUnitTestFileNames() =>
        [
            "BlackoutForecastShiftWriteBackTest.cs",
            "WriteBackCollectorTest.cs",
            "WriteBackRoundTest.cs",
            "WriteBackServiceTest.cs",
            "WriteBackSuppressionWarningTest.cs",
            "WriteBackTriggerServiceTest.cs",
            "WriteBackTriggerServiceTest.BeyondTheHorizon.cs",
            "WriteBackTriggerServiceTest.StartDates.cs",
        ];

        private static List<string> TheRoundUnitTestFilesInTheirNewFolder()
            => [.. TheRoundUnitTestFileNames().Select(name => $"{TheNewTestFolder()}/{name}")];

        private static List<string> TheRoundInItsNewPlace()
            => [.. WriteBackMembership.MovingTypes.Select(type => type.NewFullName)];

        private static string TheLooseImplementation() => string.Join('.', "Lighthouse.Backend.Services", "Implementation");

        private static string TheLooseInterfaces() => string.Join('.', "Lighthouse.Backend.Services", "Interfaces");

        private static string TheServices() => string.Join('.', "Lighthouse.Backend", "Services");

        private static string TheForecastingApplication() => string.Join('.', "Lighthouse.Backend.Modules.Forecasting", "Application");

        private static string TheOldTestFolder() => string.Join('/', "Lighthouse.Backend", "Lighthouse.Backend.Tests", "Services", "Implementation");

        private static string TheNewTestFolder() => string.Join('/', "Lighthouse.Backend", "Lighthouse.Backend.Tests", "Modules", "Forecasting", "Application", "WriteBack");

        private static string TheNewTestNamespace() => string.Join('.', "Lighthouse.Backend.Tests.Modules.Forecasting.Application", "WriteBack");

        private static string TheOldFullNameOf(string name) => WriteBackMembership.MovingTypes.Single(type => type.Name == name).OldFullName;

        private static string TheNewFullNameOf(string name) => WriteBackMembership.MovingTypes.Single(type => type.Name == name).NewFullName;

        private static List<string> GivenTheTopLevelTypesOfTheCompiledCode()
        {
            var inServices = IlDependencies.TypesIn(ProductionModule.Value, $"^{Regex.Escape(TheServices())}(\\.|$)");
            var inTheNewNamespace = IlDependencies.TypesIn(ProductionModule.Value, $"^{Regex.Escape(WriteBackMembership.TheNewNamespace)}$");
            var anywhereInTheFlatApplication = IlDependencies.TypesIn(ProductionModule.Value, $"^{Regex.Escape(TheForecastingApplication())}$");

            var topLevel = inServices.Concat(inTheNewNamespace).Concat(anywhereInTheFlatApplication)
                .Where(name => !name.Contains('+'))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            Assert.That(topLevel, Has.Count.GreaterThan(20), $"Only {topLevel.Count} top-level types were read from the compiled code, so the check would pass without looking at the Services namespaces. How to fix: read the namespaces below Lighthouse.Backend.Services from the production assembly.");
            return topLevel;
        }

        private static List<(string RelativePath, string Source)> GivenTheSourceFilesOfTheTestProject()
        {
            var root = RepositoryRoot();
            var testProject = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests");
            var buildOutput = new[] { Path.Combine(testProject, "bin") + Path.DirectorySeparatorChar, Path.Combine(testProject, "obj") + Path.DirectorySeparatorChar };

            var files = Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                .Where(path => !buildOutput.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(500), $"Only {files.Count} source files were found under {testProject}, so the check would pass without reading the test project.");
            return files;
        }

        private static List<string> WhenThePlacementIsJudged(List<string> compiledTopLevelTypes)
        {
            var round = WriteBackMembership.MovingTypes;
            var present = compiledTopLevelTypes.ToHashSet(StringComparer.Ordinal);
            var newNamespace = WriteBackMembership.TheNewNamespace + ".";

            var inBoth = round.Where(type => present.Contains(type.OldFullName) && present.Contains(type.NewFullName))
                .Select(type => $"in both namespaces: {type.OldFullName}");
            var stillInTheOldNamespace = round.Where(type => present.Contains(type.OldFullName) && !present.Contains(type.NewFullName))
                .Select(type => $"still in its old namespace: {type.OldFullName}");
            var missingFromTheNewNamespace = round.Where(type => !present.Contains(type.NewFullName))
                .Select(type => $"missing from the new namespace: {type.NewFullName}");
            var extraInTheNewNamespace = compiledTopLevelTypes
                .Where(name => name.StartsWith(newNamespace, StringComparison.Ordinal) && !round.Any(type => type.NewFullName == name))
                .Select(name => $"not part of the round: {name}");
            var roundNamedLeftUnderServices = compiledTopLevelTypes
                .Where(name => name.StartsWith(TheServices() + ".", StringComparison.Ordinal) && IsNamedAfterTheRound(name))
                .Where(name => !round.Any(type => type.OldFullName == name))
                .Select(name => $"left under Services: {name}");

            return
            [
                .. inBoth.Order(StringComparer.Ordinal),
                .. stillInTheOldNamespace.Order(StringComparer.Ordinal),
                .. missingFromTheNewNamespace.Order(StringComparer.Ordinal),
                .. extraInTheNewNamespace.Order(StringComparer.Ordinal),
                .. roundNamedLeftUnderServices.Order(StringComparer.Ordinal),
            ];
        }

        // Types the round was held to leave under Services are the eight; one more named like the round is a ninth member nobody listed.
        private static bool IsNamedAfterTheRound(string fullName)
            => Regex.IsMatch(fullName[(fullName.LastIndexOf('.') + 1)..], "^I?WriteBack", RegexOptions.None, TimeSpan.FromSeconds(1));

        private static List<string> WhenTheTestFilePlacementIsJudged(List<string> relativePathsOfTheTestProject)
        {
            var present = relativePathsOfTheTestProject.ToHashSet(StringComparer.Ordinal);
            var missing = TheRoundUnitTestFileNames()
                .Where(name => !present.Contains($"{TheNewTestFolder()}/{name}"))
                .Select(name => $"missing from the new test folder: {TheNewTestFolder()}/{name}");
            var left = TheRoundUnitTestFileNames()
                .Where(name => present.Contains($"{TheOldTestFolder()}/{name}"))
                .Select(name => $"still in the old test folder: {TheOldTestFolder()}/{name}");

            return [.. missing.Order(StringComparer.Ordinal), .. left.Order(StringComparer.Ordinal)];
        }

        private static List<string> WhenTheNamespaceOfEachMovedTestFileIsRead(List<(string RelativePath, string Source)> files)
        {
            var declared = new Regex($"^\\s*namespace\\s+{Regex.Escape(TheNewTestNamespace())}\\s*(;|\\{{|$)", RegexOptions.Multiline, TimeSpan.FromSeconds(1));

            return [.. files
                .Where(file => TheRoundUnitTestFilesInTheirNewFolder().Contains(file.RelativePath, StringComparer.Ordinal))
                .Where(file => !declared.IsMatch(file.Source))
                .Select(file => file.RelativePath)
                .Order(StringComparer.Ordinal)];
        }

        private static List<string> WhenTheWorkItemsStrayListIsJudged(string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));

            return [.. WriteBackMembership.MovingTypes
                .SelectMany(type => new[]
                {
                    regex.IsMatch(type.OldFullName) ? $"still sees the old name: {type.OldFullName}" : null,
                    regex.IsMatch(type.NewFullName) ? null : $"does not see the new name: {type.NewFullName}",
                })
                .OfType<string>()
                .Order(StringComparer.Ordinal)];
        }

        private static void ThenThePlacementIsSound(List<string> findings)
            => Assert.That(
                findings,
                Is.Empty,
                "The write-back round is not placed in Forecasting's Application WriteBack namespace only:" + Environment.NewLine +
                string.Join(Environment.NewLine, findings) + Environment.NewLine +
                "Why it matters: a round type left in the loose Services namespaces is still reachable by the code that must not reach the round there, and one in both places is half moved; " +
                "a type in the flat Application or an extra type in the new namespace puts a decision in the wrong place. " +
                "How to fix: git mv each of the eight round types into Modules/Forecasting/Application/WriteBack with the namespace Lighthouse.Backend.Modules.Forecasting.Application.WriteBack, and put nothing else there.");

        private static void ThenExactlyTheExpectedFindingsAreNamed(string label, List<string> findings, List<string> expected)
            => Assert.That(
                findings,
                Is.EqualTo(expected),
                $"For made-up input with {label} the check named [{string.Join(" | ", findings)}] but should name exactly [{string.Join(" | ", expected)}]. " +
                "Why it matters: a placement check that names nothing, or the wrong thing, lets a type or a test file stay behind unseen. " +
                "How to fix: name every round type or test file that is in the old place, in both, or missing from the new one, and every extra type in the new namespace, each with its full name.");

        private static void ThenTheTestFilesAreBesideTheirSubject(List<string> findings, List<string> undeclared)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    findings,
                    Is.Empty,
                    "The unit tests of the write-back round are not all in the test folder beside their subject:" + Environment.NewLine +
                    string.Join(Environment.NewLine, findings) + Environment.NewLine +
                    "Why it matters: a test that stays in the loose Services test folder no longer sits beside what it tests, and nobody looking at the round finds it. " +
                    "How to fix: git mv each of the eight files into Lighthouse.Backend.Tests/Modules/Forecasting/Application/WriteBack.");
                Assert.That(
                    undeclared,
                    Is.Empty,
                    "These moved unit-test files do not declare the namespace of their folder:" + Environment.NewLine +
                    string.Join(Environment.NewLine, undeclared) + Environment.NewLine +
                    "Why it matters: a test namespace that differs from its folder hides where the test lives. " +
                    "How to fix: declare namespace Lighthouse.Backend.Tests.Modules.Forecasting.Application.WriteBack in each of them.");
            }
        }

        private static void ThenWorkItemsRefusesTheRoundWhereItLives(List<string> findings)
            => Assert.That(
                findings,
                Is.Empty,
                "Work Items' list of loose services it must not reach does not follow the round:" + Environment.NewLine +
                string.Join(Environment.NewLine, findings) + Environment.NewLine +
                "Why it matters: Work Items refuses the round only by the full names on this list, so a name left on the old place refuses nothing and the round in Forecasting is open to Work Items unseen. " +
                "How to fix: rename the eight round entries of StrayLooseImplementation and StrayLooseInterfaces to the full names in Lighthouse.Backend.Modules.Forecasting.Application.WriteBack, and keep every other entry.");

        private static void ThenOnlyTheSeenOldNameAndTheSevenUnseenNewNamesAreNamed(List<string> findings)
            => Assert.That(
                findings,
                Has.Count.EqualTo(9)
                    .And.Some.EqualTo($"still sees the old name: {TheOldFullNameOf("WriteBackRound")}")
                    .And.Some.EqualTo($"does not see the new name: {TheNewFullNameOf("WriteBackCollector")}"),
                $"The Work Items check named [{string.Join(" | ", findings)}]. Expected it to name the one old name the list still sees and the eight new names it does not (nine findings), and not accept a list that names the round only by its old place. " +
                "Why it matters: a check that finds nothing passes a list that has not followed the round. How to fix: name every round type whose old name the list sees or whose new name it does not.");

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root, the folder holding Lighthouse.Backend/Lighthouse.sln, was not found above the test directory.");
        }
    }
}
