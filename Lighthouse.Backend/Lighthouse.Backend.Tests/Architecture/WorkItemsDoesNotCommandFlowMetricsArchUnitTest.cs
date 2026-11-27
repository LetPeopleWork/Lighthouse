using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItemsFixtures.MetricsCommand.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Work Items refreshes a Team and announces it; Flow metrics, downstream, updates the Team's metrics when it
    /// hears the announcement. So no Work Items type may tell Flow metrics to update a Team's metrics: a Work Items
    /// type that reaches the Team metrics service would make Work Items depend on the module that depends on it.
    /// The rule reads the compiled production assembly. The same rule runs on made-up Work Items types: one that
    /// updates the metrics inside an async method must fail and be named, and one that only announces the refresh
    /// must pass. The compiled code cannot show a service looked up by name at run time; none exists.
    /// </summary>
    [TestFixture]
    public class WorkItemsDoesNotCommandFlowMetricsArchUnitTest
    {
        private const string WorkItemsModule = @"^Lighthouse\.Backend\.Modules\.WorkItems(\.|$)";

        private const string MadeUpWorkItemsModule = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.MetricsCommand(\.|$)";

        private const string TheTeamRefresh = "Lighthouse.Backend.Modules.WorkItems.Application.TeamDataService";

        private static readonly string[] TheTeamMetricsUpdate =
        [
            "Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.TeamMetricsService",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsDoesNotCommandFlowMetricsArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void NoWorkItemsType_TellsFlowMetricsToUpdateATeamsMetrics()
        {
            var verdict = TheRuleOver(ProductionModule.Value, WorkItemsModule);

            Assert.That(verdict.Examined, Does.Contain(TheTeamRefresh),
                $"The rule did not find {TheTeamRefresh} among the Work Items types it read. Why it matters: a rule pointed at a " +
                "namespace that no longer holds the Team refresh passes while it guards nothing. How to fix: point the rule at the " +
                "namespace the Work Items module lives in now.");

            verdict.Check();
        }

        [Test]
        public void AMadeUpWorkItemsTypeThatUpdatesTheMetricsInsideAnAsyncMethod_FailsTheRuleAndIsNamed()
        {
            var verdict = TheRuleOver(FixtureModule.Value, MadeUpWorkItemsModule);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "The rule passed although a made-up Work Items type calls the Team metrics update inside an async method. " +
                "Why it matters: the rule would let Work Items command Flow metrics again unseen. How to fix: report every Work " +
                "Items type, async bodies included, that references the Team metrics service.");

            var offender = typeof(ATeamRefreshThatUpdatesTheMetrics).FullName!;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain($"Not on the list, and offending: {offender}. "),
                    $"The failure must name {offender}, and only it, so the reader knows which type to change.");
                Assert.That(failure.Message, Does.Contain($"{offender} reaches {TheTeamMetricsUpdate[0]}"),
                    $"The failure must say that {offender} reaches {TheTeamMetricsUpdate[0]}, so the reader knows which dependency to undo.");
                Assert.That(failure.Message, Does.Not.Contain(typeof(ATeamRefreshThatAnnouncesItself).FullName!),
                    "The made-up type that only announces the refresh does not reach Flow metrics, so the failure must not name it.");
            }
        }

        private static TeamMetricsCommandVerdict TheRuleOver(ModuleDefinition module, string workItems)
            => new(
                IlDependencies.TypesIn(module, workItems),
                IlDependencies.DependentsIn(module, workItems, type => TheTeamMetricsUpdate.Contains(type.FullName, StringComparer.Ordinal)));

        private sealed record TeamMetricsCommandVerdict(IReadOnlyList<string> Examined, IReadOnlyList<IlDependent> Dependents)
        {
            public void Check()
                => InlineBaseline.AssertMatches(
                    Dependents.Select(dependent => dependent.TypeName),
                    [],
                    "No Work Items type may reference Flow metrics' Team metrics service, because Flow metrics depends on Work Items: " +
                    "a Work Items type that tells Flow metrics to update a Team's metrics makes the two modules depend on each other. " +
                    "Announce the refresh with TeamDataRefreshed and let the Flow metrics reaction to it update the metrics." +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, Dependents.Select(dependent => $"{dependent.TypeName} reaches {string.Join(", ", dependent.Targets)}")));
        }
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.MetricsCommand.Application
{
    internal static class ATeamRefreshThatUpdatesTheMetrics
    {
        public static async Task Refresh(global::Lighthouse.Backend.Models.Team team, IServiceProvider services)
        {
            var teamMetricsService = (global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService)services.GetService(typeof(global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService))!;
            await Task.Yield();
            await teamMetricsService.UpdateTeamMetrics(team);
        }
    }

    internal static class ATeamRefreshThatAnnouncesItself
    {
        public static async Task Refresh(global::Lighthouse.Backend.Models.Team team, global::Lighthouse.Backend.Platform.DomainEvents.IDomainEventDispatcher dispatcher)
        {
            await Task.Yield();
            await dispatcher.PublishAsync(new global::Lighthouse.Backend.Modules.WorkItems.Contracts.TeamDataRefreshed(team.Id));
        }
    }
}
