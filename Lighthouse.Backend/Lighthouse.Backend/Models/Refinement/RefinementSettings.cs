using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>
    /// How a Team runs its refinement, stored as one JSON value on the Team. Every member carries its
    /// default in an initialiser, so a member added later reads back as that default from JSON written
    /// before it existed, and no migration is needed for it.
    /// </summary>
    public class RefinementSettings
    {
        public List<RefinementStateSetting> States { get; set; } = [];

        public ReadinessSetting Readiness { get; set; } = new();

        public StageRules StageRules { get; set; } = new();

        /// <summary>When the Team refines; null for a Team that has not said.</summary>
        public RefinementCadence? Cadence { get; set; }

        public RefinementBand Band { get; set; } = new();
    }

    /// <summary>
    /// The two likelihoods the range to refine towards is read at: the low end is what the Team pulls with
    /// <see cref="LowPercentile"/> likelihood, the high end what it pulls with <see cref="HighPercentile"/>.
    /// </summary>
    public class RefinementBand
    {
        public const int DefaultLowPercentile = 50;

        public const int DefaultHighPercentile = 85;

        public int LowPercentile { get; set; } = DefaultLowPercentile;

        public int HighPercentile { get; set; } = DefaultHighPercentile;
    }

    /// <summary>
    /// The weekdays a Team refines on, every so many weeks counted from the week of <see cref="AnchorWeek"/>.
    /// Built through <see cref="Of"/> so one cadence always has one stored form: each weekday once, Monday
    /// first, and the starting week kept as its Monday whichever of its days was named. Every week counts the
    /// same from any week, so a cadence that does not skip weeks keeps no starting week at all; one kept there
    /// would only hold back the first Refinement.
    /// </summary>
    public class RefinementCadence
    {
        /// <summary>The next Refinement is looked for a year ahead, so a cadence repeating less often would never name one.</summary>
        public const int MaxIntervalWeeks = 52;

        public List<DayOfWeek> Weekdays { get; set; } = [];

        public int IntervalWeeks { get; set; } = 1;

        public DateOnly? AnchorWeek { get; set; }

        public static RefinementCadence Of(IEnumerable<DayOfWeek> weekdays, int intervalWeeks, DateOnly? anchorWeek)
        {
            return new RefinementCadence
            {
                Weekdays = [.. weekdays.Distinct().OrderBy(DaysFromMonday)],
                IntervalWeeks = intervalWeeks,
                AnchorWeek = intervalWeeks == 1 ? null : anchorWeek?.AddDays(-DaysFromMonday(anchorWeek.Value.DayOfWeek)),
            };
        }

        private static int DaysFromMonday(DayOfWeek day) => ((int)day + 6) % 7;
    }

    /// <summary>
    /// Which Work Items in refinement are Ready and which are being refined, each by an optional rule;
    /// whatever no rule matches is Waiting. A Team that sets neither rule has no stages at all.
    /// </summary>
    public class StageRules
    {
        public WorkItemRuleSet? Ready { get; set; }

        public WorkItemRuleSet? BeingRefined { get; set; }

        /// <summary>These rules without the conditions on fields the schema no longer offers.</summary>
        public StageRules WithoutFieldsMissingFrom(WorkItemRuleSchema schema)
        {
            return new StageRules
            {
                Ready = Healed(Ready, schema),
                BeingRefined = Healed(BeingRefined, schema),
            };
        }

        /// <summary>A rule without a single condition says nothing, so it counts as no rule at all.</summary>
        public static WorkItemRuleSet? RuleWithConditionsOrNull(WorkItemRuleSet? rule)
            => rule is { Conditions.Count: > 0 } ? rule : null;

        private static WorkItemRuleSet? Healed(WorkItemRuleSet? rule, WorkItemRuleSchema schema)
            => rule is null ? null : RuleWithConditionsOrNull(AdditionalFieldRuleHealing.WithoutFieldsMissingFrom(rule, schema));
    }

    public class RefinementStateSetting
    {
        public string State { get; set; } = string.Empty;

        public RefinementStage Stage { get; set; } = RefinementStage.Waiting;
    }

    /// <summary>How many Yes votes from how many voters make a Work Item Ready, and what sends it to discussion instead.</summary>
    public class ReadinessSetting
    {
        public const int DefaultMinYes = 3;

        public const int DefaultMinVoters = 3;

        public int MinYes { get; set; } = DefaultMinYes;

        public int MinVoters { get; set; } = DefaultMinVoters;

        public DiscussionRules DiscussWhen { get; set; } = new();
    }

    /// <summary>
    /// Two independent rules, either of which sends a Work Item to discussion: enough No votes, or enough
    /// "Yes, if…" votes. A null threshold turns that rule off.
    /// </summary>
    public class DiscussionRules
    {
        public const int DefaultNo = 1;

        public const int DefaultYesIf = 2;

        public int? No { get; set; } = DefaultNo;

        public int? YesIf { get; set; } = DefaultYesIf;
    }

    /// <summary>Stored by ordinal inside the Team's JSON: append new members, never renumber.</summary>
    public enum RefinementStage
    {
        Waiting = 0,
        BeingRefined = 1,
        Ready = 2,
    }
}
