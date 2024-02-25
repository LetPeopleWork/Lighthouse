using CMFTAspNet.Factories;
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

        public IEnumerable<WorkTrackingSystemOption> Get(WorkTrackingSystems selectedSystem)
        {
            var options = workTrackingOptionsFactory.CreateOptionsForWorkTrackingSystem(selectedSystem);

            return options;
        }
    }
}
