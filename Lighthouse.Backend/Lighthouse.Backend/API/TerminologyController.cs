using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class TerminologyController(ITerminologyService terminologyService) : ControllerBase
    {
        private readonly ITerminologyService terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));

        [SignedInOnly("The terms every screen renders, the same for everybody and not specific to any Team or Portfolio.")]
        [HttpGet("all")]
        public ActionResult<IEnumerable<TerminologyEntry>> GetAllTerminology()
        {
            var terminology = terminologyService.GetAll();
            return Ok(terminology);
        }

        [HttpPut]
        [LicenseGuard(RequirePremium =  true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<ActionResult> UpdateTerminology([FromBody] IEnumerable<TerminologyEntry> terminology)
        {
            await terminologyService.UpdateTerminology(terminology);
            return Ok();
        }
    }
}
