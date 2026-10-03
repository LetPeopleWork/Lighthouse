using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    public class RefinementSettingsDto
    {
        public RefinementSettingsDto()
        {
        }

        public RefinementSettingsDto(RefinementSettings settings, IWorkItemQueryOwner owner)
        {
            var mappedStates = new HashSet<string>(
                owner.ToDoStates.Concat(owner.DoingStates).Select(state => state.Trim()),
                StringComparer.OrdinalIgnoreCase);

            States = settings.States
                .Select(state => new RefinementStateSettingDto(state, mappedStates.Contains(state.State.Trim())))
                .ToList();
        }

        public List<RefinementStateSettingDto> States { get; set; } = [];
    }

    public class RefinementStateSettingDto
    {
        public RefinementStateSettingDto()
        {
        }

        public RefinementStateSettingDto(RefinementStateSetting setting, bool isMapped)
        {
            State = setting.State;
            IsMapped = isMapped;
        }

        public string State { get; set; } = string.Empty;

        // Worked out from the Team's current To Do and Doing states each time it is read, so it has no setter:
        // a value a client sends back is never read.
        public bool IsMapped { get; }
    }
}
