using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Moq;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    [TestFixture]
    public class RuleSetValidationTest
    {
        [Test]
        public void When_both_stage_rules_are_invalid_the_Ready_rule_is_named()
        {
            var forecastFilterRuleService = new Mock<IForecastFilterRuleService>();
            forecastFilterRuleService
                .Setup(service => service.ValidateRuleSet(It.IsAny<WorkItemRuleSet>(), It.IsAny<Team>()))
                .Returns(false);
            var stageRules = new StageRulesDto { Ready = RuleOn("unknown.ready"), BeingRefined = RuleOn("unknown.refining") };

            var error = RuleSetValidation.ValidateStageRules(stageRules, new Team(), forecastFilterRuleService.Object);

            Assert.That(error, Does.StartWith("'Ready when' rule is invalid"));
        }

        [Test]
        public void An_invalid_Being_refined_rule_is_named_when_the_Ready_rule_is_fine()
        {
            var forecastFilterRuleService = new Mock<IForecastFilterRuleService>();
            forecastFilterRuleService
                .Setup(service => service.ValidateRuleSet(It.IsAny<WorkItemRuleSet>(), It.IsAny<Team>()))
                .Returns<WorkItemRuleSet, Team>((ruleSet, _) => ruleSet.Conditions[0].FieldKey == "ready");
            var stageRules = new StageRulesDto { Ready = RuleOn("ready"), BeingRefined = RuleOn("unknown.refining") };

            var error = RuleSetValidation.ValidateStageRules(stageRules, new Team(), forecastFilterRuleService.Object);

            Assert.That(error, Does.StartWith("'Being refined when' rule is invalid"));
        }

        private static StageRuleDto RuleOn(string fieldKey)
        {
            return new StageRuleDto
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = fieldKey, Operator = "equals", Value = "x" }],
            };
        }
    }
}
