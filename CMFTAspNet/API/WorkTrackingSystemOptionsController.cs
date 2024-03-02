using CMFTAspNet.Factories;
using CMFTAspNet.Models;
using CMFTAspNet.WorkTracking;
using Microsoft.AspNetCore.Mvc;

namespace CMFTAspNet.API
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
