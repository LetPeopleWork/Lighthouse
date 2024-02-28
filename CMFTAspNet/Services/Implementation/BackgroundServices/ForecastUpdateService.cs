

using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.Repositories;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation.BackgroundServices
{
    public class ForecastUpdateService : BackgroundService
    {
        private readonly IConfiguration configuration;
        private IServiceScopeFactory serviceScopeFactory;
        private ILogger<ForecastUpdateService> logger;

        public ForecastUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<ForecastUpdateService> logger)
        {
            this.configuration = configuration;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DelayStart(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await TryUpdateForecasts(stoppingToken);
            }
        }

        private async Task DelayStart(CancellationToken stoppingToken)
        {
            var delayedStart = configuration.GetValue<int>("PeriodicRefresh:Forecasts:StartDelay");
            await Task.Delay(TimeSpan.FromMinutes(delayedStart), stoppingToken);
        }

        private async Task TryUpdateForecasts(CancellationToken stoppingToken)
        {
            try
            {
                await UpdateAllFeatureForecasts(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }

        private async Task UpdateAllFeatureForecasts(CancellationToken stoppingToken)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

                var features = featureRepository.GetAll();
                var monteCarloService = scope.ServiceProvider.GetRequiredService<IMonteCarloService>();
                monteCarloService.ForecastFeatures(features);
                await featureRepository.Save();

            }

            var forecastUpdateInterval = configuration.GetValue<int>("PeriodicRefresh:Forecasts:Interval");
            await Task.Delay(TimeSpan.FromMinutes(forecastUpdateInterval), stoppingToken);
        }
    }
}
