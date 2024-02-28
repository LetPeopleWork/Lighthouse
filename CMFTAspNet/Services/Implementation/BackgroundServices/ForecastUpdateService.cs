using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation.BackgroundServices
{
    public class ForecastUpdateService : UpdateBackgroundServiceBase
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public ForecastUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<ForecastUpdateService> logger) : base(configuration, "Forecasts", logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

                var features = featureRepository.GetAll();
                var monteCarloService = scope.ServiceProvider.GetRequiredService<IMonteCarloService>();
                monteCarloService.ForecastFeatures(features);
                await featureRepository.Save();

            }
        }
    }
}
