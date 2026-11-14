using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.Forecasting.Application
{
    public interface IForecastRealityCheckService
    {
        RealityCheckResultDto Run(Team team, ThroughputFilterMode mode);
    }
}
