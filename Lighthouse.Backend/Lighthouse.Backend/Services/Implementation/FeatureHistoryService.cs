﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Build.Framework;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureHistoryService : IFeatureHistoryService
    {
        private readonly IRepository<FeatureHistoryEntry> repository;
        private readonly IAppSettingService appSettingsService;
        private readonly ILogger<FeatureHistoryService> logger;

        public FeatureHistoryService(IRepository<FeatureHistoryEntry> repository, IAppSettingService appSettingsService, ILogger<FeatureHistoryService> logger)
        {
            this.repository = repository;
            this.appSettingsService = appSettingsService;
            this.logger = logger;
        }

        public async Task ArchiveFeature(Feature feature)
        {
            logger.LogInformation("Archiving Feature {Feature} ({ID})", feature.Name, feature.Id);

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var historyEntry = repository.GetByPredicate(fh => fh.FeatureId == feature.Id && fh.Snapshot == today);

            if (historyEntry == null)
            {
                logger.LogDebug("Adding New Feature History Entry for Feature {Feature} ({ID})", feature.Name, feature.Id);
                AddNewFeatureHistoryEntry(feature);
            }
            else
            {
                logger.LogDebug("Update Existing Feature History Entry for Feature {Feature} ({ID})", feature.Name, feature.Id);
                UpdateExistingFeatureHistoryEntry(feature, historyEntry);
            }

            await repository.Save();
        }

        public async Task CleanupData()
        {
            logger.LogInformation("Cleaning Up Feature History Entry Data");

            var cleanUpDataHistorySettings = appSettingsService.GetDataRetentionSettings();
            var cutOffDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-cleanUpDataHistorySettings.MaxStorageTimeInDays));

            var oldEntries = repository.GetAllByPredicate(fh => fh.Snapshot < cutOffDate);
            foreach (var entry in oldEntries)
            {
                logger.LogInformation("Removing Feature History Entry for Feature {Feature} from {Date}", entry.FeatureId, entry.Snapshot);
                repository.Remove(entry.Id);
            }

            await repository.Save();
        }

        private void UpdateExistingFeatureHistoryEntry(Feature feature, FeatureHistoryEntry? historyEntry)
        {
            historyEntry.Update(feature);
            repository.Update(historyEntry);
        }

        private void AddNewFeatureHistoryEntry(Feature feature)
        {
            var historyEntry = new FeatureHistoryEntry(feature);
            repository.Add(historyEntry);
        }
    }
}
