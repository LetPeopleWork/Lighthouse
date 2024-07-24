using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkTrackingSystemConnectionRepository : RepositoryBase<WorkTrackingSystemConnection>
    {
        private readonly ICryptoService cryptoService;

        public WorkTrackingSystemConnectionRepository(LighthouseAppContext context, ICryptoService cryptoService, ILogger<WorkTrackingSystemConnectionRepository> logger) : base(context, (context) => context.WorkTrackingSystemConnections, logger)
        {
            this.cryptoService = cryptoService;
        }

        public override IEnumerable<WorkTrackingSystemConnection> GetAll()
        {
            var connections = Context.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .ToList();

            foreach (var option in connections.SelectMany(c => c.Options).Where(o => o.IsSecret))
            {
                option.Value = cryptoService.Decrypt(option.Value);
            }

            return connections;
        }

        public override WorkTrackingSystemConnection? GetById(int id)
        {
            return GetAll().SingleOrDefault(t => t.Id == id);
        }
    }
}
