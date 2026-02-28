using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkTrackingSystemConnectionRepository(
        LighthouseAppContext context,
        ILogger<WorkTrackingSystemConnectionRepository> logger)
        : RepositoryBase<WorkTrackingSystemConnection>(context, (lighthouseAppContext) => lighthouseAppContext.WorkTrackingSystemConnections,
            logger)
    {
        public override IEnumerable<WorkTrackingSystemConnection> GetAll()
        {
            return Context.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .Include(c => c.AdditionalFieldDefinitions)
                .Include(c => c.WriteBackMappingDefinitions);
        }

        public override WorkTrackingSystemConnection? GetById(int id)
        {
            return GetAll().SingleOrDefault(t => t.Id == id);
        }
    }
}
