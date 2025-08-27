using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkTrackingSystemConnectionRepository : RepositoryBase<WorkTrackingSystemConnection>
    {
        private readonly ILogger<WorkTrackingSystemConnectionRepository> logger;
        private readonly IWorkTrackingSystemFactory workTrackingSystemFactory;

        public WorkTrackingSystemConnectionRepository(
            LighthouseAppContext context, ILogger<WorkTrackingSystemConnectionRepository> logger, IWorkTrackingSystemFactory workTrackingSystemFactory)
            : base(context, (context) => context.WorkTrackingSystemConnections, logger)
        {
            this.logger = logger;
            this.workTrackingSystemFactory = workTrackingSystemFactory;
            SeedBuiltInConnections();
        }

        public override IEnumerable<WorkTrackingSystemConnection> GetAll()
        {
            return Context.WorkTrackingSystemConnections
                .Include(c => c.Options);
        }

        public override WorkTrackingSystemConnection? GetById(int id)
        {
            return GetAll().SingleOrDefault(t => t.Id == id);
        }

        private void SeedBuiltInConnections()
        {
            var csvConnection = GetByPredicate(c => c.WorkTrackingSystem == WorkTrackingSystems.Csv);
            if (csvConnection == null)
            {
                var csvWorkTrackingConnection = workTrackingSystemFactory.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
                csvWorkTrackingConnection.Name = "CSV";

                Add(csvWorkTrackingConnection);
                SaveSync();
                logger.LogInformation("Created built-in CSV work tracking connection");
            }
        }
    }
}
