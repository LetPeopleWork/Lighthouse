using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class StageRuleMatcherTest
    {
        private const string ReadyTag = "ready";

        private const string AnalysingTag = "analysing";

        private readonly StageRuleMatcher subject = new(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider());

        private static readonly WorkItem TaggedReady = Tagged("GR-058", ReadyTag);

        private static readonly WorkItem TaggedAnalysing = Tagged("GR-051", AnalysingTag);

        private static readonly WorkItem TaggedBoth = Tagged("GR-054", ReadyTag, AnalysingTag);

        private static readonly WorkItem Untagged = Tagged("GR-073");

        private static readonly List<WorkItem> AllFour = [TaggedReady, TaggedAnalysing, TaggedBoth, Untagged];

        [TestCaseSource(nameof(RulesThatSetNoStage))]
        public void A_Team_without_a_stage_rule_has_no_stages(StageRules? rules)
        {
            var matches = subject.Match(rules, AllFour);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matches.StagesConfigured, Is.False);
                Assert.That(AllFour.Select(matches.StageOf), Is.All.Null);
            }
        }

        [Test]
        public void Both_rules_give_every_Work_Item_its_stage()
        {
            var matches = subject.Match(new StageRules { Ready = TagsContain(ReadyTag), BeingRefined = TagsContain(AnalysingTag) }, AllFour);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matches.StagesConfigured, Is.True);
                Assert.That(matches.StageOf(TaggedReady), Is.EqualTo(RefinementStage.Ready));
                Assert.That(matches.StageOf(TaggedAnalysing), Is.EqualTo(RefinementStage.BeingRefined));
                Assert.That(matches.StageOf(TaggedBoth), Is.EqualTo(RefinementStage.Ready));
                Assert.That(matches.StageOf(Untagged), Is.EqualTo(RefinementStage.Waiting));
            }
        }

        [Test]
        public void A_Being_refined_rule_alone_leaves_nothing_Ready()
        {
            var matches = subject.Match(new StageRules { Ready = NoConditions(), BeingRefined = TagsContain(AnalysingTag) }, AllFour);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matches.StagesConfigured, Is.True);
                Assert.That(matches.StageOf(TaggedReady), Is.EqualTo(RefinementStage.Waiting));
                Assert.That(matches.StageOf(TaggedBoth), Is.EqualTo(RefinementStage.BeingRefined));
            }
        }

        private static IEnumerable<TestCaseData> RulesThatSetNoStage()
        {
            yield return new TestCaseData((StageRules?)null).SetName("No stage rules at all");
            yield return new TestCaseData(new StageRules()).SetName("Neither rule set");
            yield return new TestCaseData(new StageRules { Ready = NoConditions(), BeingRefined = NoConditions() })
                .SetName("Rules saved without a single condition");
        }

        private static WorkItemRuleSet TagsContain(string tag) => new()
        {
            Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.tags", Operator = "contains", Value = tag }],
        };

        private static WorkItemRuleSet NoConditions() => new();

        private static WorkItem Tagged(string referenceId, params string[] tags)
            => new() { ReferenceId = referenceId, Name = referenceId, State = "Backlog", Order = "1", Tags = [.. tags] };
    }
}
