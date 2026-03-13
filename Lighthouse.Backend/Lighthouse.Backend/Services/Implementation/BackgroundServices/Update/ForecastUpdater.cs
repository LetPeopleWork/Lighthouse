using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class ForecastUpdater(
        ILogger<ForecastUpdater> logger,
        IServiceScopeFactory serviceScopeFactory,
        IUpdateQueueService updateQueueService)
        : UpdateServiceBase<Portfolio>(logger, serviceScopeFactory, updateQueueService, UpdateType.Forecasts),
            IForecastUpdater
    {
        protected override RefreshSettings GetRefreshSettings()
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        protected override bool ShouldUpdateEntity(Portfolio entity, RefreshSettings refreshSettings)
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var portfolioRepo = serviceProvider.GetRequiredService<IRepository<Portfolio>>();

            var portfolio = portfolioRepo.GetById(id);
            if (portfolio == null)
            {
                return;
            }

            var forecastService = serviceProvider.GetRequiredService<IForecastService>();
            await forecastService.UpdateForecastsForPortfolio(portfolio);

            var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
            await writeBackTriggerService.TriggerForecastWriteBackForPortfolio(portfolio);
        }
    }
}
