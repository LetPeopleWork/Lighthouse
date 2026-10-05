using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    public interface IRefinementViewQuery
    {
        /// <param name="presentedVoterKey">The key the reader's browser keeps, used only to mark the reader's own votes.</param>
        /// <returns>Null when there is no such Team.</returns>
        RefinementView? ForTeam(int teamId, string? presentedVoterKey);
    }

    /// <param name="StagesConfigured">Whether the Team sets any stage rule; without one no row has a stage.</param>
    /// <param name="Calendar">When the Team next refines, and whether it does today; no date without a cadence.</param>
    public sealed record RefinementView(
        bool RefinementConfigured,
        List<RefinementRow> WorkItems,
        Yardstick Yardstick,
        VoterIdentityKind VoterIdentity,
        RefinementNeed Need,
        bool StagesConfigured = false,
        RefinementCalendarFacts? Calendar = null)
    {
        public RefinementCalendarFacts CalendarFacts => Calendar ?? RefinementCalendarFacts.None;

        public int ReadyByVotesCount => RefinementResolution.ReadyByVotesCountOf(WorkItems);

        public ReadySource ReadySource => RefinementResolution.ReadySourceOf(StagesConfigured);

        public int ReadyCount => RefinementResolution.ReadyCountOf(StagesConfigured, WorkItems);
    }

    /// <param name="Stage">The stage the Team's rules give the row; null when the Team sets no stage rule.</param>
    /// <param name="SignalsDisagree">Whether the stage and the votes cast differ on the row being Ready.</param>
    public sealed record RefinementRow(WorkItem WorkItem, RowVotes Votes, RowStanding Standing, RefinementStage? Stage = null, bool SignalsDisagree = false);

    /// <summary>When the Team next refines, and how its ready Work Items compare with what it is likely to pull until then.</summary>
    public sealed record RefinementOutlook(RefinementCalendarFacts Calendar, RefinementNeed Need);

    /// <param name="Verdict">Null exactly when <paramref name="UnavailableReason"/> says why there is no range.</param>
    public sealed record RefinementNeed(RefinementVerdict? Verdict, NeedUnavailableReason? UnavailableReason, NeedRange? Range)
    {
        public static RefinementNeed Unavailable(NeedUnavailableReason reason) => new(null, reason, null);
    }

    /// <summary>
    /// How many Work Items the Team is likely to pull over <paramref name="HorizonWorkingDays"/>, read at the two
    /// likelihoods of its band. The high end is stated as forecast, never cut down to the Work Items listed.
    /// </summary>
    public sealed record NeedRange(int Low, int High, int LowPercentile, int HighPercentile, int HorizonWorkingDays);

    /// <summary>Which signal the tab's ready count follows.</summary>
    public enum ReadySource
    {
        Votes = 0,
        Stages = 1,
    }

    /// <summary>What the votes make of a row, and how many more votes it needs when that is what stands in the way.</summary>
    public sealed record RowStanding(RowReadiness Readiness, int? MissingVotes)
    {
        public static RowStanding Ready { get; } = new(RowReadiness.Ready, null);
    }

    /// <summary>How many people currently have a vote on the row, the reader's own vote, if any, and how the votes split.</summary>
    public sealed record RowVotes(int VoteCount, SizingAnswer? MyVote, VoteSplit Split)
    {
        public static RowVotes None { get; } = new(0, null, VoteSplit.None);
    }

    /// <summary>How many current votes say each answer.</summary>
    public sealed record VoteSplit(int Yes, int YesBut, int No)
    {
        public static VoteSplit None { get; } = new(0, 0, 0);
    }

    /// <summary>The number every voter answers against: so many days, with so much probability.</summary>
    public sealed record Yardstick(YardstickSource Source, int? Days, int? Probability)
    {
        public static Yardstick None { get; } = new(YardstickSource.Unavailable, null, null);
    }

    /// <summary>How a voter is known on this instance: by their account, or by a name they declare.</summary>
    public enum VoterIdentityKind
    {
        Account = 0,
        SelfDeclared = 1,
    }
}
