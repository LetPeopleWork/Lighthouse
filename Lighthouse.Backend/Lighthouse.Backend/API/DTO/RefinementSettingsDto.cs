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
            DiscussWhen = new DiscussionRulesDto(setting.DiscussWhen);
        }

        public int? MinYes { get; set; }

        public int? MinVoters { get; set; }

        /// <summary>Null on a save leaves the discussion rules the Team already has.</summary>
        public DiscussionRulesDto? DiscussWhen { get; set; }

        /// <summary>The readiness a save results in: what it sends, and what is stored for everything it leaves out.</summary>
        public ReadinessSetting AppliedTo(ReadinessSetting stored)
        {
            return new ReadinessSetting
            {
                MinYes = MinYes ?? stored.MinYes,
                MinVoters = MinVoters ?? stored.MinVoters,
                DiscussWhen = DiscussWhen?.AppliedTo(stored.DiscussWhen) ?? stored.DiscussWhen,
            };
        }
    }

    /// <summary>
    /// A threshold sent as null turns its rule off, while a threshold left out of the save keeps the stored
    /// one, so each setter remembers that it was called.
    /// </summary>
    public class DiscussionRulesDto
    {
        private int? no;

        private int? yesIf;

        private bool noSent;

        private bool yesIfSent;

        public DiscussionRulesDto()
        {
        }

        public DiscussionRulesDto(DiscussionRules rules)
        {
            No = rules.No;
            YesIf = rules.YesIf;
        }

        public int? No
        {
            get => no;
            set
            {
                no = value;
                noSent = true;
            }
        }

        public int? YesIf
        {
            get => yesIf;
            set
            {
                yesIf = value;
                yesIfSent = true;
            }
        }

        public DiscussionRules AppliedTo(DiscussionRules stored)
        {
            return new DiscussionRules
            {
                No = noSent ? no : stored.No,
                YesIf = yesIfSent ? yesIf : stored.YesIf,
            };
        }
    }
}
