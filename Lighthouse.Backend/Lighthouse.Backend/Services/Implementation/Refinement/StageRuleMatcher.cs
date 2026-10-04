using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>Which of the given Work Items each of a Team's stage rules matches, judged by the rule engine every other rule uses.</summary>
    public sealed class StageRuleMatcher(
        IRuleEvaluator<WorkItem> ruleEvaluator,
        IRuleFieldProvider<WorkItem> fieldProvider,
        IForecastFilterRuleService forecastFilterRuleService)
    {
        /// <summary>The Team's own stage rules, judged against the fields its tracker still offers.</summary>
        public StageMatches MatchFor(Team team, List<WorkItem> workItems) => Match(StageRulesOf(team), workItems);

        public StageMatches Match(StageRules? rules, List<WorkItem> workItems)
        {
            var ready = RuleWithConditionsOrNull(rules?.Ready);
            var beingRefined = RuleWithConditionsOrNull(rules?.BeingRefined);
            if (ready is null && beingRefined is null)
            {
                return StageMatches.NoStages;
            }

            return new StageMatches(true, MatchedBy(ready, workItems), MatchedBy(beingRefined, workItems));
        }

        // A field that went away reads as empty on every Work Item, so a rule still naming it would match at random.
        private StageRules? StageRulesOf(Team team)
        {
            var stored = team.RefinementSettings?.StageRules;
            return stored is null || team.WorkTrackingSystemConnection is null
                ? stored
                : stored.WithoutFieldsMissingFrom(forecastFilterRuleService.GetSchema(team));
        }

        // A rule saved without a single condition says nothing, so it counts as no rule at all.
        private static WorkItemRuleSet? RuleWithConditionsOrNull(WorkItemRuleSet? rule)
            => rule is { Conditions.Count: > 0 } ? rule : null;

        private HashSet<WorkItem> MatchedBy(WorkItemRuleSet? rule, List<WorkItem> workItems)
            => rule is null
                ? new HashSet<WorkItem>(ReferenceEqualityComparer.Instance)
                : new HashSet<WorkItem>(ruleEvaluator.Match(rule, workItems, fieldProvider), ReferenceEqualityComparer.Instance);
    }

    /// <summary>Whether the Team uses stages at all, and the Work Items its Ready and Being refined rules each match.</summary>
    public sealed record StageMatches(bool StagesConfigured, IReadOnlySet<WorkItem> Ready, IReadOnlySet<WorkItem> BeingRefined)
    {
        public static StageMatches NoStages { get; } = new(false, new HashSet<WorkItem>(), new HashSet<WorkItem>());

        public RefinementStage? StageOf(WorkItem workItem)
            => StagesConfigured ? RefinementResolution.StageOf(Ready.Contains(workItem), BeingRefined.Contains(workItem)) : null;
    }
}
