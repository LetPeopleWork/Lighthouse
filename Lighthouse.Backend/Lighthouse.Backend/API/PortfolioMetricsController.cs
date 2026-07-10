using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/portfolios/{portfolioId:int}/metrics")]
    [Route("api/latest/portfolios/{portfolioId:int}/metrics")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
    public class PortfolioMetricsController(
        IRepository<Portfolio> portfolioRepository,
        IPortfolioMetricsService portfolioMetricsService,
        IBlackoutPeriodService blackoutPeriodService,
        IBlockedCountSnapshotRepository blockedCountSnapshotRepository,
        IBlockedItemService blockedItemService,
        IWorkItemBlockedTransitionRepository workItemBlockedTransitionRepository,
        ILogger<PortfolioMetricsController> logger)
        : ControllerBase
    {
        private const string StartDateMustBeBeforeEndDateErrorMessage = "Start date must be before end date.";
        private const string StateMustNotBeEmptyErrorMessage = "State must not be empty.";

        [HttpGet("throughput")]
        public ActionResult<RunChartData> GetThroughput(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetThroughputForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("started")]
        public ActionResult<RunChartData> GetStartedItems(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetStartedItemsForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("arrivals")]
        public ActionResult<RunChartData> GetArrivals(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetArrivalsForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("wipOverTime")]
        public ActionResult<RunChartData> GetFeaturesInProgressOverTime(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetFeaturesInProgressOverTimeForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("wip")]
        public ActionResult<IEnumerable<FeatureDto>> GetInProgressFeatures(int portfolioId, [FromQuery] DateTime asOfDate)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var features = portfolioMetricsService.GetInProgressFeaturesForPortfolio(portfolio, asOfDate).ToList();
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                    DateTime.UtcNow.Date, FeatureForecastWindow.EndFor(features));
                return features.Select(f => new FeatureDto(f, blackoutPeriods));
            });
        }

        [HttpGet("cycleTimePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetCycleTimePercentiles(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int? definitionId = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTimePercentiles", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                IsNamedRequest(definitionId)
                    ? portfolioMetricsService.GetNamedCycleTimePercentilesForPortfolio(portfolio, startDate, endDate, definitionId!.Value)
                    : portfolioMetricsService.GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("workItemAgePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetWorkItemAgePercentiles(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("workItemAgePercentiles", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetWorkItemAgePercentilesForPortfolio(portfolio, endDate));
        }

        [HttpGet("ageInStatePercentiles")]
        public ActionResult<IEnumerable<AgeInStatePercentilesDto>> GetAgeInStatePercentiles(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("ageInStatePercentiles", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetAgeInStatePercentilesForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("cumulativeStateTime")]
        public ActionResult<CumulativeStateTimeDto> GetCumulativeStateTime(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int[]? itemIds = null, [FromQuery] int? definitionId = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cumulativeStateTime", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetCumulativeStateTimeForPortfolio(portfolio, startDate, endDate, itemIds, definitionId));
        }

        [HttpGet("cumulativeStateTime/items")]
        public ActionResult<CumulativeStateTimeItemsDto> GetCumulativeStateTimeItems(int portfolioId, [FromQuery] string? state, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int[]? itemIds = null)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return BadRequest(StateMustNotBeEmptyErrorMessage);
            }

            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cumulativeStateTime/items", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetCumulativeStateTimeItemsForPortfolio(portfolio, state, startDate, endDate, itemIds));
        }

        [HttpGet("cumulativeStateTime/candidates")]
        public ActionResult<CumulativeStateTimeCandidatesDto> GetCumulativeStateTimeCandidates(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cumulativeStateTime/candidates", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetCumulativeStateTimeCandidatesForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("cycleTimeData")]
        public ActionResult<IEnumerable<FeatureDto>> GetCycleTimeData(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTimeData", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetNamedCycleTimeDataForPortfolio(portfolio, startDate, endDate);
                var features = data.Select(entry => entry.Feature).ToList();
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                    DateTime.UtcNow.Date, FeatureForecastWindow.EndFor(features));
                return data.Select(entry => new FeatureDto(entry.Feature, blackoutPeriods, namedCycleTimes: entry.NamedCycleTimes));
            });
        }

        [HttpGet("allFeaturesForSizeChart")]
        public ActionResult<IEnumerable<FeatureDto>> GetAllFeaturesForSizeChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var features = portfolioMetricsService.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                    DateTime.UtcNow.Date, FeatureForecastWindow.EndFor(features));
                return features.Select(f => new FeatureDto(f, blackoutPeriods));
            });
        }

        [HttpGet("sizePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetSizePercentiles(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetSizePercentilesForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("multiitemforecastpredictabilityscore")]
        public ActionResult<ForecastPredictabilityScore> GetMultiItemForecastPredictabilityScore(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, portfolio => portfolioMetricsService.GetMultiItemForecastPredictabilityScoreForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("totalWorkItemAge")]
        public ActionResult<int> GetTotalWorkItemAge(int portfolioId, [FromQuery] DateTime asOfDate)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetTotalWorkItemAge(portfolio, asOfDate));
        }

        [HttpGet("throughputInfo")]
        public ActionResult<ThroughputInfoDto> GetThroughputInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetThroughputInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("arrivalsInfo")]
        public ActionResult<ArrivalsInfoDto> GetArrivalsInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetArrivalsInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("featureSizePercentilesInfo")]
        public ActionResult<FeatureSizePercentilesInfoDto> GetFeatureSizePercentilesInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetFeatureSizePercentilesInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("wipOverviewInfo")]
        public ActionResult<WipOverviewInfoDto> GetWipOverviewInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetWipOverviewInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("flowEfficiencyInfo")]
        public ActionResult<FlowEfficiencyInfoDto> GetFlowEfficiencyInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetFlowEfficiencyInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("totalWorkItemAgeInfo")]
        public ActionResult<TotalWorkItemAgeInfoDto> GetTotalWorkItemAgeInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetTotalWorkItemAgeInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("predictabilityScoreInfo")]
        public ActionResult<PredictabilityScoreInfoDto> GetPredictabilityScoreInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetPredictabilityScoreInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("cycleTimePercentilesInfo")]
        public ActionResult<CycleTimePercentilesInfoDto> GetCycleTimePercentilesInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetCycleTimePercentilesInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("throughput/pbc")]
        public ActionResult<ProcessBehaviourChart> GetThroughputProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetThroughputProcessBehaviourChart(portfolio, startDate, endDate), startDate, endDate));
        }

        [HttpGet("arrivals/pbc")]
        public ActionResult<ProcessBehaviourChart> GetArrivalsProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetArrivalsProcessBehaviourChart(portfolio, startDate, endDate), startDate, endDate));
        }

        [HttpGet("wipOverTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetWipProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetWipProcessBehaviourChart(portfolio, startDate, endDate), startDate, endDate));
        }

        [HttpGet("totalWorkItemAge/pbc")]
        public ActionResult<ProcessBehaviourChart> GetTotalWorkItemAgeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(portfolio, startDate, endDate), startDate, endDate));
        }

        [HttpGet("cycleTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetCycleTimeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTime/pbc", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetCycleTimeProcessBehaviourChart(portfolio, startDate, endDate), startDate, endDate));
        }

        [HttpGet("featureSize/pbc")]
        public ActionResult<ProcessBehaviourChart> GetFeatureSizeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetFeatureSizeProcessBehaviourChart(portfolio, startDate, endDate), startDate, endDate));
        }

        [HttpGet("estimationVsCycleTime")]
        public ActionResult<EstimationVsCycleTimeResponse> GetEstimationVsCycleTimeData(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetEstimationVsCycleTimeData(portfolio, startDate, endDate));
        }

        [HttpGet("featureSizeEstimation")]
        public ActionResult<FeatureSizeEstimationResponse> GetFeatureSizeEstimationData(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetFeatureSizeEstimationData(portfolio, startDate, endDate));
        }

        [HttpGet("blockedCountHistory")]
        public ActionResult<IEnumerable<BlockedCountSnapshotDto>> GetBlockedCountHistory(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var start = DateOnly.FromDateTime(startDate.Date);
                var end = DateOnly.FromDateTime(endDate.Date);
                return blockedCountSnapshotRepository
                    .GetAllByPredicate(s => s.OwnerId == portfolioId && s.OwnerType == OwnerType.Portfolio
                                            && s.RecordedAt >= start && s.RecordedAt <= end)
                    .OrderBy(s => s.RecordedAt)
                    .AsEnumerable()
                    .Select(s => new BlockedCountSnapshotDto
                    {
                        RecordedAt = s.RecordedAt.ToString("yyyy-MM-dd"),
                        BlockedCount = s.BlockedCount,
                    });
            });
        }

        [HttpGet("blockedItemsAtDate")]
        public ActionResult<IEnumerable<WorkItemDto>> GetBlockedItemsAtDate(int portfolioId, [FromQuery] DateTime date)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var targetDate = DateOnly.FromDateTime(date.Date);
                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

                if (targetDate >= today)
                {
                    return portfolioMetricsService
                        .GetBlockedEligibleFeaturesForPortfolio(portfolio)
                        .Where(f => blockedItemService.IsBlocked(f, portfolio))
                        .Select(f => new WorkItemDto(f, isBlocked: true, [], f.CurrentStateEnteredAt));
                }

                var blockedIds = workItemBlockedTransitionRepository.GetBlockedWorkItemIdsAt(targetDate).ToHashSet();
                var reconstructed = portfolio.Features
                    .Where(f => blockedIds.Contains(f.Id))
                    .Select(f => new WorkItemDto(f, isBlocked: true, [], null))
                    .ToList();

                ReconcileReconstructedCountWithSnapshot(portfolioId, OwnerType.Portfolio, targetDate, reconstructed.Count);
                AnnotateCaptureCompleteness(targetDate, portfolio.Features.Select(f => f.Id).ToList());

                return reconstructed;
            });
        }

        private const string ReconstructionCompleteFromHeader = "X-Blocked-Reconstruction-Complete-From";

        // Reconciliation guard (ADR-099): where a captured BlockedCountSnapshot exists for the requested date,
        // compare it against the interval-reconstructed membership count. A divergence signals a
        // transition-capture gap and must never be silent — surface it as a structured warning.
        private void ReconcileReconstructedCountWithSnapshot(int ownerId, OwnerType ownerType, DateOnly targetDate, int reconstructedCount)
        {
            var snapshot = blockedCountSnapshotRepository.GetByPredicate(
                s => s.OwnerId == ownerId && s.OwnerType == ownerType && s.RecordedAt == targetDate);

            if (snapshot is null || snapshot.BlockedCount == reconstructedCount)
            {
                return;
            }

            logger.LogWarning(
                "Blocked-membership reconstruction for {OwnerType} {OwnerId} at {Date:yyyy-MM-dd} diverged from the captured snapshot (reconstructed {ReconstructedCount}, snapshot {SnapshotCount}); a transition-capture gap is likely.",
                ownerType, ownerId, targetDate, reconstructedCount, snapshot.BlockedCount);
        }

        // Capture-completeness note: reconstruction is only complete from the earliest captured transition
        // onwards. When the requested date predates that point the returned set may be partial, so advertise
        // the completeness boundary out-of-band via a response header (the body stays a bare WorkItemDto[]).
        private void AnnotateCaptureCompleteness(DateOnly targetDate, IReadOnlyCollection<int> ownerWorkItemIds)
        {
            var earliestCapture = workItemBlockedTransitionRepository.GetAll()
                .Where(t => ownerWorkItemIds.Contains(t.WorkItemId))
                .Select(t => (DateTime?)t.EnteredAt)
                .Min();

            if (earliestCapture is null)
            {
                return;
            }

            var captureStartDate = DateOnly.FromDateTime(earliestCapture.Value);
            if (targetDate < captureStartDate)
            {
                Response.Headers[ReconstructionCompleteFromHeader] = captureStartDate.ToString("yyyy-MM-dd");
            }
        }

        private int[] GetBlackoutDayIndicesArray(DateTime startDate, DateTime endDate)
        {
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(startDate, endDate);
            return blackoutPeriods.GetBlackoutDayIndices(startDate, endDate).OrderBy(i => i).ToArray();
        }

        private ProcessBehaviourChart AnnotateBlackoutDays(ProcessBehaviourChart chart, DateTime startDate, DateTime endDate)
        {
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(startDate, endDate);
            return blackoutPeriods.AnnotateBlackoutDays(chart);
        }

        private void LogDateBoundaries(string endpoint, int portfolioId, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Metrics request {Endpoint} for portfolio {PortfolioId}: startDate={StartDate:yyyy-MM-dd} endDate={EndDate:yyyy-MM-dd} (Kind={StartKind}/{EndKind})",
                endpoint, portfolioId, startDate, endDate, startDate.Kind, endDate.Kind);
        }

        private static bool IsNamedRequest(int? definitionId) => definitionId is > 0;
    }
}