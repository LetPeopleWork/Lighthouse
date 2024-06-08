namespace Lighthouse.Services.Implementation.BackgroundServices
{
    public class ForecastUpdateService : UpdateBackgroundServiceBase
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<ForecastUpdateService> logger;

        public ForecastUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<ForecastUpdateService> logger) : base(configuration, "Forecasts", logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Starting Update of Forecasts for all Features");

            using (var scope = serviceScopeFactory.CreateScope())
            {                
                var monteCarloService = scope.ServiceProvider.GetRequiredService<IMonteCarloService>();
                await monteCarloService.ForecastAllFeatures();
            }

            logger.LogInformation($"Done Updating of Forecasts for all Features");
        }
    }
}
