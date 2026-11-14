using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Lighthouse.Backend.Modules.Forecasting.Domain;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// What every web host that runs the reality check in a scenario replaces: the day stands still, the licence
    /// is premium, the shipped engine draws from a pinned starting number over fewer runs, and every forecast
    /// passes through the scenario's script first.
    /// </summary>
    internal static class RealityCheckOnAWebHost
    {
        internal static void PinWhatTheCheckDependsOn(this IServiceCollection services, RealityCheckForecastScript script)
        {
            services.RemoveAll<ILighthouseClock>();
            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(RealityCheckForecastScript.Today));

            services.RemoveAll<ILicenseService>();
            services.AddSingleton(PortDoubles.PremiumLicence());

            services.RemoveAll<IDrawStreamFactory>();
            services.AddSingleton<IDrawStreamFactory>(new DrawsFromAPinnedStartingNumber(RealityCheckForecastScript.PinnedStartingNumber));
            services.RemoveAll<ForecastSimulationLimits>();
            services.AddSingleton(new ForecastSimulationLimits(
                RealityCheckForecastScript.SimulatedRunsWhenTheEngineRuns,
                ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover));

            services.RemoveAll<IForecastService>();
            services.AddScoped<ForecastService>();
            services.AddScoped<IForecastService>(provider =>
                script.Over(provider.GetRequiredService<ForecastService>()));
        }
    }
}
