using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/recurring-blackout-rules")]
    [Route("api/latest/recurring-blackout-rules")]
    [ApiController]
    public class RecurringBlackoutRulesController(IRecurringBlackoutRuleService recurringBlackoutRuleService) : ControllerBase
    {
        private readonly IRecurringBlackoutRuleService recurringBlackoutRuleService = recurringBlackoutRuleService ?? throw new ArgumentNullException(nameof(recurringBlackoutRuleService));

        [HttpGet]
        public ActionResult<IEnumerable<RecurringBlackoutRuleDto>> GetAll()
        {
            var rules = recurringBlackoutRuleService.GetAll();
            return Ok(rules);
        }

        [HttpPost]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<IActionResult> Create([FromBody] RecurringBlackoutRuleDto dto)
        {
            var created = await recurringBlackoutRuleService.Create(dto);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<IActionResult> Update(int id, [FromBody] RecurringBlackoutRuleDto dto)
        {
            try
            {
                var updated = await recurringBlackoutRuleService.Update(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await recurringBlackoutRuleService.Delete(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
