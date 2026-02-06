using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TerminologyController(ITerminologyService terminologyService) : ControllerBase
    {
        private readonly ITerminologyService terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));

        [HttpGet("all")]
        public ActionResult<IEnumerable<TerminologyEntry>> GetAllTerminology()
        {
            var terminology = terminologyService.GetAll();
            return Ok(terminology);
        }

        [HttpPut]
        [LicenseGuard(RequirePremium =  true)]
        public async Task<ActionResult> UpdateTerminology([FromBody] IEnumerable<TerminologyEntry> terminology)
        {
            await terminologyService.UpdateTerminology(terminology);
            return Ok();
        }
    }
}
