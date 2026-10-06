using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// When the Team next refines, and how many Work Items it is likely to pull over one Refinement cycle: what a
    /// Refinement has to leave ready to last until the Refinement after it. The range is the Team's own manual How
    /// Many forecast over the working days of that cycle, worked out the way the Forecasts tab does, so the two
    /// never disagree.
    /// </summary>
    public sealed class RefinementNeedCalculator(
        IRefinementCalendar refinementCalendar,
        IBlackoutPeriodService blackoutPeriodService,
        ITeamMetricsService teamMetricsService,
        IForecastService forecastService)
    {
        public RefinementOutlook For(Team team, int readyCount)
        {
            var calendar = refinementCalendar.FactsFor(team.RefinementSettings?.Cadence);

            return new RefinementOutlook(calendar, NeedFor(team, calendar.Cycle, readyCount));
        }

        /// <summary>
        /// Why there is no range, or null when there is one. The most basic missing piece of set-up is named
        /// first, and the Throughput history is only looked at when it is the last thing left to ask.
        /// </summary>
        public static NeedUnavailableReason? UnavailableReasonFor(bool hasRefinementStates, bool hasCadence, Func<bool> hasSufficientData)
        {
            if (!hasRefinementStates)
            {
                return NeedUnavailableReason.NoRefinementStates;
            }

            if (!hasCadence)
            {
                return NeedUnavailableReason.NoCadence;
            }

            return hasSufficientData() ? null : NeedUnavailableReason.InsufficientData;
        }

        private RefinementNeed NeedFor(Team team, RefinementCycle? cycle, int readyCount)
        {
            var status = new Lazy<ForecastThroughputStatus>(
                () => teamMetricsService.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting));

            // A next Refinement with none after it is no Refinement to plan for, the same as having no cadence.
            var reason = UnavailableReasonFor(team.HasRefinementStates, cycle is not null, () => status.Value.HasSufficientData);
            if (reason is { } unavailable)
            {
                return RefinementNeed.Unavailable(unavailable);
            }

            var cycleStart = InstanceCalendar.AsUtcMidnight(cycle!.Start);
            var cycleEnd = InstanceCalendar.AsUtcMidnight(cycle.End);
            var workingDays = blackoutPeriodService
                .GetEffectiveBlackoutDays(cycleStart, cycleEnd)
                .CountWorkingDays(cycleStart, cycleEnd);

            var forecast = forecastService.HowMany(status.Value.Throughput, workingDays);
            var band = team.RefinementSettings?.Band ?? new RefinementBand();
            var range = new NeedRange(
                NeedBand.ValueAt(forecast, band.LowPercentile),
                NeedBand.ValueAt(forecast, band.HighPercentile),
                band.LowPercentile,
                band.HighPercentile,
                workingDays,
                cycle);

            return new RefinementNeed(NeedBand.VerdictFor(readyCount, range.Low, range.High), null, range);
        }
    }
}
