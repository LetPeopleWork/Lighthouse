using Lighthouse.Backend.Models.AppSettings;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    public abstract class UpdateBackgroundServiceBase : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<UpdateBackgroundServiceBase> logger;

        protected UpdateBackgroundServiceBase(IServiceScopeFactory serviceScopeFactory, ILogger<UpdateBackgroundServiceBase> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }        

        protected abstract RefreshSettings GetRefreshSettings();

        protected T GetServiceFromServiceScope<T>(IServiceScope scope) where T : notnull
        {
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        protected IServiceScope CreateServiceScope()
        {
            return serviceScopeFactory.CreateScope();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Start Executing Background Service");

            await DelayStart(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await TryUpdating(stoppingToken);
            }

            logger.LogInformation("Stopping Executing Background Service");
        }

        protected abstract Task UpdateAllItems(CancellationToken stoppingToken);

        private async Task TryUpdating(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Invoking Update");
                await UpdateAllItems(stoppingToken);

                var refreshSettings = GetRefreshSettings();

                logger.LogInformation("Done Updating - Waiting {Interval} Minutes till next execution", refreshSettings.Interval);
                await Task.Delay(TimeSpan.FromMinutes(refreshSettings.Interval), stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An exception occured: {Exception}.", exception);
            }
        }

        private async Task DelayStart(CancellationToken stoppingToken)
        {
            var refreshSettings = GetRefreshSettings();

            logger.LogInformation("Wait {StartDelay} minutes before starting...", refreshSettings.StartDelay);
            await Task.Delay(TimeSpan.FromMinutes(refreshSettings.StartDelay), stoppingToken);
        }
    }
}
