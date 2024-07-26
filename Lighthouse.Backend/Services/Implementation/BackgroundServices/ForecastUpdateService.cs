using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    public class ForecastUpdateService : UpdateBackgroundServiceBase
    {
        private readonly ILogger<ForecastUpdateService> logger;

        public ForecastUpdateService(IServiceScopeFactory serviceScopeFactory, ILogger<ForecastUpdateService> logger) : base(serviceScopeFactory, logger)
        {
            this.logger = logger;
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetForecastRefreshSettings();
            }
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Starting Update of Forecasts for all Features");

            using (var scope = CreateServiceScope())
            {                
                var monteCarloService = GetServiceFromServiceScope<IMonteCarloService>(scope);
                await monteCarloService.ForecastAllFeatures();
            }

            logger.LogInformation($"Done Updating of Forecasts for all Features");
        }
    }
}
