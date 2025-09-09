using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        private readonly IDemoDataService demoDataService;

        public DemoController(IDemoDataService demoDataService)
        {
            this.demoDataService = demoDataService;
        }

        [HttpGet("scenarios")]
        public ActionResult<List<DemoDataScenario>> GetScenarios()
        {
            var scenarios = demoDataService.GetAllScenarios();
            return Ok(scenarios);
        }

        [HttpPost("scenarios/{id}/load")]
        public async Task<ActionResult> LoadScenario(int id)
        {
            var scenarios = demoDataService.GetAllScenarios();

            var scenario = scenarios.SingleOrDefault(scenario => scenario.Id == id);

            if (scenario == null)
            {
                return NotFound();
            }

            await demoDataService.LoadScenarios([scenario]);

            return Ok();
        }

        [HttpPost("scenarios/load-all")]
        [LicenseGuard(RequirePremium = true)]
        public async Task<ActionResult> LoadAll()
        {
            var scenarios = demoDataService.GetAllScenarios();
            await demoDataService.LoadScenarios(scenarios.ToArray());

            return Ok();
        }
    }
}
