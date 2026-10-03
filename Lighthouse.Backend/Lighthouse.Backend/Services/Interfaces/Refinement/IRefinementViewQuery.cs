using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    public interface IRefinementViewQuery
    {
        /// <returns>Null when there is no such Team.</returns>
        RefinementView? ForTeam(int teamId);
    }

    public sealed record RefinementView(bool RefinementConfigured, List<WorkItem> WorkItems);
}
