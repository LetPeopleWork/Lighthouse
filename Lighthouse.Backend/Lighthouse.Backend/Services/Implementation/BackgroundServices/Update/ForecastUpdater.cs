using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using System.Diagnostics;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class ForecastUpdater(
        ILogger<ForecastUpdater> logger,
        IServiceScopeFactory serviceScopeFactory,
        IUpdateQueueService updateQueueService,
        IDomainEventDispatcher domainEventDispatcher)
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

            var refreshLogService = serviceProvider.GetRequiredService<IRefreshLogService>();
            var stopwatch = Stopwatch.StartNew();
            var success = false;
            var itemCount = 0;

            try
            {
                var forecastService = serviceProvider.GetRequiredService<IForecastService>();
                await forecastService.UpdateForecastsForPortfolio(portfolio);

                var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
                serviceProvider.GetRequiredService<IWriteBackCollector>().Stage(
                    portfolio.WorkTrackingSystemConnection,
                    writeBackTriggerService.ResolveForecastWriteBackForPortfolio(portfolio));

                await domainEventDispatcher.PublishAsync(new PortfolioForecastsUpdated(portfolio.Id));

                itemCount = portfolio.Features.Count;
                success = true;
            }
            finally
            {
                stopwatch.Stop();

                // Forecasting reads what an earlier refresh already fetched, so it never contacts the
                // work tracking system - there is nothing scanned or downloaded to report here.
                var outcome = SyncOutcome.None;

                await refreshLogService.LogRefreshAsync(new RefreshLog
                {
                    Type = RefreshType.Forecast,
                    EntityId = portfolio.Id,
                    EntityName = portfolio.Name,
                    ItemCount = itemCount,
                    Mode = outcome.Mode,
                    RecordsScanned = outcome.RecordsScanned,
                    RecordsFetched = outcome.RecordsFetched,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ExecutedAt = DateTime.UtcNow,
                    Success = success
                });

                LogUpdateSummary(portfolio.Name, outcome, stopwatch.ElapsedMilliseconds, success);
            }
        }
    }
}
