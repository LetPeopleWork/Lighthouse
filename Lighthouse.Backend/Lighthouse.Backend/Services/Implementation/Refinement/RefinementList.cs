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

            return [.. workItemRepository
                .GetAllByPredicate(item => item.TeamId == team.Id && states.Contains(item.State))
                .AsEnumerable()
                .OrderBy(item => item.Order, BacklogRank)
                .ThenBy(item => item.ReferenceId, StringComparer.Ordinal)];
        }

        // A chosen state the Team no longer maps is left out: the settings already warn that its Work Items
        // cannot appear. A Work Item may be held under a mapping's name or under one of the tracker's states
        // the mapping gathers, so both are looked for.
        private static List<string> StatesHoldingRefinementWork(Team team)
        {
            var mappedStates = new HashSet<string>(
                team.ToDoStates.Concat(team.DoingStates).Select(state => state.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var chosen = (team.RefinementSettings?.States ?? [])
                .Select(setting => setting.State.Trim())
                .Where(mappedStates.Contains)
                .ToList();

            return [.. chosen.Union(team.GetRawStatesForCategory(chosen))];
        }
    }
}
