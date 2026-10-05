using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// When the Team next refines, and how many Work Items it is likely to pull until then. The range is the
    /// Team's own manual How Many forecast with that day as its target date, worked out the way the Forecasts
    /// tab does, so the two never disagree.
    /// </summary>
    public sealed class RefinementNeedCalculator(
        IRefinementCalendar refinementCalendar,
        ILighthouseClock clock,
        IBlackoutPeriodService blackoutPeriodService,
        ITeamMetricsService teamMetricsService,
        IForecastService forecastService)
    {
        public RefinementOutlook For(Team team, int readyCount)
        {
            var calendar = refinementCalendar.FactsFor(team.RefinementSettings?.Cadence);

            return new RefinementOutlook(calendar, NeedFor(team, calendar, readyCount));
        }

        private RefinementNeed NeedFor(Team team, RefinementCalendarFacts calendar, int readyCount)
        {
            if (calendar.NextRefinementDate is not { } nextRefinement)
            {
                return RefinementNeed.Unavailable(NeedUnavailableReason.NoCadence);
            }

            var status = teamMetricsService.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting);
            if (!status.HasSufficientData)
            {
                return RefinementNeed.Unavailable(NeedUnavailableReason.InsufficientData);
            }

            var forecastWindowStart = clock.TodayAsUtcMidnight;
            var targetDate = InstanceCalendar.AsUtcMidnight(nextRefinement);
            var workingDays = blackoutPeriodService
                .GetEffectiveBlackoutDays(forecastWindowStart, targetDate)
                .CountWorkingDays(forecastWindowStart, targetDate);

            var forecast = forecastService.HowMany(status.Throughput, workingDays);
            var band = team.RefinementSettings?.Band ?? new RefinementBand();
            var range = new NeedRange(
                NeedBand.ValueAt(forecast, band.LowPercentile),
                NeedBand.ValueAt(forecast, band.HighPercentile),
                band.LowPercentile,
                band.HighPercentile,
                workingDays);

            return new RefinementNeed(NeedBand.VerdictFor(readyCount, range.Low, range.High), null, range);
        }
    }
}
