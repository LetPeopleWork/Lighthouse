using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The update queue's mechanics are the fifth platform move: every type the platform membership map gives that move
    /// lives in Lighthouse.Backend.Platform.UpdateQueue and none is left where it lived before, while the Team and Portfolio
    /// updaters, their shared base and their three interfaces stay where they are. The unit tests of the moved types moved
    /// with them into Tests/Platform/UpdateQueue, and the rule and list strings that name a moved type name its new place.
    /// Every production type name is read from the membership map and looked up in the compiled assembly, so a name that
    /// matches no type fails and names itself.
    /// </summary>
    [TestFixture]
    public class PlatformUpdateQueueLayoutArchUnitTest
    {
        private static readonly string[] TheAreasOfTheMove = ["UpdateQueue"];

        private static readonly string[] TheUpdateQueueTestClasses =
        [
            "AdmittedCancellationsTest",
            "CancelAllWhileWorkIsAdmittedTest",
            "InProcessUpdateAdaptersTest",
            "InProcessUpdateCancellationNotifierTest",
            "RedisUpdateStatusScriptFreezeTest",
            "RedisUpdateStatusStoreBestEffortMomentsTest",
            "UpdateMomentsTest",
            "UpdateNotificationHubTests",
            "UpdateProgressOrdinalFreezeTest",
            "UpdateQueueServiceTests",
            "UpdateStatusStoreTest",
            "RefreshRoundSummaryLineSkippedForecastTest",
        ];

        private static readonly string[] ThePipelineTypes = ["PortfolioUpdater", "TeamUpdater", "UpdateServiceBase`1", "IPortfolioUpdater", "ITeamUpdater", "IUpdateService"];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformUpdateQueueLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryUpdateQueueType_LivesInPlatformUpdateQueue_AndNoneIsLeftInItsOldNamespace()
        {
            var movingTypes = GivenTheTypesTheMapGivesTheUpdateQueueMove();

            var verdicts = WhenEachAreaIsCheckedAgainstWhereItsTypesLivedBefore(movingTypes);

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheUpdatePipelines_StayWhereTheyAre_AndNoneMovesIntoThePlatform()
        {
            var pipelines = GivenTheUpdatePipelinesTheMapKeepsInPlace();

            var misplaced = WhenEachPipelineIsLookedUpInItsNamespaceAndInThePlatform(pipelines);

            Assert.That(
                misplaced,
                Is.Empty,
                $"These update pipelines are not where they belong:{Environment.NewLine}{string.Join(Environment.NewLine, misplaced)}{Environment.NewLine}" +
                "Why it matters: the Team and Portfolio updaters and their shared base are the refresh pipelines of the work-item and forecasting contexts; only the queue that runs them is platform mechanics, so moving a pipeline into the platform hands a context's refresh rules to the platform. " +
                "How to fix: leave TeamUpdater, PortfolioUpdater, UpdateServiceBase<T> and their three interfaces in the namespaces the line above names; move only the queue's mechanics.");
        }

        [Test]
        public void TheUnitTestsOfTheMovedTypes_LiveInTestsPlatformUpdateQueue_AndNoneIsLeftInItsOldNamespace()
        {
            var verdict = WhenTheMovedTestClassesAreCheckedAgainstWhereTheyLivedBefore();

            verdict.Check();
        }

        [Test]
        public void TheRuleAndListStringsThatNameAMovedType_NameItsNewPlaceOnly()
        {
            var rows = GivenTheStringsTheUpdateQueueMoveRenames();

            var stale = WhenEachFileIsReadForItsRenamedStrings(rows);

            Assert.That(
                stale,
                Is.Empty,
                $"These strings still name a moved type in its old place, or miss its new place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: an architecture rule, list or source check that names a type by its old full name or path silently stops looking at it after the move, so the rule is loosened without anyone deciding to. " +
                "How to fix: in the move commit, rewrite each string to the text the line above expects; do not delete or widen it.");
        }

        private static List<PlatformLaneType> GivenTheTypesTheMapGivesTheUpdateQueueMove()
        {
            var movingTypes = PlatformLaneMembership.MovingTypes
                .Where(type => PlatformLaneMembership.Areas[type.Area] == PlatformMove.UpdateQueue)
                .ToList();

            using var scope = Assert.EnterMultipleScope();
            Assert.That(
                movingTypes.Select(type => type.Area).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
                Is.EqualTo(TheAreasOfTheMove),
                "The membership map gave the update-queue move areas other than UpdateQueue. " +
                "Why it matters: the layout check would pass over an area the move is not about, or skip the one it is about. How to fix: read the map's moving types whose area belongs to this move.");
            Assert.That(
                movingTypes.Select(type => type.Name),
                Does.Contain("UpdateQueueService").And.Contain("IUpdateQueueService").And.Contain("UpdateKey")
                    .And.Contain("UpdateCancellationContext").And.Contain("UpdateNotificationHub").And.Contain("RedisUpdateStatusStore")
                    .And.Contain("PostgresUpdateExecutionLock").And.Contain("RefreshRoundSummary"),
                "The membership map gave the update-queue move none of UpdateQueueService, IUpdateQueueService, UpdateKey, UpdateCancellationContext, UpdateNotificationHub, RedisUpdateStatusStore, PostgresUpdateExecutionLock or RefreshRoundSummary. " +
                "Why it matters: the layout check would pass over a list that misses the types the move is about. How to fix: read the map's moving types whose area belongs to this move.");
            Assert.That(
                movingTypes.Select(type => type.Name).Intersect(ThePipelineTypes, StringComparer.Ordinal),
                Is.Empty,
                "The membership map gave the update-queue move one of the update pipelines. " +
                "Why it matters: the move would take a context's refresh pipeline into the platform. How to fix: keep the pipelines on the map's list of types that stay.");
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

        private static List<TypeThatStays> GivenTheUpdatePipelinesTheMapKeepsInPlace()
        {
            var pipelines = PlatformLaneMembership.TypesThatStay
                .Where(type => string.Equals(type.Owner, "The update pipelines", StringComparison.Ordinal))
                .ToList();

            Assert.That(
                pipelines.Select(type => type.Name),
                Is.EquivalentTo(ThePipelineTypes),
                "The membership map does not keep exactly TeamUpdater, PortfolioUpdater, UpdateServiceBase`1, ITeamUpdater, IPortfolioUpdater and IUpdateService in place as the update pipelines. " +
                "Why it matters: the check that the pipelines stay would pass over a list that misses one of them. How to fix: list the six pipeline types on the map's types that stay, owned by the update pipelines.");
            return pipelines;
        }

        private static List<string> WhenEachPipelineIsLookedUpInItsNamespaceAndInThePlatform(IEnumerable<TypeThatStays> pipelines)
        {
            var platformTypes = IlDependencies.TypesIn(ProductionModule.Value, @"^Lighthouse\.Backend\.Platform(\.|$)");

            return [.. from pipeline in pipelines
                let inThePlatform = platformTypes.Where(fullName => fullName.EndsWith($".{pipeline.Name}", StringComparison.Ordinal)).ToList()
                where ProductionModule.Value.GetType(pipeline.FullName) is null || inThePlatform.Count > 0
                select $"{pipeline.Name}: expected in {pipeline.Namespace} and nowhere under Lighthouse.Backend.Platform, found {(inThePlatform.Count > 0 ? string.Join(" and ", inThePlatform) : "no type in its namespace")}"];
        }

        private static KernelRuleVerdict WhenTheMovedTestClassesAreCheckedAgainstWhereTheyLivedBefore()
            => ModuleRules.TypesLiveInTheirLayer(
                TestModule.Value,
                "Lighthouse.Backend.Tests.Platform.UpdateQueue",
                TheUpdateQueueTestClasses,
                @"^Lighthouse\.Backend\.Tests\.Services\.Implementation(\.BackgroundServices\.Update)?$");

        private static void ThenEveryVerdictPasses(IEnumerable<KernelRuleVerdict> verdicts)
        {
            foreach (var verdict in verdicts)
            {
                verdict.Check();
            }
        }

        private static List<StringTheMoveRenames> GivenTheStringsTheUpdateQueueMoveRenames()
        {
            var rows = PlatformLaneMembership.StringsTheMoveRenames
                .Where(row => row.RenamedBy == PlatformMove.UpdateQueue)
                .ToList();

            Assert.That(
                rows.Select(row => Path.GetFileName(row.File)),
                Does.Contain("ForecastingModuleBoundariesArchUnitTest.cs").And.Contain("WorkItemsModuleBoundariesArchUnitTest.cs").And.Contain("UpdateCancellationContextWriterArchUnitTest.cs"),
                "The membership map lists no renamed string of the update-queue move in the Forecasting or Work Items rule lists, or in the check that reads the update queue's source files. " +
                "Why it matters: the check would pass over the lists and paths that name a moved type. How to fix: list each string the move rewrites, with the text it reads after the move.");
            return rows;
        }

        private static List<string> WhenEachFileIsReadForItsRenamedStrings(IEnumerable<StringTheMoveRenames> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let text = TextOfTheRenamingFile(root, row.File)
                where text is null || text.Contains(row.OldText, StringComparison.Ordinal) || !text.Contains(row.NewText, StringComparison.Ordinal)
                select text is null
                    ? $"{row.File}: the file is not there, and no single file of that name sits under Tests/Platform"
                    : $"{row.File}: expected \"{row.NewText}\" instead of \"{row.OldText}\""];
        }

        // A listed test file that an earlier platform move took under Tests/Platform/<area> is read there, as the one file of that name.
        private static string? TextOfTheRenamingFile(string root, string listedFile)
        {
            var listedPath = Path.Combine(root, listedFile);
            if (File.Exists(listedPath))
            {
                return File.ReadAllText(listedPath);
            }

            var platformTests = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests", "Platform");
            var sameNamed = Directory.Exists(platformTests)
                ? Directory.GetDirectories(platformTests).Select(area => Path.Combine(area, Path.GetFileName(listedFile))).Where(File.Exists).ToList()
                : [];

            return sameNamed.Count == 1 ? File.ReadAllText(sameNamed[0]) : null;
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
    }
}
