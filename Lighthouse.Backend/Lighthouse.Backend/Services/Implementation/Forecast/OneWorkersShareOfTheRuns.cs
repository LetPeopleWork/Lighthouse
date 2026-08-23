namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// One worker's share of a forecast's simulated runs, and everything that share needs to carry them out.
    /// Each worker has its own, so nothing at all is written to from two places at once and there is no lock
    /// anywhere near the busiest loop in the product. The shares are added together once the runs are over.
    /// </summary>
    public sealed class OneWorkersShareOfTheRuns(ForecastRunPlan plan)
    {
        private readonly TrialState state = new(plan);

        public TrialCompletions Completions { get; } = new(plan.RowCount);

        public WhatTheRunsCouldNotFinish WhatWentWrong { get; } = new();

        public void CarryOut(SimulatedRun oneRun, int trial)
            => WhatWentWrong.Note(oneRun.CarryOut(trial, state, Completions), trial, plan, state);
    }
}
