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
        ITeamMetricsService teamMetricsService,
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

                return ResolveTeamUpdates(mappings, workItems, RiskByReferenceIdFor(team, mappings));
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

        /// <summary>
        /// The chance each in-flight item has of missing the team's target, read from the service
        /// the screens read - so a number in someone else's tracker cannot disagree with the number
        /// on the page. Empty unless a mapping actually asks for it: the read walks the team's whole
        /// closed history, and a team that never mapped the field should not pay for it every update.
        ///
        /// The evidence is the team's configured history and the question is about today, because a
        /// field on a board is read now rather than as of whatever range a browser tab was left on.
        /// </summary>
        private Dictionary<string, int?> RiskByReferenceIdFor(Team team, List<WriteBackMappingDefinition> mappings)
        {
            if (!mappings.Exists(m => m.ValueSource == WriteBackValueSource.SleRisk))
            {
                return [];
            }

            var history = team.GetThroughputSettings(clock.Today);

            return teamMetricsService
                .GetSleRiskForTeam(team, history.StartDate, clock.TodayAsUtcMidnight)
                .ToDictionary(risk => risk.ReferenceId, risk => risk.Risk);
        }

        private List<WriteBackFieldUpdate> ResolveTeamUpdates(
            List<WriteBackMappingDefinition> mappings,
            List<WorkItem> workItems,
            Dictionary<string, int?> riskByReferenceId)
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
                    var value = ResolveWorkItemValue(mapping.ValueSource, workItem, riskByReferenceId);
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

        private string? ResolveWorkItemValue(
            WriteBackValueSource source,
            WorkItemBase workItem,
            Dictionary<string, int?> riskByReferenceId)
        {
            var age = workItem.WorkItemAge(clock.Zone, clock.Today);
            var cycleTime = workItem.CycleTime(clock.Zone);

            return source switch
            {
                WriteBackValueSource.WorkItemAgeCycleTime when age > 0 => age.ToString(),
                WriteBackValueSource.WorkItemAgeCycleTime when cycleTime > 0 => cycleTime.ToString(),
                // No answer means no write, not an empty one: an item that is finished, whose team
                // published no target, or that too little finished work can be compared against is
                // simply absent from the round rather than clearing whatever the field held.
                WriteBackValueSource.SleRisk => RiskValueFor(workItem, riskByReferenceId),
                _ => null,
            };
        }

        private static string? RiskValueFor(WorkItemBase workItem, Dictionary<string, int?> riskByReferenceId)
        {
            // The bare number, so the field stays something a board can filter and sort on.
            return riskByReferenceId.TryGetValue(workItem.ReferenceId, out var risk) && risk.HasValue
                ? risk.Value.ToString()
                : null;
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
