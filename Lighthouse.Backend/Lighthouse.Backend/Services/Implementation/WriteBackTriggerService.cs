using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WriteBackTriggerService(
        IWriteBackService writeBackService,
        ILicenseService licenseService,
        IWorkItemRepository workItemRepository,
        ILogger<WriteBackTriggerService> logger)
        : IWriteBackTriggerService
    {
        private static readonly HashSet<WriteBackValueSource> ForecastSources =
        [
            WriteBackValueSource.ForecastPercentile50,
            WriteBackValueSource.ForecastPercentile70,
            WriteBackValueSource.ForecastPercentile85,
            WriteBackValueSource.ForecastPercentile95,
        ];

        public async Task TriggerWriteBackForTeam(Team team)
        {
            try
            {
                var connection = team.WorkTrackingSystemConnection;
                if (connection == null)
                {
                    return;
                }

                var mappings = connection.WriteBackMappingDefinitions
                    .Where(m => m.AppliesTo == WriteBackAppliesTo.Team)
                    .ToList();

                if (mappings.Count == 0 || !licenseService.CanUsePremiumFeatures())
                {
                    return;
                }

                logger.LogInformation(
                    "Triggering write-back for team {TeamId} ({TeamName}), {MappingCount} mapping(s)",
                    team.Id, team.Name, mappings.Count);

                var workItems = workItemRepository
                    .GetAllByPredicate(wi => wi.TeamId == team.Id)
                    .ToList();

                var updates = ResolveTeamUpdates(mappings, workItems);

                if (updates.Count == 0)
                {
                    return;
                }

                await writeBackService.WriteFieldsToWorkItems(connection, updates);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Write-back failed for team {TeamId} ({TeamName}): {ErrorMessage}",
                    team.Id, team.Name, ex.Message);
            }
        }

        public async Task TriggerForecastWriteBackForPortfolio(Portfolio portfolio)
        {
            await TriggerPortfolioWriteBack(portfolio, isForecast: true);
        }

        public async Task TriggerFeatureWriteBackForPortfolio(Portfolio portfolio)
        {
            await TriggerPortfolioWriteBack(portfolio, isForecast: false);
        }

        private async Task TriggerPortfolioWriteBack(Portfolio portfolio, bool isForecast)
        {
            try
            {
                var connection = portfolio.WorkTrackingSystemConnection;
                if (connection == null)
                {
                    return;
                }

                var mappings = connection.WriteBackMappingDefinitions
                    .Where(m => m.AppliesTo == WriteBackAppliesTo.Portfolio)
                    .Where(m => isForecast
                        ? ForecastSources.Contains(m.ValueSource)
                        : !ForecastSources.Contains(m.ValueSource))
                    .ToList();

                if (mappings.Count == 0 || !licenseService.CanUsePremiumFeatures())
                {
                    return;
                }

                logger.LogInformation(
                    "Triggering {WriteBackType} write-back for portfolio {PortfolioId} ({PortfolioName}), {MappingCount} mapping(s)",
                    isForecast ? "forecast" : "feature", portfolio.Id, portfolio.Name, mappings.Count);

                var updates = ResolvePortfolioUpdates(mappings, portfolio.Features);

                if (updates.Count == 0)
                {
                    return;
                }

                await writeBackService.WriteFieldsToWorkItems(connection, updates);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Write-back failed for portfolio {PortfolioId} ({PortfolioName}): {ErrorMessage}",
                    portfolio.Id, portfolio.Name, ex.Message);
            }
        }

        private static List<WriteBackFieldUpdate> ResolveTeamUpdates(
            List<WriteBackMappingDefinition> mappings,
            List<WorkItem> workItems)
        {
            var updates = new List<WriteBackFieldUpdate>();

            foreach (var mapping in mappings)
            {
                foreach (var workItem in workItems)
                {
                    var value = ResolveWorkItemValue(mapping.ValueSource, workItem);
                    if (value != null)
                    {
                        updates.Add(new WriteBackFieldUpdate
                        {
                            WorkItemId = workItem.ReferenceId,
                            TargetFieldReference = mapping.TargetFieldReference,
                            Value = value,
                        });
                    }
                }
            }

            return updates;
        }

        private static List<WriteBackFieldUpdate> ResolvePortfolioUpdates(
            List<WriteBackMappingDefinition> mappings,
            List<Feature> features)
        {
            var updates = new List<WriteBackFieldUpdate>();

            foreach (var mapping in mappings)
            {
                foreach (var feature in features)
                {
                    var value = ResolveFeatureValue(mapping, feature);
                    if (value != null)
                    {
                        updates.Add(new WriteBackFieldUpdate
                        {
                            WorkItemId = feature.ReferenceId,
                            TargetFieldReference = mapping.TargetFieldReference,
                            Value = value,
                        });
                    }
                }
            }

            return updates;
        }

        private static string? ResolveWorkItemValue(WriteBackValueSource source, WorkItemBase workItem)
        {
            return source switch
            {
                WriteBackValueSource.WorkItemAge when workItem.WorkItemAge > 0 => workItem.WorkItemAge.ToString(),
                WriteBackValueSource.CycleTime when workItem.CycleTime > 0 => workItem.CycleTime.ToString(),
                _ => null,
            };
        }

        private static string? ResolveFeatureValue(WriteBackMappingDefinition mapping, Feature feature)
        {
            if (ForecastSources.Contains(mapping.ValueSource))
            {
                return ResolveForecastValue(mapping, feature);
            }

            return mapping.ValueSource switch
            {
                WriteBackValueSource.FeatureSize => feature.Size.ToString(),
                WriteBackValueSource.WorkItemAge when feature.WorkItemAge > 0 => feature.WorkItemAge.ToString(),
                WriteBackValueSource.CycleTime when feature.CycleTime > 0 => feature.CycleTime.ToString(),
                _ => null,
            };
        }

        private static string? ResolveForecastValue(WriteBackMappingDefinition mapping, Feature feature)
        {
            if (feature.StateCategory == StateCategories.Done)
            {
                return null;
            }

            var forecast = feature.Forecast;
            if (forecast == null)
            {
                return null;
            }

            var percentile = GetPercentileFromSource(mapping.ValueSource);
            var daysToCompletion = forecast.GetProbability(percentile);

            if (daysToCompletion < 0)
            {
                return null;
            }

            var forecastDate = DateTime.UtcNow.Date.AddDays(daysToCompletion);

            return mapping.TargetValueType == WriteBackTargetValueType.FormattedText && !string.IsNullOrEmpty(mapping.DateFormat)
                ? forecastDate.ToString(mapping.DateFormat)
                : forecastDate.ToString("yyyy-MM-dd");
        }

        private static int GetPercentileFromSource(WriteBackValueSource source)
        {
            return source switch
            {
                WriteBackValueSource.ForecastPercentile50 => 50,
                WriteBackValueSource.ForecastPercentile70 => 70,
                WriteBackValueSource.ForecastPercentile85 => 85,
                WriteBackValueSource.ForecastPercentile95 => 95,
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Not a forecast source"),
            };
        }
    }
}
