namespace Lighthouse.Services.Implementation.BackgroundServices
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
                var monteCarloService = scope.ServiceProvider.GetRequiredService<IMonteCarloService>();
                await monteCarloService.ForecastAllFeatures();
            }
        }
    }
}
