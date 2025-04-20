﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class ForecastUpdater : UpdateServiceBase<Project>, IForecastUpdater
    {
        public ForecastUpdater(
            ILogger<ForecastUpdater> logger, IServiceScopeFactory serviceScopeFactory, IUpdateQueueService updateQueueService)
            : base(logger, serviceScopeFactory, updateQueueService, UpdateType.Forecasts)
        {
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        protected override bool ShouldUpdateEntity(Project entity, RefreshSettings refreshSettings)
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var projectRepository = serviceProvider.GetRequiredService<IRepository<Project>>();

            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return;
            }

            var forecastService = serviceProvider.GetRequiredService<IForecastService>();
            await forecastService.UpdateForecastsForProject(project);
        }
    }
}
