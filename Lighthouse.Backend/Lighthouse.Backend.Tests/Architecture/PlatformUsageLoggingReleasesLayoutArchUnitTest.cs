using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Usage data, logging and releases are the third platform move: every type the platform membership map gives that
    /// move lives in Lighthouse.Backend.Platform.&lt;its area&gt; and none is left where it lived before, the unit tests of
    /// those types moved with them into Tests/Platform/&lt;area&gt;, the suppression and the strings that name a moved type
    /// name its new place, and a change to the release code still selects the live GitHub suite. Every production type name
    /// is read from the membership map and looked up in the compiled assembly, so a name that matches no type fails and
    /// names itself.
    /// </summary>
    [TestFixture]
    public class PlatformUsageLoggingReleasesLayoutArchUnitTest
    {
        private static readonly string[] TheAreasOfTheMove = ["Logging", "Releases", "UsageData"];

        private static readonly string[] TheUsageDataTestClasses =
        [
            "UsageDataConsentPruningServiceTests",
            "UsageDataForwardingServiceTests",
            "UsageDataConsentServiceTests",
            "UsageDataDeploymentModeResolverTests",
            "UsageDataEventQueueTests",
            "UsageDataGateBudgetTests",
            "UsageDataGateConsentTests",
            "UsageDataGateReportingTests",
            "UsageDataInstancePropertiesTests",
            "UsageDataMistypedCollectorAddressTests",
            "UsageDataPublishedMessageTests",
        ];

        private static readonly string[] TheSuppressedTypes = ["LighthouseReleaseService"];

        private static readonly string[] TheLoggingTestClasses = ["ConfigFileUpdaterTests", "RecentProblemsSinkTest", "SerilogLogConfigurationTest"];

        private static readonly string[] TheReleasesTestClasses = ["LighthouseReleaseServiceIntegrationTest", "LighthouseReleaseServiceTest"];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformUsageLoggingReleasesLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryUsageDataLoggingAndReleasesType_LivesInItsPlatformArea_AndNoneIsLeftInItsOldNamespace()
        {
            var movingTypes = GivenTheTypesTheMapGivesTheUsageDataLoggingAndReleasesMove();

            var verdicts = WhenEachAreaIsCheckedAgainstWhereItsTypesLivedBefore(movingTypes);

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheUnitTestsOfTheMovedTypes_LiveInTestsPlatformTheirArea_AndNoneIsLeftInItsOldNamespace()
        {
            var verdicts = WhenEachMovedTestAreaIsCheckedAgainstWhereItsTestsLivedBefore();

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheSuppressionOnTheLighthouseReleaseService_NamesItsNewFullName()
        {
            var rows = GivenTheSuppressionTargetsTheMoveRewrites();

            var stale = WhenEachSuppressionFileIsReadForItsRewrittenTarget(rows);

            Assert.That(
                stale,
                Is.Empty,
                $"These suppression targets still name a moved type in its old place, or miss its new place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: a suppression is matched by the type's full name, so one left on the old name suppresses nothing after the move and the build then fails on the very finding it excused. " +
                "How to fix: in the move commit, change the target in GlobalSuppressions.cs to the type's new full name, as each line above says.");
        }

        [Test]
        public void TheRuleAndListStringsThatNameAMovedType_NameItsNewPlaceOnly()
        {
            var rows = GivenTheStringsTheUsageDataLoggingAndReleasesMoveRenames();

            var stale = WhenEachFileIsReadForItsRenamedStrings(rows);

            Assert.That(
                stale,
                Is.Empty,
                $"These strings still name a moved type in its old place, or miss its new place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: an architecture rule or source check that names a type by its old full name or path silently stops looking at it after the move, so the rule is loosened without anyone deciding to. " +
                "How to fix: in the move commit, rewrite each string to the text the line above expects; do not delete or widen it.");
        }

        [Test]
        public void ACodeChangeToTheReleaseCode_SelectsTheLiveGitHubSuite_AtItsNewFiles_AndAChangeToTheRestDoesNot()
        {
            var changedFiles = GivenTheFilesOfTheReleasesAreaAndOfAnotherPlatformArea();

            var selected = WhenTheCiTestSelectionClassifiesEachChangedFileForGitHub(changedFiles);

            var wrong = changedFiles
                .Where(file => !selected.TryGetValue(file.Path, out var answer) || answer != file.ShouldSelectTheGitHubSuite)
                .Select(file => $"{file.Path}: expected {file.ShouldSelectTheGitHubSuite.ToString().ToLowerInvariant()}")
                .ToList();
            Assert.That(
                wrong,
                Is.Empty,
                $"The CI test selection picks the live GitHub suite wrongly for a change to:{Environment.NewLine}{string.Join(Environment.NewLine, wrong)}{Environment.NewLine}" +
                "Why it matters: the release code talks to GitHub, so when its path is not selected a release regression reaches main without the suite that talks to GitHub ever running, and when the rest of the platform is selected that suite is spent for nothing. " +
                "How to fix: make GITHUB_REGEX in Scripts/test-selection/path-classifier.sh select exactly the GitHub service and the Lighthouse release service, their interfaces and their tests, under Platform/Releases.");
        }

        private static List<PlatformLaneType> GivenTheTypesTheMapGivesTheUsageDataLoggingAndReleasesMove()
        {
            var movingTypes = PlatformLaneMembership.MovingTypes
                .Where(type => PlatformLaneMembership.Areas[type.Area] == PlatformMove.UsageDataLoggingAndReleases)
                .ToList();

            using var scope = Assert.EnterMultipleScope();
            Assert.That(
                movingTypes.Select(type => type.Area).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
                Is.EqualTo(TheAreasOfTheMove),
                "The membership map gave the usage-data, logging and releases move areas other than Logging, Releases and UsageData. " +
                "Why it matters: the layout check would pass over an area the move is not about, or skip one it is about. How to fix: read the map's moving types whose area belongs to this move.");
            Assert.That(
                movingTypes.Select(type => type.Name),
                Does.Contain("UsageDataGate").And.Contain("IUsageDataPublisher").And.Contain("UsageDataConsentPruningService")
                    .And.Contain("RecentProblemsSink").And.Contain("SerilogLogConfiguration").And.Contain("ConfigFileUpdater")
                    .And.Contain("GitHubService").And.Contain("LighthouseReleaseService").And.Contain("AssemblyService"),
                "The membership map gave the usage-data, logging and releases move none of UsageDataGate, IUsageDataPublisher, UsageDataConsentPruningService, RecentProblemsSink, SerilogLogConfiguration, ConfigFileUpdater, GitHubService, LighthouseReleaseService or AssemblyService. " +
                "Why it matters: the layout check would pass over a list that misses the types the move is about. How to fix: read the map's moving types whose area belongs to this move.");
            return movingTypes;
        }

        private static List<KernelRuleVerdict> WhenEachAreaIsCheckedAgainstWhereItsTypesLivedBefore(IEnumerable<PlatformLaneType> types)
            => [.. types
                .GroupBy(type => type.NewNamespace, StringComparer.Ordinal)
                .Select(area => ModuleRules.TypesLiveInTheirLayer(
                    ProductionModule.Value,
                    area.Key,
                    [.. area.Select(type => type.Name)],
                    $"^({string.Join("|", area.Select(type => Regex.Escape(type.OldNamespace)).Distinct(StringComparer.Ordinal))})$"))];

        private static List<KernelRuleVerdict> WhenEachMovedTestAreaIsCheckedAgainstWhereItsTestsLivedBefore()
        {
            const string TestsNamespace = "Lighthouse.Backend.Tests.Platform";
            const string OldTests = @"Lighthouse\.Backend\.Tests\.Services\.Implementation";

            return
            [
                ModuleRules.TypesLiveInTheirLayer(TestModule.Value, $"{TestsNamespace}.UsageData", TheUsageDataTestClasses, $"^{OldTests}(\\.BackgroundServices|\\.UsageData)$"),
                ModuleRules.TypesLiveInTheirLayer(TestModule.Value, $"{TestsNamespace}.Logging", TheLoggingTestClasses, $"^{OldTests}(\\.Logging)?$"),
                ModuleRules.TypesLiveInTheirLayer(TestModule.Value, $"{TestsNamespace}.Releases", TheReleasesTestClasses, $"^{OldTests}$"),
            ];
        }

        private static void ThenEveryVerdictPasses(IEnumerable<KernelRuleVerdict> verdicts)
        {
            foreach (var verdict in verdicts)
            {
                verdict.Check();
            }
        }

        private static List<SuppressionTargetToRewrite> GivenTheSuppressionTargetsTheMoveRewrites()
        {
            var rows = PlatformLaneMembership.SuppressionTargets
                .Where(row => row.RewrittenBy == PlatformMove.UsageDataLoggingAndReleases)
                .ToList();

            Assert.That(
                rows.Select(row => row.Target),
                Is.EqualTo(TheSuppressedTypes),
                "The membership map does not give the usage-data, logging and releases move exactly the LighthouseReleaseService suppression. " +
                "Why it matters: the check would skip a suppression the move must rewrite. How to fix: list the suppression target the move rewrites.");
            return rows;
        }

        private static List<string> WhenEachSuppressionFileIsReadForItsRewrittenTarget(IEnumerable<SuppressionTargetToRewrite> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let type = PlatformLaneMembership.MovingTypes.Single(moving => moving.Name == row.Target)
                let text = File.ReadAllText(Path.Combine(root, row.File))
                where text.Contains($"~T:{type.OldFullName}", StringComparison.Ordinal) || !text.Contains($"~T:{type.NewFullName}", StringComparison.Ordinal)
                select $"{row.File}: expected \"~T:{type.NewFullName}\" instead of \"~T:{type.OldFullName}\""];
        }

        private static List<StringTheMoveRenames> GivenTheStringsTheUsageDataLoggingAndReleasesMoveRenames()
        {
            var rows = PlatformLaneMembership.StringsTheMoveRenames
                .Where(row => row.RenamedBy == PlatformMove.UsageDataLoggingAndReleases)
                .ToList();

            Assert.That(
                rows,
                Is.Not.Empty,
                "The membership map lists no renamed string for the usage-data, logging and releases move. " +
                "Why it matters: the check would pass over rule and list strings that name a moved type. How to fix: list each string the move rewrites, with the text it reads after the move.");
            return rows;
        }

        private static List<string> WhenEachFileIsReadForItsRenamedStrings(IEnumerable<StringTheMoveRenames> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let text = File.ReadAllText(Path.Combine(root, row.File))
                where text.Contains(row.OldText, StringComparison.Ordinal) || !text.Contains(row.NewText, StringComparison.Ordinal)
                select $"{row.File}: expected \"{row.NewText}\" instead of \"{row.OldText}\""];
        }

        private static List<ChangedFile> GivenTheFilesOfTheReleasesAreaAndOfAnotherPlatformArea()
        {
            const string Production = "Lighthouse.Backend/Lighthouse.Backend/Platform";
            const string Tests = "Lighthouse.Backend/Lighthouse.Backend.Tests/Platform";

            return
            [
                new($"{Production}/Releases/GitHubService.cs", true),
                new($"{Production}/Releases/IGitHubService.cs", true),
                new($"{Production}/Releases/LighthouseReleaseService.cs", true),
                new($"{Production}/Releases/ILighthouseReleaseService.cs", true),
                new($"{Tests}/Releases/LighthouseReleaseServiceTest.cs", true),
                new($"{Tests}/Releases/LighthouseReleaseServiceIntegrationTest.cs", true),
                new($"{Production}/Releases/AssemblyService.cs", false),
                new($"{Production}/Releases/IAssemblyService.cs", false),
                new($"{Production}/UsageData/UsageDataGate.cs", false),
                new($"{Production}/Logging/RecentProblemsSink.cs", false),
            ];
        }

        private static Dictionary<string, bool> WhenTheCiTestSelectionClassifiesEachChangedFileForGitHub(IReadOnlyCollection<ChangedFile> changedFiles)
        {
            var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "Scripts", "test-selection", "path-classifier.sh"));
            const string Declaration = "GITHUB_REGEX='";
            var line = script.Split('\n').FirstOrDefault(candidate => candidate.StartsWith(Declaration, StringComparison.Ordinal));
            Assert.That(
                line,
                Is.Not.Null,
                "Scripts/test-selection/path-classifier.sh declares no GITHUB_REGEX. Why it matters: it is the pattern that decides when CI runs the live GitHub suite. How to fix: declare GITHUB_REGEX there as a single-quoted extended regular expression.");

            var pattern = line![Declaration.Length..line.LastIndexOf('\'')];
            return changedFiles.ToDictionary(file => file.Path, file => Regex.IsMatch(file.Path, pattern, RegexOptions.CultureInvariant), StringComparer.Ordinal);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Lighthouse.Backend/Lighthouse.sln) above the test directory, so the files cannot be read.");
            return directory!.FullName;
        }

        private sealed record ChangedFile(string Path, bool ShouldSelectTheGitHubSuite);
    }
}
