using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public static class NeedBand
    {
        /// <summary>
        /// How many Work Items the Team pulls with this likelihood. A How Many forecast is read at the share of
        /// runs that reach a count, so "85% likely" is the count only 15% of the runs fall short of.
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
