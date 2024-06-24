using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.WorkTracking;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkTrackingSystemOptionsController : ControllerBase
    {
        private readonly IWorkTrackingOptionsFactory workTrackingOptionsFactory;

        public WorkTrackingSystemOptionsController(IWorkTrackingOptionsFactory workTrackingOptionsFactory)
        {
            this.workTrackingOptionsFactory = workTrackingOptionsFactory;
        }

        [HttpGet("Team")]
        public IEnumerable<WorkTrackingSystemOption<Team>> GetTeamWorktrackingOptions(WorkTrackingSystems selectedSystem)
        {
            return workTrackingOptionsFactory.CreateOptionsForWorkTrackingSystem<Team>(selectedSystem);
        }

        [HttpGet("Project")]
        public IEnumerable<WorkTrackingSystemOption<Project>> GetProjectWorktrackingOptions(WorkTrackingSystems selectedSystem)
        {
            return workTrackingOptionsFactory.CreateOptionsForWorkTrackingSystem<Project>(selectedSystem);
        }
    }
}
