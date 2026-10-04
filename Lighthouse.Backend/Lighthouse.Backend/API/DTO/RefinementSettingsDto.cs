using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Models.WorkItemRules;

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
            StageRules = new StageRulesDto(settings.StageRules);
        }

        public List<RefinementStateSettingDto> States { get; set; } = [];

        /// <summary>Null on a save leaves the readiness the Team already has.</summary>
        public ReadinessSettingDto? Readiness { get; set; }

        /// <summary>Null on a save leaves the stage rules the Team already has.</summary>
        public StageRulesDto? StageRules { get; set; }
    }

    /// <summary>
    /// A rule sent as null removes it, while a rule left out of the save keeps the stored one, so each
    /// setter remembers that it was called.
    /// </summary>
    public class StageRulesDto
    {
        private StageRuleDto? ready;

        private StageRuleDto? beingRefined;

        private bool readySent;

        private bool beingRefinedSent;

        public StageRulesDto()
        {
        }

        public StageRulesDto(StageRules rules)
        {
            Ready = rules.Ready is null ? null : new StageRuleDto(rules.Ready);
            BeingRefined = rules.BeingRefined is null ? null : new StageRuleDto(rules.BeingRefined);
        }

        public StageRuleDto? Ready
        {
            get => ready;
            set
            {
                ready = value;
                readySent = true;
            }
        }

        public StageRuleDto? BeingRefined
        {
            get => beingRefined;
            set
            {
                beingRefined = value;
                beingRefinedSent = true;
            }
        }

        public StageRules AppliedTo(StageRules stored)
        {
            return new StageRules
            {
                Ready = readySent ? ready?.ToRuleSet() : stored.Ready,
                BeingRefined = beingRefinedSent ? beingRefined?.ToRuleSet() : stored.BeingRefined,
            };
        }
    }

    public class StageRuleDto
    {
        public StageRuleDto()
        {
        }

        public StageRuleDto(WorkItemRuleSet ruleSet)
        {
            Version = ruleSet.Version;
            Mode = ruleSet.Mode;
            Conditions = ruleSet.Conditions;
        }

        public int? Version { get; set; }

        public string? Mode { get; set; }

        public List<WorkItemRuleCondition> Conditions { get; set; } = [];

        public WorkItemRuleSet ToRuleSet()
        {
            return new WorkItemRuleSet
            {
                Version = Version ?? WorkItemRuleSet.SchemaVersion,
                Mode = Mode ?? WorkItemRuleSet.ModeAnd,
                Conditions = Conditions,
            };
        }
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
