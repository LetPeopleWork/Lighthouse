using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class SleYardstickResolver(ITeamMetricsService teamMetricsService, ILighthouseClock clock)
    {
        private const int FallbackPercentile = 85;

        public Yardstick For(Team team)
        {
            // A probability without a number of days, or days without a probability, promises nothing.
            if (team.ServiceLevelExpectationProbability > 0 && team.ServiceLevelExpectationRange > 0)
            {
                return new Yardstick(YardstickSource.Sle, team.ServiceLevelExpectationRange, team.ServiceLevelExpectationProbability);
            }

            return CycleTimeFallbackFor(team);
        }

        private Yardstick CycleTimeFallbackFor(Team team)
        {
            var window = team.GetThroughputSettings(clock.Today);
            var fallback = teamMetricsService
                .GetCycleTimePercentilesForTeam(team, window.StartDate, window.EndDate)
                .FirstOrDefault(percentile => percentile.Percentile == FallbackPercentile);

            if (fallback is null)
            {
                return Yardstick.None;
            }

            return new Yardstick(YardstickSource.CycleTimeFallback, fallback.Value, FallbackPercentile);
        }
    }
}
