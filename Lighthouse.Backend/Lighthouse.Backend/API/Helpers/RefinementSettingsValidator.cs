using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.API.Helpers
{
    public static class RefinementSettingsValidator
    {
        // Checked against the states in the same save, because one save may change the Team's states and its
        // refinement choice together. A mapped name is an entry of those lists in its own right, so it counts.
        // Only states added in this save are judged: the settings form re-sends every section on each save, so a
        // state chosen earlier that has since stopped being mapped comes back too, and refusing it would block
        // every later edit to the Team. It is kept and flagged instead.
        public static List<string> ValidateSettings(TeamSettingDto teamSetting, IEnumerable<string> storedStates)
        {
            if (teamSetting.Refinement is null)
            {
                return [];
            }

            var candidates = NormalisedSet(teamSetting.ToDoStates.Concat(teamSetting.DoingStates));
            var alreadyChosen = NormalisedSet(storedStates);

            return teamSetting.Refinement.States
                .Select(chosen => chosen.State.Trim())
                .Where(state => !alreadyChosen.Contains(state) && !candidates.Contains(state))
                .Select(state => $"'{state}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.")
                .ToList();
        }

        private static HashSet<string> NormalisedSet(IEnumerable<string> states)
        {
            return new HashSet<string>(states.Select(state => state.Trim()), StringComparer.OrdinalIgnoreCase);
        }
    }
}
