using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    public class DataRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<DataRetentionService> logger;
        private readonly int startupDelayInMinutes;

        public DataRetentionService(IServiceScopeFactory serviceScopeFactory, ILogger<DataRetentionService> logger, int startupDelayInMinutes = 30)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
            this.startupDelayInMinutes = startupDelayInMinutes;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Data Retention Service started");

            await AwaitStartupDelay(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await InvokeDataCleanUp();
                await WaitForNextExecution(stoppingToken);
            }

            logger.LogInformation("Data Retention Service was stopped");
        }

        private async Task InvokeDataCleanUp()
        {
            logger.LogInformation("Invoking Data Clean Up");

            try
            {
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var featureHistoryService = scope.ServiceProvider.GetRequiredService<IFeatureHistoryService>();

                    await featureHistoryService.CleanupData();
                }

                logger.LogInformation("Data Clean Up done");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An exception occured during Data Cleanup: {Exception}.", exception);
            }
        }

        private async Task WaitForNextExecution(CancellationToken token)
        {
            var waitTimeInHours = 24;

            logger.LogInformation("Waiting {Time} hours for next execution of Data Cleanup", waitTimeInHours);
            await Task.Delay(TimeSpan.FromHours(waitTimeInHours), token);
        }

        private async Task AwaitStartupDelay(CancellationToken token)
        {
            logger.LogInformation("Waiting {Time} minutes before invoking Data Cleanup", startupDelayInMinutes);
            await Task.Delay(TimeSpan.FromMinutes(startupDelayInMinutes), token);
        }
    }
}
