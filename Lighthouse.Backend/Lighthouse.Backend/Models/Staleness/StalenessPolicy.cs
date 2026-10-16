// SCAFFOLD: true
namespace Lighthouse.Backend.Models.Staleness
{
    /// <summary>
    /// What the policy needs to know about one Work Item or Feature, already resolved for the day being
    /// asked about: a past read hands over the state the item held then, not the one it holds today.
    /// </summary>
    public sealed record StalenessCandidate(
        StateCategories StateCategory,
        string StateName,
        DateTime? EnteredStateAt,
        bool IsBlocked,
        DateTime? BlockedSince);

    public sealed class StalenessPolicy
    {
        private const string NotYetImplemented = "Not yet implemented -- RED scaffold";

        private readonly int timeInStateThresholdDays;
        private readonly int blockedThresholdDays;

        private StalenessPolicy(int timeInStateThresholdDays, int blockedThresholdDays)
        {
            this.timeInStateThresholdDays = timeInStateThresholdDays;
            this.blockedThresholdDays = blockedThresholdDays;
        }

        public static StalenessPolicy For(WorkTrackingSystemOptionsOwner owner)
            => new(owner.StalenessThresholdDays, owner.BlockedStalenessThresholdDays);

        public StalenessVerdict Evaluate(StalenessCandidate candidate, TimeZoneInfo zone, DateOnly asOfDay)
            => throw new InvalidOperationException(
                $"{NotYetImplemented} (thresholds {timeInStateThresholdDays}/{blockedThresholdDays})");
    }
}
