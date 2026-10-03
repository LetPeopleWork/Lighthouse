using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    public class RefinementSettingsDto
    {
        public RefinementSettingsDto()
        {
        }

        public RefinementSettingsDto(RefinementSettings settings)
        {
            States = settings.States.Select(state => new RefinementStateSettingDto(state)).ToList();
        }

        public List<RefinementStateSettingDto> States { get; set; } = [];
    }

    public class RefinementStateSettingDto
    {
        public RefinementStateSettingDto()
        {
        }

        public RefinementStateSettingDto(RefinementStateSetting setting)
        {
            State = setting.State;
        }

        public string State { get; set; } = string.Empty;
    }
}
