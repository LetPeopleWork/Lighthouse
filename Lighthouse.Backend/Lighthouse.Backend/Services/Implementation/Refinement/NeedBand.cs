using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public static class NeedBand
    {
        /// <summary>
        /// The end of the range read at this likelihood. A How Many forecast is read at the share of runs that
        /// reach a count, so the high end at 85 is the forecast at 15: only 15% of runs pull more than it. The
        /// low end at 50 is the median, a count the Team pulls at least with 50% likelihood.
        /// </summary>
        public static int ValueAt(HowManyForecast forecast, int percentile) => forecast.GetProbability(100 - percentile);

        public static RefinementVerdict VerdictFor(int readyCount, int low, int high)
        {
            if (readyCount < low)
            {
                return RefinementVerdict.Below;
            }

            return readyCount > high ? RefinementVerdict.Above : RefinementVerdict.In;
        }
    }
}
