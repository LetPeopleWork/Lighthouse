using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public sealed record ForecastThroughputStatus(RunChartData Throughput, bool FilterApplied, string? ExcludedSummary);

    public interface ITeamMetricsService
    {
        RunChartData GetCurrentThroughputForTeamForecast(Team team, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting);

        ForecastThroughputStatus GetForecastThroughputStatus(Team team, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting);

        ProcessBehaviourChart GetThroughputProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetThroughputProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode);

        ProcessBehaviourChart GetWipProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetTotalWorkItemAgeProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetCycleTimeProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate);

        RunChartData GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate);

        RunChartData GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode);

        RunChartData GetBlackoutAwareThroughputForTeam(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting);

        RunChartData GetStartedItemsForTeam(Team team, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetArrivalsProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate);

        RunChartData GetCreatedItemsForTeam(Team team, IEnumerable<string> workItemTypes, DateTime startDate, DateTime endDate);

        RunChartData GetWorkInProgressOverTimeForTeam(Team team, DateTime startDate, DateTime endDate);

        ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForTeam(Team team, DateTime startDate, DateTime endDate);

        ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForTeam(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode);

        IEnumerable<Feature> GetCurrentFeaturesInProgressForTeam(Team team, DateTime asOfDate);

        IEnumerable<WorkItem> GetWipSnapshotForTeam(Team team, DateTime endDate);

        ThroughputInfoDto GetThroughputInfoForTeam(Team team, DateTime startDate, DateTime endDate);

        ArrivalsInfoDto GetArrivalsInfoForTeam(Team team, DateTime startDate, DateTime endDate);

        ForecastInputCandidatesDto GetForecastInputCandidates(Team team);

        IEnumerable<WorkItem> GetClosedItemsForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForTeam(Team team, DateTime startDate, DateTime endDate);

        EstimationVsCycleTimeResponse GetEstimationVsCycleTimeData(Team team, DateTime startDate, DateTime endDate);

        int GetTotalWorkItemAge(Team team, DateTime endDate);

        WipOverviewInfoDto GetWipOverviewInfoForTeam(Team team, DateTime startDate, DateTime endDate);

        FeaturesWorkedOnInfoDto GetFeaturesWorkedOnInfoForTeam(Team team, DateTime startDate, DateTime endDate);

        TotalWorkItemAgeInfoDto GetTotalWorkItemAgeInfoForTeam(Team team, DateTime startDate, DateTime endDate);

        PredictabilityScoreInfoDto GetPredictabilityScoreInfoForTeam(Team team, DateTime startDate, DateTime endDate);

        CycleTimePercentilesInfoDto GetCycleTimePercentilesInfoForTeam(Team team, DateTime startDate, DateTime endDate);

        Task UpdateTeamMetrics(Team team);
    }
}
