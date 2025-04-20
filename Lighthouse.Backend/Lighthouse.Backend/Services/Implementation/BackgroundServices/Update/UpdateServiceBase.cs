using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public abstract class UpdateServiceBase<TEntity> : BackgroundService, IUpdateService where TEntity : class, IEntity
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IUpdateQueueService updateQueueService;
        private readonly UpdateType updateType;

        protected UpdateServiceBase(ILogger<UpdateServiceBase<TEntity>> logger, IServiceScopeFactory serviceScopeFactory, IUpdateQueueService updateQueueService, UpdateType updateType)
        {
            Logger = logger;
            this.serviceScopeFactory = serviceScopeFactory;
            this.updateQueueService = updateQueueService;
            this.updateType = updateType;
        }

        protected ILogger<UpdateServiceBase<TEntity>> Logger { get; }

        public void TriggerUpdate(int id)
        {
            updateQueueService.EnqueueUpdate(updateType, id, async serviceProvider =>
            {
                try
                {
                    await Update(id, serviceProvider);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "An exception occurred while updating {Entity} with ID {Id}: {Exception}", typeof(TEntity).Name, id, exception.Message);
                }
            });
        }

        protected static T GetServiceFromServiceScope<T>(IServiceScope scope) where T : notnull
        {
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        protected IServiceScope CreateServiceScope()
        {
            return serviceScopeFactory.CreateScope();
        }

        protected abstract RefreshSettings GetRefreshSettings();

        protected abstract Task Update(int id, IServiceProvider serviceProvider);

        protected abstract bool ShouldUpdateEntity(TEntity entity, RefreshSettings refreshSettings);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Start Executing Background Service");

            await DelayStart(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await TryUpdating(stoppingToken);
            }

            Logger.LogInformation("Stopping Executing Background Service");
        }

        private async Task TryUpdating(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Starting Update for {UpdateType}", updateType.ToString());
                UpdateAll();

                var refreshSettings = GetRefreshSettings();

                Logger.LogInformation("Done Updating {UpdateType} - Waiting {Interval} Minutes till next execution", updateType.ToString(), refreshSettings.Interval);
                await Task.Delay(TimeSpan.FromMinutes(refreshSettings.Interval), stoppingToken);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "An exception occured: {Exception}.", exception);
            }
        }

        private void UpdateAll()
        {
            using (var scope = CreateServiceScope())
            {
                var repository = GetServiceFromServiceScope<IRepository<TEntity>>(scope);
                var refreshSettings = GetRefreshSettings();

                foreach (var entity in repository.GetAll().ToList())
                {
                    Logger.LogInformation("Checking last update for {Entity}", entity.Id);
                    if (ShouldUpdateEntity(entity, refreshSettings))
                    {
                        TriggerUpdate(entity.Id);
                    }
                }
            }
        }

        private async Task DelayStart(CancellationToken stoppingToken)
        {
            var refreshSettings = GetRefreshSettings();

            Logger.LogInformation("Wait {StartDelay} minutes before starting...", refreshSettings.StartDelay);
            await Task.Delay(TimeSpan.FromMinutes(refreshSettings.StartDelay), stoppingToken);
        }
    }
}
