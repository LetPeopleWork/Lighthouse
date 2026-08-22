using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.API.DTO.Archived
{
    /// <summary>
    /// Builds the view of a retired Delivery. It is handed the closure record and nothing else that
    /// can be recalculated - no Features, no forecast, no calendar - so there is no way for a number
    /// on a retired Delivery to quietly become today's answer instead of the one it closed with.
    /// </summary>
    public static class ArchivedDeliveryProjection
    {
        private static readonly JsonSerializerOptions PinnedJsonReadOptions = new() { PropertyNameCaseInsensitive = true };

        public static ArchivedDeliveryDto ToDto(ArchivedDeliveryIdentity identity, DeliveryClosureRecord closureRecord)
        {
            var ruleSet = RuleSetFrom(closureRecord.RuleDefinitionJson);

            return new ArchivedDeliveryDto
            {
                Id = identity.Id,
                Name = identity.Name,
                Date = identity.Date,
                PortfolioId = identity.PortfolioId,
                ConcurrencyToken = identity.ConcurrencyToken,
                MetricSnapshotCount = identity.MetricSnapshotCount,
                ArchivedOn = closureRecord.ArchivedOn,
                Progress = ProgressOf(closureRecord),
                TotalWork = closureRecord.TotalWork,
                DoneWork = closureRecord.DoneWork,
                RemainingWork = closureRecord.RemainingWork,
                LikelihoodPercentage = closureRecord.LikelihoodPercentage,
                HasSufficientData = closureRecord.HasSufficientData,
                TeamsWithoutForecast = TeamNamesFrom(closureRecord.TeamsWithoutForecastJson),
                FeatureBreakdown = FeatureRowsFrom(closureRecord.FeatureBreakdownJson),
                WhenDistribution = PinnedDatesFrom(closureRecord.WhenDistributionJson),
                SelectionMode = closureRecord.SelectionMode,
                Rules = ruleSet.Conditions,
                Mode = ruleSet.Mode,
            };
        }

        private static List<DeliveryFeatureMetricDto> FeatureRowsFrom(string? featureBreakdownJson)
        {
            return ReadPinned<DeliveryFeatureMetricDto>(featureBreakdownJson);
        }

        private static List<WhenDistributionPointDto> PinnedDatesFrom(string? whenDistributionJson)
        {
            return ReadPinned<WhenDistributionPointDto>(whenDistributionJson);
        }

        private static List<T> ReadPinned<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<T>>(json, PinnedJsonReadOptions) ?? [];
        }

        private static WorkItemRuleSet RuleSetFrom(string? ruleDefinitionJson)
        {
            if (string.IsNullOrEmpty(ruleDefinitionJson))
            {
                return new WorkItemRuleSet();
            }

            return WorkItemRuleSetJson.Deserialize(ruleDefinitionJson) ?? new WorkItemRuleSet();
        }

        private static double ProgressOf(DeliveryClosureRecord closureRecord)
        {
            if (closureRecord.TotalWork == 0)
            {
                return 0.0;
            }

            return Math.Clamp((double)closureRecord.DoneWork / closureRecord.TotalWork * 100.0, 0.0, 100.0);
        }

        private static List<string> TeamNamesFrom(string? teamsWithoutForecastJson)
        {
            if (string.IsNullOrEmpty(teamsWithoutForecastJson))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<string>>(teamsWithoutForecastJson, PinnedJsonReadOptions) ?? [];
        }
    }
}
