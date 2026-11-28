using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Fixes which types make up Work Items before any of them moves into Modules/WorkItems. The list is read from the
    /// compiled code: every top-level type in a Work Items namespace, old or new, plus the reactions and refresh-log
    /// services that live elsewhere today and are found by name. Each such type is either on Moves or on Stays with the
    /// context that keeps it; a type on neither list, or a listed type the code no longer holds, fails and is named.
    /// Names are compared without their namespace, so the same lists hold before and after the move.
    /// </summary>
    [TestFixture]
    public class WorkItemsMembershipArchUnitTest
    {
        private const string WorkItemsNamespaces =
            @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.(WorkItems|TeamData|WorkItemRules|Dependencies|Parents)|Modules\.WorkItems)(\.|$)";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] Moves =
        [
            "WorkItemsModule",
            "WorkItemService", "IWorkItemService", "BlockedItemService", "IBlockedItemService", "SyncModeResolver", "FetchFingerprint",
            "TeamDataService", "ITeamDataService",
            "RuleEvaluator", "RuleSetSemantics", "IRuleEvaluator", "IRuleFieldProvider", "WorkItemFieldProvider", "FeatureFieldProvider",
            "DependencyDecision", "DependencyHonourPolicy", "DependencyReconciler", "DependencyRefreshReporter", "DependencyCycleDetector",
            "DependencyFacts", "DependencyLoop", "DependencyLoops", "DependenciesAsRead", "FeatureDependencyRendering", "WhatTheForecastWaitsFor",
            "IDependencyDecision", "IDependencyHonourPolicy", "IDependencyReconciler", "IDependencyRefreshReporter", "IWhatTheForecastWaitsFor",
            "DependencyHonourInput", "FeatureDependencyFacts",
            "ParentPlacementReport", "RecordPlacement",
            "WorkItemBlockedTransitionCaptureHandler", "WorkItemBlockedTransitionCloseHandler",
            "FeatureBlockedTransitionCaptureHandler", "FeatureBlockedTransitionCloseHandler",
            "TeamDeletedRefreshLogCleanupHandler", "ConnectionFetchShapeChangedUpdateTriggerHandler",
            "PortfolioDeletedOrphanedFeatureCleanupHandler", "ForecastRoundFinishedRefreshLogHandler",
            "RefreshLogService", "IRefreshLogService",
            "WorkItemTransitioned", "WorkItemBlocked", "WorkItemUnblocked", "WorkItemBecameStale",
            "FeatureBlocked", "FeatureUnblocked", "TeamDataRefreshed", "PortfolioFeaturesRefreshed",
        ];

        private static readonly Dictionary<string, string> Stays = new(StringComparer.Ordinal)
        {
            ["OrphanedFeatureCleanupService"] = "Work Items, once it reads Features through a repository port instead of the database context",
            ["IOrphanedFeatureCleanupService"] = "Work Items, together with its only implementation, once the cleanup reads Features through a repository port",
        };

        private static readonly string[] RefreshLogCandidates =
            ["RefreshLogService", "IRefreshLogService", "OrphanedFeatureCleanupService", "IOrphanedFeatureCleanupService"];

        // Found by name because they live outside the Work Items namespaces today.
        private static readonly string[] MembersOutsideTheWorkItemsNamespaces =
        [
            "WorkItemBlockedTransitionCaptureHandler", "WorkItemBlockedTransitionCloseHandler",
            "FeatureBlockedTransitionCaptureHandler", "FeatureBlockedTransitionCloseHandler",
            "TeamDeletedRefreshLogCleanupHandler", "ConnectionFetchShapeChangedUpdateTriggerHandler",
            .. RefreshLogCandidates,
        ];

        private const string FixHint =
            "Every Work Items type is either moved or kept on purpose: put it on Moves if it goes to Modules/WorkItems, or on Stays with the context that keeps it. A listed type the code no longer holds was renamed or deleted: put its new name on the list, or take it off.";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsMembershipArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheWorkItemsTypesInTheCompiledCode_AreExactlyTheTypesListedToMoveOrToStay()
        {
            var found = WhenTheWorkItemsTypesAreCollectedFrom(ProductionModule.Value);

            ThenTheyMatchTheLists(found);
        }

        [Test]
        public void EveryTypeThatStays_NamesTheContextThatKeepsIt()
        {
            var unowned = Stays.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key).ToList();

            Assert.That(unowned, Is.Empty,
                $"These Stays entries name no owner: {string.Join(", ", unowned)}. A type left behind without an owner is a type nobody will move later. Write the context that keeps it next to its name.");
        }

        [Test]
        public void NoTypeIsListedBothToMoveAndToStay()
        {
            var both = Moves.Intersect(Stays.Keys, StringComparer.Ordinal).ToList();

            Assert.That(both, Is.Empty,
                $"{string.Join(", ", both)} is on Moves and on Stays. A type either moves to Modules/WorkItems or stays where it is: take it off one list.");
        }

        [TestCase("RefreshLogService")]
        [TestCase("IRefreshLogService")]
        [TestCase("OrphanedFeatureCleanupService")]
        [TestCase("IOrphanedFeatureCleanupService")]
        public void ARefreshLogCandidate_MovesExactlyWhenItAndTheImplementationsOfItReachOnlyWhatWorkItemsMayReach(string candidate)
        {
            var definition = GivenTheCandidateIsInTheCompiledCode(ProductionModule.Value, candidate);

            var judgement = WhenTheCandidateIsJudged(ProductionModule.Value, definition);

            ThenItIsListedToMoveExactlyWhenAdmitted(candidate, judgement);
        }

        [Test]
        public void AnInterfaceWithCleanEdges_StaysWhenItsOnlyImplementationStays_AndMovesWhenItsImplementationMoves()
        {
            var portOfAStayingCleanup = GivenTheCandidateIsInTheCompiledCode(TestModule.Value, "IAMadeUpCleanup");
            var portOfAMovingWriter = GivenTheCandidateIsInTheCompiledCode(TestModule.Value, "IAMadeUpLogPort");

            var staying = WhenTheCandidateIsJudged(TestModule.Value, portOfAStayingCleanup);
            var moving = WhenTheCandidateIsJudged(TestModule.Value, portOfAMovingWriter);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(staying.OutsideReach, Is.Empty,
                    "The made-up cleanup port was caught reaching something itself; it reaches nothing, so only its implementation can keep it back.");
                Assert.That(staying.StayingImplementations, Has.Count.EqualTo(1).And.Contains("AMadeUpCleanupReadingTheDatabase"),
                    "An interface whose only implementation reads the database context was not held back by that implementation. A port that moves without its implementation leaves the implementation behind reaching into Modules/WorkItems.");
                Assert.That(staying.Admitted, Is.False,
                    "An interface whose only implementation stays was admitted to move. It stays with its implementation.");
                Assert.That(moving.Admitted, Is.True,
                    $"An interface whose implementation reaches only what Work Items may reach was held back. {moving.Describe("IAMadeUpLogPort")} Only a staying implementation may keep a port back.");
            }
        }

        [Test]
        public void AMadeUpTypeInAWorkItemsNamespace_IsNamedAsUnlisted_AndANamesakeOutsideIsNot()
        {
            var found = WhenTheWorkItemsTypesAreCollectedFrom(ProductionModule.Value)
                .Concat(WhenTheWorkItemsTypesAreCollectedFrom(TestModule.Value));

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("AMadeUpRefreshStepInTheModule"),
                    "A made-up type under Modules.WorkItems was not named as unlisted. The collection must take in the module's namespaces, or a type added there during the move would join Work Items unseen.");
                Assert.That(failure, Does.Contain("AMadeUpBlockedStepInTheOldPlace"),
                    "A made-up type under Services.Implementation.WorkItems was not named as unlisted. The collection must keep taking in the old namespaces until the last type has moved.");
                Assert.That(failure, Does.Not.Contain("ANamesakeOutsideWorkItems"),
                    "A type in Modules.WorkItemsMembershipFixtures was named as Work Items. The namespace pattern must end at a dot or the end of the name, not take in every namespace that starts with the same letters.");
                Assert.That(failure, Does.Not.Contain("ANestedStep"),
                    "A type nested inside a made-up Work Items type was named on its own. Only top-level types belong on the lists; a nested type moves with the type that holds it.");
                Assert.That(failure, Does.Not.Contain("On the list, but no longer"),
                    "Adding made-up types made a listed type look missing. The collection must not lose a real type when another assembly's types are added.");
            }
        }

        [Test]
        public void AListedTypeRenamedInTheCode_IsNamedAsMissing_AndItsNewNameAsUnlisted()
        {
            var found = WhenTheWorkItemsTypesAreCollectedFrom(ProductionModule.Value)
                .Select(name => name == "BlockedItemService" ? "BlockedWorkService" : name);

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("On the list, but no longer offending: BlockedItemService"),
                    "A listed type the code no longer holds was not named. A rename during the move must fail until the list follows it.");
                Assert.That(failure, Does.Contain("Not on the list, and offending: BlockedWorkService"),
                    "A renamed type was not named as unlisted under its new name.");
            }
        }

        private static List<string> WhenTheWorkItemsTypesAreCollectedFrom(ModuleDefinition module)
        {
            var inTheNamespaces = IlDependencies.TypesIn(module, WorkItemsNamespaces).Where(name => !name.Contains('+'));
            var byName = module.Types.Select(type => type.FullName).Where(name => MembersOutsideTheWorkItemsNamespaces.Contains(ShortNameOf(name)));

            return [.. inTheNamespaces.Concat(byName).Select(ShortNameOf).Distinct(StringComparer.Ordinal)];
        }

        private static void ThenTheyMatchTheLists(IEnumerable<string> found)
            => InlineBaseline.AssertMatches(found, [.. Moves, .. Stays.Keys], FixHint);

        private static TypeDefinition GivenTheCandidateIsInTheCompiledCode(ModuleDefinition module, string candidate)
        {
            var definition = module.Types.SingleOrDefault(type => ShortNameOf(type.FullName) == candidate);

            Assert.That(definition, Is.Not.Null,
                $"{candidate} was not found in the compiled code, so whether it may move cannot be judged. Put its new name here and on the lists, or take it off both if it was deleted.");

            return definition!;
        }

        private static CandidateJudgement WhenTheCandidateIsJudged(ModuleDefinition module, TypeDefinition candidate)
        {
            var implementations = candidate.IsInterface
                ? module.Types.Where(type => type.Interfaces.Any(implemented => implemented.InterfaceType.FullName == candidate.FullName)).ToList()
                : [];
            var staying = implementations.Where(implementation => OutsideReachOf(module, implementation).Count > 0).ToList();
            var everyImplementationStays = implementations.Count > 0 && staying.Count == implementations.Count;

            return new CandidateJudgement(
                OutsideReachOf(module, candidate),
                everyImplementationStays ? [.. staying.Select(implementation => implementation.Name)] : []);
        }

        private static List<string> OutsideReachOf(ModuleDefinition module, TypeDefinition candidate)
            => IlDependencies.DependentsIn(
                    module,
                    $"^{Regex.Escape(candidate.Namespace)}$",
                    target => !IsWithinWhatWorkItemsMayReach(target, candidate))
                .Where(dependent => dependent.TypeName == candidate.FullName)
                .SelectMany(dependent => dependent.Targets)
                .ToList();

        private static void ThenItIsListedToMoveExactlyWhenAdmitted(string candidate, CandidateJudgement judgement)
            => Assert.That(Moves.Contains(candidate), Is.EqualTo(judgement.Admitted), judgement.Describe(candidate));

        private static bool IsWithinWhatWorkItemsMayReach(TypeReference target, TypeDefinition candidate)
        {
            var outermost = OutermostOf(target);

            return target is GenericParameter
                || outermost.FullName == candidate.FullName
                || candidate.Interfaces.Any(implemented => implemented.InterfaceType.FullName == outermost.FullName)
                || IsIn(outermost.Namespace, @"^System(\.|$)")
                || IsIn(outermost.Namespace, @"^Microsoft\.Extensions\.Logging(\.|$)")
                || IsIn(outermost.Namespace, @"^Lighthouse\.Backend\.Models(\.|$)")
                || IsIn(outermost.Namespace, @"^Lighthouse\.Backend\.Services\.Interfaces\.Repositories(\.|$)")
                || outermost.FullName == "Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService"
                || IsIn(outermost.Namespace, WorkItemsNamespaces)
                || MembersOutsideTheWorkItemsNamespaces.Contains(outermost.Name);
        }

        private static TypeReference OutermostOf(TypeReference type)
            => type.DeclaringType is null ? type : OutermostOf(type.DeclaringType);

        private static bool IsIn(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);

        private sealed record CandidateJudgement(IReadOnlyList<string> OutsideReach, IReadOnlyList<string> StayingImplementations)
        {
            public bool Admitted => OutsideReach.Count == 0 && StayingImplementations.Count == 0;

            public string Describe(string candidate)
            {
                if (OutsideReach.Count > 0)
                {
                    return $"{candidate} reaches {string.Join(", ", OutsideReach)}, which Work Items may not reach from its module, so it cannot move yet: take it off Moves and put it on Stays with its owner. The compiled code does not show types named only in a catch clause, an event or a named attribute argument, so a reach of that kind is not judged here.";
                }

                return StayingImplementations.Count > 0
                    ? $"{candidate} reaches only what Work Items may reach, but its implementation {string.Join(", ", StayingImplementations)} stays, and a port stays with its implementation: take it off Moves and put it on Stays with the same owner."
                    : $"{candidate} and its implementations reach only the kernel, repositories, settings, logging and Work Items, so it may move: put it on Moves and take it off Stays.";
            }
        }

        private static string ShortNameOf(string fullName)
        {
            var name = fullName[(fullName.LastIndexOf('.') + 1)..];
            var arity = name.IndexOf('`');

            return arity < 0 ? name : name[..arity];
        }
    }
}

