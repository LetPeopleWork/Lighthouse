using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/blackout-periods")]
    [ApiController]
    public class BlackoutPeriodsController(IBlackoutPeriodService blackoutPeriodService) : ControllerBase
    {
        private readonly IBlackoutPeriodService blackoutPeriodService = blackoutPeriodService ?? throw new ArgumentNullException(nameof(blackoutPeriodService));

        [HttpGet]
        public ActionResult<IEnumerable<BlackoutPeriod>> GetAll()
        {
            var periods = blackoutPeriodService.GetAll();
            return Ok(periods);
        }

        [HttpPost]
        [LicenseGuard(RequirePremium = true)]
        public async Task<IActionResult> Create([FromBody] BlackoutPeriodDto dto)
        {
            try
            {
                var created = await blackoutPeriodService.Create(dto);
                return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [LicenseGuard(RequirePremium = true)]
        public async Task<IActionResult> Update(int id, [FromBody] BlackoutPeriodDto dto)
        {
            try
            {
                var updated = await blackoutPeriodService.Update(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [LicenseGuard(RequirePremium = true)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await blackoutPeriodService.Delete(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
