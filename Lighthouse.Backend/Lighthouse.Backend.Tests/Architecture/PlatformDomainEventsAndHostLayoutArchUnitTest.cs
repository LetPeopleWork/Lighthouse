using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The domain-event dispatcher with its two contracts, the clock and the host facilities are the fourth platform move:
    /// every type the platform membership map gives that move lives in Lighthouse.Backend.Platform.&lt;its area&gt; and none
    /// is left where it lived before, the handlers stay with their contexts, the unit tests of the moved types moved with
    /// them into Tests/Platform/&lt;area&gt;, and the suppression, the lists and the source checks that name a moved type
    /// name its new place. Every production type name is read from the membership map and looked up in the compiled
    /// assembly, so a name that matches no type fails and names itself.
    /// </summary>
    [TestFixture]
    public class PlatformDomainEventsAndHostLayoutArchUnitTest
    {
        private static readonly string[] TheAreasOfTheMove = ["DomainEvents", "Host"];

        private static readonly string[] TheDomainEventsTestClasses = ["DomainEventDispatcherGoldTest", "DomainEventDispatcherTest"];

        private static readonly string[] TheHostTestClasses = ["LighthouseClockTest", "SystemInfoServiceTest"];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformDomainEventsAndHostLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryDomainEventAndHostType_LivesInItsPlatformArea_AndNoneIsLeftInItsOldNamespace()
        {
            var movingTypes = GivenTheTypesTheMapGivesTheDomainEventsAndHostMove();

            var verdicts = WhenEachAreaIsCheckedAgainstWhereItsTypesLivedBefore(movingTypes);

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheDomainEventHandlers_StayWithTheirContexts_AndNoneMovesIntoThePlatformDispatcherArea()
        {
            var handlers = GivenTheDomainEventHandlersOfTheProduct();

            var movedIn = WhenTheHandlersInThePlatformDispatcherAreaAreListed(handlers);

            Assert.That(
                movedIn,
                Is.Empty,
                $"These domain-event handlers sit in the platform dispatcher area:{Environment.NewLine}{string.Join(Environment.NewLine, movedIn)}{Environment.NewLine}" +
                "Why it matters: a handler is the reaction of one context to an event, so putting it next to the dispatcher takes the reaction away from the context that owns it and puts the context's rules in the platform. " +
                "How to fix: leave every handler in its context's folder and namespace; only the dispatcher and its two contracts live in Platform/DomainEvents.");
        }

        [Test]
        public void TheUnitTestsOfTheMovedTypes_LiveInTestsPlatformTheirArea_AndNoneIsLeftInItsOldNamespace()
        {
            var verdicts = WhenEachMovedTestAreaIsCheckedAgainstWhereItsTestsLivedBefore();

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheSuppressionOnTheRandomNumberService_NamesItsNewFullName()
        {
            var rows = GivenTheSuppressionTargetsTheMoveRewrites();

            var stale = WhenEachSuppressionFileIsReadForItsRewrittenTarget(rows);

            Assert.That(
                stale,
                Is.Empty,
                $"These suppression targets still name a moved member in its old place, or miss its new place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: a suppression is matched by the member's full name, so one left on the old name suppresses nothing after the move and the build then fails on the very finding it excused. " +
                "How to fix: in the move commit, change the target in GlobalSuppressions.cs to the member's new full name, as each line above says.");
        }

        [Test]
        public void TheRuleAndListStringsThatNameAMovedType_NameItsNewPlaceOnly()
        {
            var rows = GivenTheStringsTheDomainEventsAndHostMoveRenames();

            var stale = WhenEachFileIsReadForItsRenamedStrings(rows);

            Assert.That(
                stale,
                Is.Empty,
                $"These strings still name a moved type in its old place, or miss its new place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: an architecture rule, list or source check that names a type by its old full name or path silently stops looking at it after the move, so the rule is loosened without anyone deciding to. " +
                "How to fix: in the move commit, rewrite each string to the text the line above expects; do not delete or widen it. " +
                "A row of the list of host users still to move may instead go away, once its test class no longer starts a host.");
        }

        private static List<PlatformLaneType> GivenTheTypesTheMapGivesTheDomainEventsAndHostMove()
        {
            var movingTypes = PlatformLaneMembership.MovingTypes
                .Where(type => PlatformLaneMembership.Areas[type.Area] == PlatformMove.DomainEventsAndHost)
                .ToList();

            using var scope = Assert.EnterMultipleScope();
            Assert.That(
                movingTypes.Select(type => type.Area).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
                Is.EqualTo(TheAreasOfTheMove),
                "The membership map gave the domain-events and host move areas other than DomainEvents and Host. " +
                "Why it matters: the layout check would pass over an area the move is not about, or skip one it is about. How to fix: read the map's moving types whose area belongs to this move.");
            Assert.That(
                movingTypes.Select(type => type.Name),
                Does.Contain("DomainEventDispatcher").And.Contain("IDomainEventDispatcher").And.Contain("IDomainEventHandler`1")
                    .And.Contain("LighthouseClock").And.Contain("ILighthouseClock").And.Contain("RandomNumberService")
                    .And.Contain("FileSystemService").And.Contain("SystemInfoService"),
                "The membership map gave the domain-events and host move none of DomainEventDispatcher, IDomainEventDispatcher, IDomainEventHandler`1, LighthouseClock, ILighthouseClock, RandomNumberService, FileSystemService or SystemInfoService. " +
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

        private static List<TypeDefinition> GivenTheDomainEventHandlersOfTheProduct()
        {
            var handlers = ProductionModule.Value.GetTypes().Where(IsADomainEventHandler).ToList();

            Assert.That(
                handlers,
                Is.Not.Empty,
                "The compiled product holds no type that implements IDomainEventHandler<T>. " +
                "Why it matters: the check that no handler moved would pass over nothing. How to fix: look the handlers up by the contract they implement, in the contract's current name.");
            return handlers;
        }

        private static List<string> WhenTheHandlersInThePlatformDispatcherAreaAreListed(IEnumerable<TypeDefinition> handlers)
            => [.. handlers
                .Where(handler => string.Equals(handler.Namespace, "Lighthouse.Backend.Platform.DomainEvents", StringComparison.Ordinal))
                .Select(handler => handler.FullName)];

        private static bool IsADomainEventHandler(TypeDefinition type)
            => type.IsClass && type.Interfaces.Any(contract => string.Equals(contract.InterfaceType.Name, "IDomainEventHandler`1", StringComparison.Ordinal));

        private static List<KernelRuleVerdict> WhenEachMovedTestAreaIsCheckedAgainstWhereItsTestsLivedBefore()
        {
            const string TestsNamespace = "Lighthouse.Backend.Tests.Platform";
            const string OldTests = @"Lighthouse\.Backend\.Tests\.Services";

            return
            [
                ModuleRules.TypesLiveInTheirLayer(TestModule.Value, $"{TestsNamespace}.DomainEvents", TheDomainEventsTestClasses, $"^{OldTests}\\.Implementation\\.DomainEvents$"),
                ModuleRules.TypesLiveInTheirLayer(TestModule.Value, $"{TestsNamespace}.Host", TheHostTestClasses, $"^{OldTests}(\\.Implementation)?$"),
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
                .Where(row => row.RewrittenBy == PlatformMove.DomainEventsAndHost)
                .ToList();

            Assert.That(
                rows.Select(row => row.Target).ToList(),
                Is.EqualTo(new List<string> { "RandomNumberService.GetRandomNumber(System.Int32)~System.Int32" }),
                "The membership map does not give the domain-events and host move exactly the RandomNumberService.GetRandomNumber suppression. " +
                "Why it matters: the check would skip a suppression the move must rewrite. How to fix: list the suppression target the move rewrites.");
            return rows;
        }

        private static List<string> WhenEachSuppressionFileIsReadForItsRewrittenTarget(IEnumerable<SuppressionTargetToRewrite> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let typeName = row.Target[..row.Target.IndexOf('.', StringComparison.Ordinal)]
                let member = row.Target[(typeName.Length + 1)..]
                let type = PlatformLaneMembership.MovingTypes.Single(moving => moving.Name == typeName)
                let text = File.ReadAllText(Path.Combine(root, row.File))
                where text.Contains($"~M:{type.OldFullName}.{member}", StringComparison.Ordinal) || !text.Contains($"~M:{type.NewFullName}.{member}", StringComparison.Ordinal)
                select $"{row.File}: expected \"~M:{type.NewFullName}.{member}\" instead of \"~M:{type.OldFullName}.{member}\""];
        }

        private static List<StringTheMoveRenames> GivenTheStringsTheDomainEventsAndHostMoveRenames()
        {
            var rows = PlatformLaneMembership.StringsTheMoveRenames
                .Where(row => row.RenamedBy == PlatformMove.DomainEventsAndHost)
                .ToList();

            Assert.That(
                rows,
                Is.Not.Empty,
                "The membership map lists no renamed string for the domain-events and host move. " +
                "Why it matters: the check would pass over rule, list and source strings that name a moved type. How to fix: list each string the move rewrites, with the text it reads after the move.");
            return rows;
        }

        private static List<string> WhenEachFileIsReadForItsRenamedStrings(IEnumerable<StringTheMoveRenames> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let text = File.ReadAllText(Path.Combine(root, row.File))
                where text.Contains(row.OldText, StringComparison.Ordinal) || !(text.Contains(row.NewText, StringComparison.Ordinal) || TheRowWentWithItsHostUser(row, text))
                select $"{row.File}: expected \"{row.NewText}\" instead of \"{row.OldText}\""];
        }

        private static bool TheRowWentWithItsHostUser(StringTheMoveRenames row, string text)
            => row.File.EndsWith("TestHostUseGuardTest.HostUsersStillToMove.cs", StringComparison.Ordinal)
                && !text.Contains(row.NewText[(row.NewText.LastIndexOf('.') + 1)..], StringComparison.Ordinal);

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
