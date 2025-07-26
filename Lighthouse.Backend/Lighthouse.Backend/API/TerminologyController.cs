using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TerminologyController : ControllerBase
    {
        private readonly ITerminologyService _terminologyService;

        public TerminologyController(ITerminologyService terminologyService)
        {
            _terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));
        }

        [HttpGet]
        public ActionResult<TerminologyDto> GetTerminology()
        {
            var terminology = _terminologyService.GetTerminology();
            return Ok(terminology);
        }
    }
}
