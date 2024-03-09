
namespace Lighthouse.Services.Implementation.BackgroundServices
{
    public abstract class UpdateBackgroundServiceBase : BackgroundService
    {
        private readonly ILogger<UpdateBackgroundServiceBase> logger;

        protected UpdateBackgroundServiceBase(IConfiguration configuration, string configurationSectionName, ILogger<UpdateBackgroundServiceBase> logger)
        {
            StartDelay = configuration.GetValue<int>($"PeriodicRefresh:{configurationSectionName}:StartDelay");
            Interval = configuration.GetValue<int>($"PeriodicRefresh:{configurationSectionName}:Interval");
            RefreshAfter = configuration.GetValue<int>($"PeriodicRefresh:{configurationSectionName}:RefreshAfter");

            this.logger = logger;
        }

        protected int StartDelay { get; }

        protected int Interval { get; }

        protected int RefreshAfter { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DelayStart(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await TryUpdating(stoppingToken);
            }
        }

        protected abstract Task UpdateAllItems(CancellationToken stoppingToken);

        private async Task TryUpdating(CancellationToken stoppingToken)
        {
            try
            {
                await UpdateAllItems(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(Interval), stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "An exception occured: {Exception}.", exception);
            }
        }

        private async Task DelayStart(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(StartDelay), stoppingToken);
        }
    }
}
