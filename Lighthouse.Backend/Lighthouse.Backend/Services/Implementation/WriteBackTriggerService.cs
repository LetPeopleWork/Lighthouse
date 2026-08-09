using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WriteBackTriggerService(
        ILicenseService licenseService,
        IWorkItemRepository workItemRepository,
        IBlackoutPeriodService blackoutPeriodService,
        ILighthouseClock clock,
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

        public IReadOnlyList<WriteBackFieldUpdate> ResolveWriteBackForTeam(Team team)
        {
            try
            {
                var mappings = team.WorkTrackingSystemConnection.WriteBackMappingDefinitions
                    .Where(m => m.AppliesTo == WriteBackAppliesTo.Team)
                    .ToList();

                if (mappings.Count == 0 || !licenseService.CanUsePremiumFeatures())
                {
                    return [];
                }

                logger.LogInformation(
                    "Resolving write-back for team {TeamId} ({TeamName}), {MappingCount} mapping(s)",
                    team.Id, team.Name, mappings.Count);

                var workItems = workItemRepository
                    .GetAllByPredicate(wi => wi.TeamId == team.Id)
                    .ToList();

                return ResolveTeamUpdates(mappings, workItems);
            }
            // Resolution reads repositories and the blackout calendar, so it can still fail. Swallowing
            // here keeps a broken mapping from cutting short the rest of the update execution, which is
            // what the four separate try/catches did before ADR-144 collapsed them into one flush.
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex,
                    "Write-back resolution failed for team {TeamId} ({TeamName}): {ErrorMessage}",
                    team.Id, team.Name, ex.Message);
                return [];
            }
        }

        public IReadOnlyList<WriteBackFieldUpdate> ResolveForecastWriteBackForPortfolio(Portfolio portfolio)
        {
            return ResolvePortfolioWriteBack(portfolio, isForecast: true);
        }

        public IReadOnlyList<WriteBackFieldUpdate> ResolveFeatureWriteBackForPortfolio(Portfolio portfolio)
        {
            return ResolvePortfolioWriteBack(portfolio, isForecast: false);
        }

        private List<WriteBackFieldUpdate> ResolvePortfolioWriteBack(Portfolio portfolio, bool isForecast)
        {
            try
            {
                var mappings = portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions
                    .Where(m => m.AppliesTo == WriteBackAppliesTo.Portfolio)
                    .Where(m => isForecast
                        ? ForecastSources.Contains(m.ValueSource)
                        : !ForecastSources.Contains(m.ValueSource))
                    .ToList();

                if (mappings.Count == 0 || !licenseService.CanUsePremiumFeatures())
                {
                    return [];
                }

                logger.LogInformation(
                    "Resolving {WriteBackType} write-back for portfolio {PortfolioId} ({PortfolioName}), {MappingCount} mapping(s)",
                    isForecast ? "forecast" : "feature", portfolio.Id, portfolio.Name, mappings.Count);

                return ResolvePortfolioUpdates(mappings, portfolio.Features);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex,
                    "Write-back resolution failed for portfolio {PortfolioId} ({PortfolioName}): {ErrorMessage}",
                    portfolio.Id, portfolio.Name, ex.Message);
                return [];
            }
        }

        private List<WriteBackFieldUpdate> ResolveTeamUpdates(
            List<WriteBackMappingDefinition> mappings,
            List<WorkItem> workItems)
        {
            var updates = new List<WriteBackFieldUpdate>();

            foreach (var mapping in mappings)
            {
                var fieldReference = mapping.AdditionalFieldDefinition?.Reference;
                if (string.IsNullOrEmpty(fieldReference))
                {
                    LogUnresolvedMapping(mapping);
                    continue;
                }

                foreach (var workItem in workItems)
                {
                    var value = ResolveWorkItemValue(mapping.ValueSource, workItem);
                    if (value != null)
                    {
                        updates.Add(new WriteBackFieldUpdate
                        {
                            WorkItemId = workItem.ReferenceId,
                            TargetFieldReference = fieldReference,
                            Value = value,
                        });
                    }
                }
            }

            return updates;
        }

        private List<WriteBackFieldUpdate> ResolvePortfolioUpdates(
            List<WriteBackMappingDefinition> mappings,
            List<Feature> features)
        {
            var updates = new List<WriteBackFieldUpdate>();

            foreach (var mapping in mappings)
            {
                var fieldReference = mapping.AdditionalFieldDefinition?.Reference;
                if (string.IsNullOrEmpty(fieldReference))
                {
                    LogUnresolvedMapping(mapping);
                    continue;
                }

                foreach (var feature in features)
                {
                    var value = ResolveFeatureValue(mapping, feature);
                    if (value != null)
                    {
                        updates.Add(new WriteBackFieldUpdate
                        {
                            WorkItemId = feature.ReferenceId,
                            TargetFieldReference = fieldReference,
                            Value = value,
                        });
                    }
                }
            }

            return updates;
        }

        private void LogUnresolvedMapping(WriteBackMappingDefinition mapping)
        {
            logger.LogWarning(
                "Skipping write-back mapping {MappingId}: AdditionalFieldDefinition is not resolved (Id: {FieldId})",
                mapping.Id, mapping.AdditionalFieldDefinitionId);
        }

        private string? ResolveWorkItemValue(WriteBackValueSource source, WorkItemBase workItem)
        {
            var age = workItem.WorkItemAge(clock.Zone, clock.Today);
            var cycleTime = workItem.CycleTime(clock.Zone);

            return source switch
            {
                WriteBackValueSource.WorkItemAgeCycleTime when age > 0 => age.ToString(),
                WriteBackValueSource.WorkItemAgeCycleTime when cycleTime > 0 => cycleTime.ToString(),
                _ => null,
            };
        }

        private string? ResolveFeatureValue(WriteBackMappingDefinition mapping, Feature feature)
        {
            if (ForecastSources.Contains(mapping.ValueSource))
            {
                return ResolveForecastValue(mapping, feature);
            }

            var age = feature.WorkItemAge(clock.Zone, clock.Today);
            var cycleTime = feature.CycleTime(clock.Zone);

            return mapping.ValueSource switch
            {
                WriteBackValueSource.FeatureSize => feature.Size.ToString(),
                WriteBackValueSource.WorkItemAgeCycleTime when age > 0 => age.ToString(),
                WriteBackValueSource.WorkItemAgeCycleTime when cycleTime > 0 => cycleTime.ToString(),
                _ => null,
            };
        }

        private string? ResolveForecastValue(WriteBackMappingDefinition mapping, Feature feature)
        {
            if (feature.StateCategory == StateCategories.Done)
            {
                return null;
            }

            var forecast = feature.Forecast;

            var percentile = GetPercentileFromSource(mapping.ValueSource);
            var daysToCompletion = forecast.GetProbability(percentile);

            if (daysToCompletion < 0)
            {
                return null;
            }

            var forecastWindowStart = clock.TodayAsUtcMidnight;
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                forecastWindowStart, forecastWindowStart.AddDays(daysToCompletion));

            var forecastDate = blackoutPeriods.ProjectWorkingDays(forecastWindowStart, daysToCompletion);

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
