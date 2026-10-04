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
            States = settings.States
                .Select(state => new RefinementStateSettingDto(state))
                .ToList();
            Readiness = new ReadinessSettingDto(settings.Readiness);
        }

        public List<RefinementStateSettingDto> States { get; set; } = [];

        /// <summary>Null on a save leaves the readiness the Team already has.</summary>
        public ReadinessSettingDto? Readiness { get; set; }
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

    public class ReadinessSettingDto
    {
        public ReadinessSettingDto()
        {
        }

        public ReadinessSettingDto(ReadinessSetting setting)
        {
            MinYes = setting.MinYes;
            MinVoters = setting.MinVoters;
            Veto = setting.Veto is null ? null : new VetoSettingDto(setting.Veto);
        }

        public int? MinYes { get; set; }

        public int? MinVoters { get; set; }

        /// <summary>Null means no veto: only the Yes votes and voters decide.</summary>
        public VetoSettingDto? Veto { get; set; }
    }

    public class VetoSettingDto
    {
        public VetoSettingDto()
        {
        }

        public VetoSettingDto(VetoSetting setting)
        {
            Threshold = setting.Threshold;
            Counts = setting.Counts;
        }

        public int? Threshold { get; set; }

        public VetoCounts? Counts { get; set; }
    }
}
