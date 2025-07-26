using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TerminologyController : ControllerBase
    {
        private readonly ITerminologyService terminologyService;

        public TerminologyController(ITerminologyService terminologyService)
        {
            this.terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));
        }

        [HttpGet("all")]
        public ActionResult<IEnumerable<TerminologyEntry>> GetAllTerminology()
        {
            var terminology = terminologyService.GetAll();
            return Ok(terminology);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateTerminology([FromBody] IEnumerable<TerminologyEntry> terminology)
        {
            if (terminology == null)
            {
                return BadRequest("Terminology data cannot be null");
            }

            await terminologyService.UpdateTerminology(terminology);
            return Ok();
        }
    }
}
