using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>The Work Items a Team holds in its refinement states, in the tracker's backlog order.</summary>
    public sealed class RefinementList(IWorkItemRepository workItemRepository)
    {
        // Trackers rank with numbers or with text; Features are put in backlog order the same way, so the
        // two lists never disagree about which item comes first.
        private static readonly Comparer<string> BacklogRank = Comparer<string>.Create(FeatureComparer.CompareOrderValues);

        public List<WorkItem> For(Team team)
        {
            var states = StatesHoldingRefinementWork(team);
            if (states.Count == 0)
            {
                return [];
            }

            // The database compares text by case where the rest of the product does not, so the state is
            // matched here rather than in the query.
            return [.. workItemRepository
                .GetAllByPredicate(item => item.TeamId == team.Id)
                .AsEnumerable()
                .Where(item => states.Contains(item.State))
                .OrderBy(item => item.Order, BacklogRank)
                .ThenBy(item => item.ReferenceId, StringComparer.Ordinal)];
        }

        // A Work Item may be held under a mapping's name or under one of the tracker's states the mapping
        // gathers, so both are looked for.
        private static HashSet<string> StatesHoldingRefinementWork(Team team)
        {
            var chosen = team.RefinementSettings?.States.Select(setting => setting.State).ToList() ?? [];

            return new HashSet<string>(chosen.Concat(team.GetRawStatesForCategory(chosen)), StringComparer.OrdinalIgnoreCase);
        }
    }
}
