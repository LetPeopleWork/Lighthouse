namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// What a forecast's simulated runs left behind, collected as they happen so that the two things worth
    /// saying about them can be said once at the end rather than ten thousand times.
    /// </summary>
    public sealed class WhatTheRunsCouldNotFinish
    {
        private readonly HashSet<string> teamsCaughtInACircle = new(StringComparer.Ordinal);

        public int RunsGivenUpOn { get; private set; }

        public int RunsThatReachedTheHorizon { get; private set; }

        /// <summary>
        /// Only the Teams of runs that had nothing left to start, because the line about them says the
        /// Features must be waiting on each other in a circle. A Portfolio with one real circle and one Team
        /// slow enough to reach the horizon would otherwise send an operator hunting a circle that Team is
        /// not in.
        /// </summary>
        public IEnumerable<string> TeamsCaughtInACircle => teamsCaughtInACircle.Order(StringComparer.Ordinal);

        public static WhatTheRunsCouldNotFinish AllOf(IEnumerable<WhatTheRunsCouldNotFinish> shares)
        {
            var all = new WhatTheRunsCouldNotFinish();

            foreach (var share in shares)
            {
                all.RunsGivenUpOn += share.RunsGivenUpOn;
                all.RunsThatReachedTheHorizon += share.RunsThatReachedTheHorizon;
                all.teamsCaughtInACircle.UnionWith(share.teamsCaughtInACircle);
            }

            return all;
        }

        public void Note(HowTheRunEnded ending, ForecastRunPlan plan, TrialState state)
        {
            if (ending == HowTheRunEnded.EverythingFinished)
            {
                return;
            }

            if (ending == HowTheRunEnded.ReachedTheHorizon)
            {
                RunsThatReachedTheHorizon++;
                return;
            }

            RunsGivenUpOn++;

            for (var team = 0; team < plan.TeamCount; team++)
            {
                if (state.RemainingOf(team) > 0)
                {
                    teamsCaughtInACircle.Add(plan.TeamAt(team).Name);
                }
            }
        }
    }
}
