using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.API.Helpers
{
    public static class RefinementSettingsValidator
    {
        public static List<string> ValidateSettings(TeamSettingDto teamSetting, IEnumerable<string> storedStates)
        {
            var refinement = teamSetting.Refinement;
            if (refinement is null)
            {
                return [];
            }

            return
            [
                .. ValidateStates(teamSetting, refinement, storedStates),
                .. ValidateReadiness(refinement.Readiness),
            ];
        }

        // Checked against the states in the same save, because one save may change the Team's states and its
        // refinement choice together. A mapped name is an entry of those lists in its own right, so it counts.
        // Only states added in this save are judged: a form opened before the admin took a chosen state out of To Do
        // and Doing still sends it, and refusing that would block the save. Such a state is dropped when the save
        // is applied instead.
        private static List<string> ValidateStates(TeamSettingDto teamSetting, RefinementSettingsDto refinement, IEnumerable<string> storedStates)
        {
            var candidates = NormalisedSet(teamSetting.ToDoStates.Concat(teamSetting.DoingStates));
            var alreadyChosen = NormalisedSet(storedStates);

            return refinement.States
                .Select(chosen => chosen.State.Trim())
                .Where(state => !alreadyChosen.Contains(state) && !candidates.Contains(state))
                .Select(state => $"'{state}' cannot be a refinement state: only the Team's To Do and Doing states can be chosen.")
                .ToList();
        }

        // A save that leaves readiness out keeps what is stored, so there is nothing to judge.
        private static List<string> ValidateReadiness(ReadinessSettingDto? readiness)
        {
            var errors = new List<string>();
            if (readiness is null)
            {
                return errors;
            }

            if (readiness.MinYes < 1)
            {
                errors.Add($"'{readiness.MinYes}' cannot be the Yes votes readiness needs: at least one Yes is needed.");
            }

            if (readiness.MinVoters < readiness.MinYes)
            {
                errors.Add($"'{readiness.MinVoters}' cannot be the voters readiness needs: never fewer than the {readiness.MinYes} Yes votes.");
            }

            if (readiness.Veto?.Threshold < 1)
            {
                errors.Add($"'{readiness.Veto.Threshold}' cannot be the votes a veto needs: at least one vote is needed.");
            }

            return errors;
        }

        private static HashSet<string> NormalisedSet(IEnumerable<string> states)
        {
            return new HashSet<string>(states.Select(state => state.Trim()), StringComparer.OrdinalIgnoreCase);
        }
    }
}
