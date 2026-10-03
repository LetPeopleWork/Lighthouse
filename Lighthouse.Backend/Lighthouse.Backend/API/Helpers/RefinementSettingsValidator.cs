using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.API.Helpers
{
    public static class RefinementSettingsValidator
    {
        // Checked against the states in the same save, because one save may change the Team's states and its
        // refinement choice together. A mapped name is an entry of those lists in its own right, so it counts.
        public static List<string> ValidateSettings(TeamSettingDto teamSetting)
        {
            if (teamSetting.Refinement is null)
            {
                return [];
            }

            var candidates = new HashSet<string>(
                teamSetting.ToDoStates.Concat(teamSetting.DoingStates).Select(state => state.Trim()),
                StringComparer.OrdinalIgnoreCase);

            return teamSetting.Refinement.States
                .Select(chosen => chosen.State.Trim())
                .Where(state => !candidates.Contains(state))
                .Select(state => $"'{state}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.")
                .ToList();
        }
    }
}
