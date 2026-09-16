using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.ConnectionHealth
{
    /// <summary>
    /// How old an answer about a connection may be, and how often to go looking for one that is too old.
    /// Two questions, one arithmetic, in one place - because the two have to stay in step and the way
    /// they go wrong is quiet. Look less often than answers expire and connections sit stale without
    /// anything being stale for long enough to notice; derive them separately and nothing says so.
    /// </summary>
    public sealed class ConnectionHealthCadence(IAppSettingService appSettingService)
    {
        /// <summary>
        /// What the word "healthy" promises, since it means "observed within this". Derived from the
        /// refresh intervals rather than configured, because its one job is to be longer than the gap at
        /// which a successful refresh already records health. Set it below that and a connection
        /// something refreshes goes stale between refreshes and gets asked anyway - the recurring
        /// outbound call per connection this whole shape exists to avoid. Derived, it cannot be set
        /// wrongly.
        ///
        /// Twice rather than once because the real gap is the interval plus however long the refresh
        /// took, and the update queue is a single lane with nothing bounding a refresh's wall-time.
        /// </summary>
        public TimeSpan HowFreshAnAnswerMustBe => TimeSpan.FromMinutes(2 * Math.Max(
            appSettingService.GetTeamDataRefreshSettings().Interval,
            appSettingService.GetFeatureRefreshSettings().Interval));

        /// <summary>
        /// A quarter of that, so a connection is asked within a quarter of the threshold of falling out
        /// of date. A pass that finds nothing stale costs one indexed query and no outbound call, which
        /// is what makes looking often affordable. The floor keeps a minute between passes however short
        /// the refresh intervals are set.
        /// </summary>
        public TimeSpan HowOftenToLook =>
            TimeSpan.FromMinutes(Math.Max(HowFreshAnAnswerMustBe.TotalMinutes / 4, 1));
    }
}
