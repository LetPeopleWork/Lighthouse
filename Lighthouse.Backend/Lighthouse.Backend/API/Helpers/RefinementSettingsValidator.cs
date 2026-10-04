using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.API.Helpers
{
    public static class RefinementSettingsValidator
    {
        private const string DiscussionRuleNeedsAVote = "a discussion rule needs at least 1 vote.";

        public static List<string> ValidateSettings(TeamSettingDto teamSetting, RefinementSettings? stored)
        {
            var refinement = teamSetting.Refinement;
            if (refinement is null)
            {
                return [];
            }

            return
            [
                .. ValidateStates(teamSetting, refinement, stored?.States.Select(chosen => chosen.State) ?? []),
                .. ValidateReadiness(refinement.Readiness, stored?.Readiness ?? new ReadinessSetting()),
                .. ValidateCadence(refinement.Cadence),
            ];
        }

        private static List<string> ValidateCadence(RefinementCadenceDto? cadence)
        {
            if (cadence is null)
            {
                return [];
            }

            var errors = cadence.Weekdays
                .Where(day => !RefinementCadenceDto.IsWeekdayName(day))
                .Select(day => $"'{day}' cannot be a Refinement day: only a weekday such as Monday can be chosen.")
                .ToList();

            if (cadence.IntervalWeeks < 1)
            {
                errors.Add($"'{cadence.IntervalWeeks}' cannot be the weeks between Refinements: at least one week is needed.");
            }

            // Without a starting week, "every second Tuesday" names no Tuesday in particular.
            if (cadence.IntervalWeeks > 1 && cadence.AnchorWeek is null)
            {
                errors.Add($"Refining every {cadence.IntervalWeeks} weeks needs a starting week.");
            }

            return errors;
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

        // A save that leaves readiness out keeps what is stored, so there is nothing to judge. Otherwise the
        // readiness the save would leave behind is judged, so one that sends only some fields cannot break a
        // rule together with the stored values for the rest.
        private static List<string> ValidateReadiness(ReadinessSettingDto? sent, ReadinessSetting stored)
        {
            if (sent is null)
            {
                return [];
            }

            var readiness = sent.AppliedTo(stored);
            var errors = new List<string>();

            if (readiness.MinYes < 1)
            {
                errors.Add($"'{readiness.MinYes}' cannot be the Yes votes readiness needs: at least one Yes is needed.");
            }

            if (readiness.MinVoters < readiness.MinYes)
            {
                errors.Add($"'{readiness.MinVoters}' cannot be the voters readiness needs: never fewer than the {readiness.MinYes} Yes votes.");
            }

            if (readiness.DiscussWhen.No < 1)
            {
                errors.Add($"'{readiness.DiscussWhen.No}' cannot be the No votes that send a Work Item to discussion: {DiscussionRuleNeedsAVote}");
            }

            if (readiness.DiscussWhen.YesIf < 1)
            {
                errors.Add($"'{readiness.DiscussWhen.YesIf}' cannot be the \"Yes, if…\" votes that send a Work Item to discussion: {DiscussionRuleNeedsAVote}");
            }

            return errors;
        }

        private static HashSet<string> NormalisedSet(IEnumerable<string> states)
        {
            return new HashSet<string>(states.Select(state => state.Trim()), StringComparer.OrdinalIgnoreCase);
        }
    }
}
