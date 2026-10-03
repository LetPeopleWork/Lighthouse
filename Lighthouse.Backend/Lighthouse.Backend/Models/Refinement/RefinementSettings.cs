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
    }

    public class RefinementStateSetting
    {
        public string State { get; set; } = string.Empty;

        public RefinementStage Stage { get; set; } = RefinementStage.Waiting;
    }

    /// <summary>Stored by ordinal inside the Team's JSON: append new members, never renumber.</summary>
    public enum RefinementStage
    {
        Waiting = 0,
    }
}
