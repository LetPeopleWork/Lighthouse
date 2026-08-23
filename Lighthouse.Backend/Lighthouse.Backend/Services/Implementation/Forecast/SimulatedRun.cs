using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// One simulated run of a forecast: a single day counter, and on each day every Team that still has work
    /// draws its own delivery from its own measured history and works on its own rows.
    ///
    /// One clock for every Team is what makes "has the Feature waited on finished yet?" a question with an
    /// answer. Each Team used to run on a clock of its own, so its Day 5 and another Team's Day 5 were not
    /// the same moment and not even the same run - the question was not merely unanswered, it was not well
    /// formed. A shared clock shares time and nothing else: what a Team delivers still comes from that
    /// Team's own history.
    /// </summary>
    public sealed class SimulatedRun(ForecastRunPlan plan, IDrawStream draws, int theMostDaysARunMayCover)
    {
        private const int TheDrawForHowMuchTheTeamDelivers = 0;

        private const int TheDrawsThatPickAFeature = 1;

        public HowTheRunEnded CarryOut(int trial, TrialState state, TrialCompletions completions)
        {
            state.StartAgain();

            var day = 1;

            while (state.RemainingEverywhere > 0)
            {
                if (day > theMostDaysARunMayCover)
                {
                    return HowTheRunEnded.RanOutOfDays;
                }

                if (!state.AnythingCanBeWorkedOn(day))
                {
                    return HowTheRunEnded.NothingLeftCouldBeStarted;
                }

                for (var team = 0; team < plan.TeamCount; team++)
                {
                    if (state.RemainingOf(team) > 0)
                    {
                        WorkOneDayOf(team, trial, day, state, completions);
                    }
                }

                day++;
            }

            return HowTheRunEnded.EverythingFinished;
        }

        /// <summary>
        /// A Team that could start nothing today has not banked the day. Its delivery was drawn and thrown
        /// away, because carrying it forward would hand the wait back the time it cost - which is the whole
        /// point of leaving a Feature that cannot start out of the running.
        /// </summary>
        private void WorkOneDayOf(int teamIndex, int trial, int day, TrialState state, TrialCompletions completions)
        {
            var throughput = plan.ThroughputOf(teamIndex);
            var teamId = plan.TeamAt(teamIndex).Id;

            var delivered = throughput.GetCountOnDay(
                draws.Draw(trial, teamId, day, TheDrawForHowMuchTheTeamDelivers, throughput.History));

            for (var closed = 0; closed < delivered && state.RemainingOf(teamIndex) > 0; closed++)
            {
                var ready = state.RowsReadyToBeWorkedOnBy(teamIndex, day);

                if (ready.IsEmpty)
                {
                    return;
                }

                var howManyItMayWorkOnAtOnce = Math.Min(HowManyFeaturesAtOnce(teamIndex), ready.Length);
                var worked = ready[draws.Draw(trial, teamId, day, TheDrawsThatPickAFeature + closed, howManyItMayWorkOnAtOnce)];

                if (state.CloseOneItemOf(worked, day))
                {
                    completions.RecordThat(worked, day);
                }
            }
        }

        private int HowManyFeaturesAtOnce(int teamIndex)
        {
            var featureWip = plan.TeamAt(teamIndex).FeatureWIP;

            return featureWip > 0 ? featureWip : 1;
        }
    }
}
