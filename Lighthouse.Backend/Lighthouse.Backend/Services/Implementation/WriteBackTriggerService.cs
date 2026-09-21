using System.Globalization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
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
        // Both ends of a Feature are answers the simulation produced, so both belong to the pass that
        // runs after a forecast rather than the one that runs after a sync.
        private static readonly HashSet<WriteBackValueSource> ForecastSources =
            [.. WriteBackValueSources.Completion, .. WriteBackValueSources.Start];

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
        /// The window is no longer chosen here. This used to hand the service a start date from the
        /// team's configured history and an end date of today, which ran the evidence window past the
        /// end date a team on fixed throughput dates had configured - and left the screens, which
        /// passed the browser's range, computing a different number from the same service.
        /// </summary>
        private Dictionary<string, int> RiskByReferenceIdFor(Team team, List<WriteBackMappingDefinition> mappings)
        {
            if (!mappings.Exists(m => m.ValueSource == WriteBackValueSource.SleRisk))
            {
                return [];
            }

            return teamMetricsService
                .GetSleRiskForTeam(team)
                .ToDictionary(risk => risk.ReferenceId, risk => risk.Risk);
        }

        private List<WriteBackFieldUpdate> ResolveTeamUpdates(
            List<WriteBackMappingDefinition> mappings,
            List<WorkItem> workItems,
            Dictionary<string, int> riskByReferenceId)
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

                try
                {
                    updates.AddRange(TheUpdatesFor(mapping, fieldReference, features));
                }
#pragma warning disable CA1031 // One bad mapping must not cost the others. The caller catches per
                // portfolio, so without this a single unusable mapping - an invalid DateFormat is the
                // reachable one - returns nothing for the whole portfolio, on every round, and the fields
                // that were working silently stop updating with only a log line to say why.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    logger.LogError(ex,
                        "Write-back mapping {MappingId} ({ValueSource} -> {FieldReference}) could not be resolved and was skipped: {ErrorMessage}",
                        mapping.Id, mapping.ValueSource, fieldReference, ex.Message);
                }
            }

            return updates;
        }

        /// <summary>
        /// Resolved fully before any of it is kept, so a mapping that fails part way through contributes
        /// nothing rather than the Features it happened to reach first.
        /// </summary>
        private List<WriteBackFieldUpdate> TheUpdatesFor(
            WriteBackMappingDefinition mapping,
            string fieldReference,
            List<Feature> features)
        {
            var resolved = new List<WriteBackFieldUpdate>();

            foreach (var feature in features)
            {
                var value = ResolveFeatureValue(mapping, feature);
                if (value != null)
                {
                    resolved.Add(new WriteBackFieldUpdate
                    {
                        WorkItemId = feature.ReferenceId,
                        TargetFieldReference = fieldReference,
                        Value = value,
                    });
                }
            }

            return resolved;
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
            Dictionary<string, int> riskByReferenceId)
        {
            var age = workItem.WorkItemAge(clock.Zone, clock.Today);
            var cycleTime = workItem.CycleTime(clock.Zone);

            return source switch
            {
                WriteBackValueSource.WorkItemAgeCycleTime when age > 0 => age.ToString(),
                WriteBackValueSource.WorkItemAgeCycleTime when cycleTime > 0 => cycleTime.ToString(),
                // No answer means no write, not an empty one. Every in-flight item now carries a
                // number, so the only items absent from the round are a finished one, one that has
                // not started yet, and every item of a team that published no target at all.
                WriteBackValueSource.SleRisk => RiskValueFor(workItem, riskByReferenceId),
                _ => null,
            };
        }

        private static string? RiskValueFor(WorkItemBase workItem, Dictionary<string, int> riskByReferenceId)
        {
            // The bare number, so the field stays something a board can filter and sort on. An item
            // the read did not mention gets no write at all rather than a blank - on a board there is
            // no way to say "no answer", and clearing a field a coach filters on says something
            // louder and less true than leaving it.
            return riskByReferenceId.TryGetValue(workItem.ReferenceId, out var risk)
                ? risk.ToString(CultureInfo.InvariantCulture)
                : null;
        }

        private string? ResolveFeatureValue(WriteBackMappingDefinition mapping, Feature feature)
        {
            if (WriteBackValueSources.Start.Contains(mapping.ValueSource))
            {
                return ResolveStartValue(mapping, feature);
            }

            if (WriteBackValueSources.Completion.Contains(mapping.ValueSource))
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

            return ProjectFromToday(mapping, feature.Forecast, GetPercentileFromSource(mapping.ValueSource));
        }

        /// <summary>
        /// When work on the Feature begins. Which of the two answers that is - the day it actually began,
        /// or the day the simulation expects it to - is the Feature's decision, not this service's. The
        /// table shows the same verdict from the same place, so a roadmap bar cannot end up starting on a
        /// forecast while the screen beside it shows the real day.
        /// </summary>
        private string? ResolveStartValue(WriteBackMappingDefinition mapping, Feature feature)
        {
            var start = feature.WhenWorkBegins;

            return start.Source switch
            {
                // Bug #5567 decision 4: StartedDate is a stored instant, and the day it falls on depends on
                // the zone you ask in. Formatting it directly would send the tracker the UTC day while the
                // screen shows the instance day - one apart for anything recorded late evening or early
                // morning, which is exactly when a state transition tends to be recorded.
                StartDateSource.Observed when start.ObservedDate is { } dayWorkBegan
                    => Format(mapping, InstanceCalendar.AsUtcMidnight(clock.ToInstanceDay(dayWorkBegan))),
                StartDateSource.Forecast when start.Forecast is { } forecast
                    => ProjectFromToday(mapping, forecast, GetPercentileFromSource(mapping.ValueSource)),

                // Nothing can be said. Writing anything here would fill the field with today's date, in
                // the same shape a real answer arrives in.
                _ => null,
            };
        }

        private string? ProjectFromToday(WriteBackMappingDefinition mapping, ForecastBase forecast, int percentile)
        {
            var daysAway = forecast.GetProbability(percentile);

            if (daysAway < 0)
            {
                return null;
            }

            var windowStart = clock.TodayAsUtcMidnight;
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                windowStart, windowStart.AddDays(daysAway));

            return Format(mapping, blackoutPeriods.ProjectWorkingDays(windowStart, daysAway));
        }

        private static string Format(WriteBackMappingDefinition mapping, DateTime date)
        {
            // Invariant, like RiskValueFor above: a write-back runs on a background job, so the culture is
            // whatever the process happens to have. Under a non-invariant one the "/" in a custom format is
            // a separator placeholder rather than a slash, and the culture's own calendar applies - ar-SA
            // would render 2026-03-16 as 1447-09-27.
            return mapping.TargetValueType == WriteBackTargetValueType.FormattedText && !string.IsNullOrEmpty(mapping.DateFormat)
                ? date.ToString(mapping.DateFormat, CultureInfo.InvariantCulture)
                : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static int GetPercentileFromSource(WriteBackValueSource source)
        {
            return source switch
            {
                WriteBackValueSource.ForecastPercentile50 or WriteBackValueSource.ForecastedStartPercentile50 => 50,
                WriteBackValueSource.ForecastPercentile70 or WriteBackValueSource.ForecastedStartPercentile70 => 70,
                WriteBackValueSource.ForecastPercentile85 or WriteBackValueSource.ForecastedStartPercentile85 => 85,
                WriteBackValueSource.ForecastPercentile95 or WriteBackValueSource.ForecastedStartPercentile95 => 95,
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Not a forecast source"),
            };
        }
    }
}
