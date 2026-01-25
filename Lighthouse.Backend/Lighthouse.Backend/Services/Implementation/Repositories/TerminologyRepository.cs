using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class TerminologyRepository(LighthouseAppContext context, ILogger<TerminologyRepository> logger)
        : RepositoryBase<TerminologyEntry>(context, lighthouseAppContext => lighthouseAppContext.TerminologyEntries, logger);
}
