using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public sealed class SizingLogRepository(LighthouseAppContext context) : ISizingLogRepository
    {
        public void Append(SizingLogEntry entry)
        {
            context.SizingLogEntries.Add(entry);
            context.SaveChanges();
        }

        public IEnumerable<SizingLogEntry> ReadForTeam(int teamId, IReadOnlyCollection<string> workItemReferences)
            => [.. context.SizingLogEntries
                .AsNoTracking()
                .Where(entry => entry.TeamId == teamId && workItemReferences.Contains(entry.WorkItemReferenceId))
                .OrderBy(entry => entry.Id)];
    }
}
