using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.API.Helpers
{
    public static class RefinementSettingsValidator
    {
        // Checked against the states in the same save, because one save may change the Team's states and its
        // refinement choice together. A mapped name is an entry of those lists in its own right, so it counts.
        // Only states added in this save are judged: a form opened before the admin took a chosen state out of To Do
        // and Doing still sends it, and refusing that would block the save. Such a state is dropped when the save
        // is applied instead.
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
