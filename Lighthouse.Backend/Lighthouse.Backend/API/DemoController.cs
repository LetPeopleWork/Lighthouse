using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        [HttpGet("scenarios")]
        public ActionResult<List<DemoDataScenario>> GetScenarios()
        {
            var scenarios = new List<DemoDataScenario>();

            scenarios.Add(
                new DemoDataScenario
                {
                    Id = 0,
                    Title = "Free Scenario",
                    Description = "Basic Scenario",
                    NumberOfTeams = 1,
                    NumberOfProjects = 1,
                    IsPremium = false,
                }
                );

            scenarios.Add(
                new DemoDataScenario
                {
                    Id = 1,
                    Title = "Premium Scenario",
                    Description = "Advanced Scenario",
                    NumberOfTeams = 3,
                    NumberOfProjects = 2,
                    IsPremium = true,
                }
                );

            return Ok(scenarios);
        }

        [HttpPost("scenarios/{id}/load")]
        public ActionResult LoadScenario(int id)
        {
            return Ok();
        }

        [HttpPost("scenarios/load-all")]
        [LicenseGuard(RequirePremium = true)]
        public ActionResult LoadAll()
        {
            return Ok();
        }
    }
}
