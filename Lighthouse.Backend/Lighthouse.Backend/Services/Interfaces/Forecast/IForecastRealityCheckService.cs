using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces.Forecast
{
    public interface IForecastRealityCheckService
    {
        RealityCheckResultDto Run(Team team, ThroughputFilterMode mode);
    }
}
