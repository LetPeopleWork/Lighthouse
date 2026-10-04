using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public static class StageRuleHealing
    {
        /// <summary>
        /// The Team's stage rules without the conditions on fields its tracker no longer offers. A Team without a
        /// tracker connection has no fields to judge them against, so its rules come back as stored.
        /// </summary>
        public static StageRules? HealedStageRulesOf(Team team, IForecastFilterRuleService forecastFilterRuleService)
        {
            var stored = team.RefinementSettings?.StageRules;
            return stored is null || team.WorkTrackingSystemConnection is null
                ? stored
                : stored.WithoutFieldsMissingFrom(forecastFilterRuleService.GetSchema(team));
        }
    }
}
