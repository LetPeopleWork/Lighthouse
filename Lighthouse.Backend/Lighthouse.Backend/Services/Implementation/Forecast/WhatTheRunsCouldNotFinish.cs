namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// What a forecast's simulated runs left behind, collected as they happen so that the two things worth
    /// saying about them can be said once at the end rather than ten thousand times.
    /// </summary>
    public sealed class WhatTheRunsCouldNotFinish
    {
        private readonly HashSet<string> teamsLeftUnfinished = new(StringComparer.Ordinal);

        public int RunsGivenUpOn { get; private set; }

        public int RunsThatRanOutOfDays { get; private set; }

        public int FirstRunThatRanOutOfDays { get; private set; } = -1;

        public IEnumerable<string> TeamsLeftUnfinished => teamsLeftUnfinished.Order(StringComparer.Ordinal);

        /// <summary>
        /// The workers' accounts added together. Which run is named as the first to pass the ceiling is the
        /// lowest-numbered one across all of them, not whichever worker happened to report first, so what is
        /// logged is the same however the runs were shared out.
        /// </summary>
        public static WhatTheRunsCouldNotFinish AllOf(IEnumerable<WhatTheRunsCouldNotFinish> shares)
        {
            var all = new WhatTheRunsCouldNotFinish();

            foreach (var share in shares)
            {
                all.RunsGivenUpOn += share.RunsGivenUpOn;
                all.RunsThatRanOutOfDays += share.RunsThatRanOutOfDays;
                all.teamsLeftUnfinished.UnionWith(share.teamsLeftUnfinished);

                if (share.FirstRunThatRanOutOfDays >= 0
                    && (all.FirstRunThatRanOutOfDays < 0 || share.FirstRunThatRanOutOfDays < all.FirstRunThatRanOutOfDays))
                {
                    all.FirstRunThatRanOutOfDays = share.FirstRunThatRanOutOfDays;
                }
            }

            return all;
        }

        public void Note(HowTheRunEnded ending, int trial, ForecastRunPlan plan, TrialState state)
        {
            if (ending == HowTheRunEnded.EverythingFinished)
            {
                return;
            }

            if (ending == HowTheRunEnded.RanOutOfDays)
            {
                RunsThatRanOutOfDays++;
                FirstRunThatRanOutOfDays = FirstRunThatRanOutOfDays < 0 ? trial : FirstRunThatRanOutOfDays;
            }
            else
            {
                RunsGivenUpOn++;
            }

            for (var team = 0; team < plan.TeamCount; team++)
            {
                if (state.RemainingOf(team) > 0)
                {
                    teamsLeftUnfinished.Add(plan.TeamAt(team).Name);
                }
            }
        }
    }
}
