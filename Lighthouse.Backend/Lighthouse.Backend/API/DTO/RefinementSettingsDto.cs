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
            Cadence = settings.Cadence is null ? null : new RefinementCadenceDto(settings.Cadence);
        }

        public List<RefinementStateSettingDto> States { get; set; } = [];

        /// <summary>Null on a save leaves the readiness the Team already has.</summary>
        public ReadinessSettingDto? Readiness { get; set; }

        /// <summary>Null on a save leaves the stage rules the Team already has.</summary>
        public StageRulesDto? StageRules { get; set; }

        /// <summary>Null on a save leaves the cadence the Team already has.</summary>
        public RefinementCadenceDto? Cadence { get; set; }
    }

    public class RefinementCadenceDto
    {
        private static readonly HashSet<string> WeekdayNames = new(Enum.GetNames<DayOfWeek>(), StringComparer.OrdinalIgnoreCase);

        public RefinementCadenceDto()
        {
        }

        public RefinementCadenceDto(RefinementCadence cadence)
        {
            Weekdays = [.. cadence.Weekdays.Select(day => day.ToString())];
            IntervalWeeks = cadence.IntervalWeeks;
            AnchorWeek = cadence.AnchorWeek;
        }

        /// <summary>Names rather than enum values, so a save naming no weekday is refused with the name it sent.</summary>
        public List<string> Weekdays { get; set; } = [];

        public int IntervalWeeks { get; set; } = 1;

        public DateOnly? AnchorWeek { get; set; }

        public static bool IsWeekdayName(string name) => WeekdayNames.Contains(name);

        /// <summary>
        /// The cadence a save results in; a save naming no weekday clears it. A name that is not a weekday is
        /// left out rather than parsed, because a number would otherwise parse into a day that does not exist.
        /// </summary>
        public RefinementCadence? ToCadence()
        {
            var weekdays = Weekdays
                .Where(IsWeekdayName)
                .Select(name => Enum.Parse<DayOfWeek>(name, ignoreCase: true))
                .ToList();

            return weekdays.Count == 0 ? null : RefinementCadence.Of(weekdays, IntervalWeeks, AnchorWeek);
        }
    }

    /// <summary>A rule sent as null removes it, while a rule left out of the save keeps the stored one.</summary>
    public class StageRulesDto
    {
        private SaveField<StageRuleDto?> ready;

        private SaveField<StageRuleDto?> beingRefined;

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
            get => ready.Value;
            set => ready = new(value, Sent: true);
        }

        public StageRuleDto? BeingRefined
        {
            get => beingRefined.Value;
            set => beingRefined = new(value, Sent: true);
        }

        public StageRules AppliedTo(StageRules stored)
        {
            return new StageRules
            {
                Ready = ready.Or(stored.Ready, rule => rule?.ToRuleSet()),
                BeingRefined = beingRefined.Or(stored.BeingRefined, rule => rule?.ToRuleSet()),
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

    /// <summary>A threshold sent as null turns its rule off, while a threshold left out of the save keeps the stored one.</summary>
    public class DiscussionRulesDto
    {
        private SaveField<int?> no;

        private SaveField<int?> yesIf;

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
            get => no.Value;
            set => no = new(value, Sent: true);
        }

        public int? YesIf
        {
            get => yesIf.Value;
            set => yesIf = new(value, Sent: true);
        }

        public DiscussionRules AppliedTo(DiscussionRules stored)
        {
            return new DiscussionRules
            {
                No = no.Or(stored.No),
                YesIf = yesIf.Or(stored.YesIf),
            };
        }
    }
}
