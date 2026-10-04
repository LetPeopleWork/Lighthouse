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
    }
}