namespace Lighthouse.Backend.Modules.WorkItems.MembershipFixtures
{
    internal static class AMadeUpRefreshStepInTheModule
    {
        public static int Refreshed(int[] items) => items.Count(item => item > 0);
    }
}

namespace Lighthouse.Backend.Services.Implementation.WorkItems.MembershipFixtures
{
    internal static class AMadeUpBlockedStepInTheOldPlace
    {
        public static ANestedStep Block(int id) => new(id);

        internal sealed record ANestedStep(int Id);
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsMembershipFixtures
{
    internal static class ANamesakeOutsideWorkItems
    {
        public static int Refreshed(int items) => items;
    }
}

namespace Lighthouse.Backend.WorkItemsMembershipFixtures.Cleanup
{
    internal interface IAMadeUpCleanup
    {
        bool CleanUp(int items);
    }

    internal sealed class AMadeUpCleanupReadingTheDatabase(global::Lighthouse.Backend.Data.LighthouseAppContext context) : IAMadeUpCleanup
    {
        public bool CleanUp(int items) => context is not null && items > 0;
    }

    internal interface IAMadeUpLogPort
    {
        int Write(int lines);
    }

    internal sealed class AMadeUpLogWriter : IAMadeUpLogPort
    {
        public int Write(int lines) => lines + 1;
    }
}
