using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class ProjectMetricsService : BaseMetricsService, IProjectMetricsService
    {
        private readonly string inProgressFeaturesMetricIdentifier = "InProgressFeatures";

        private readonly ILogger<ProjectMetricsService> logger;
        private readonly IRepository<Feature> featureRepository;

        public ProjectMetricsService(
            ILogger<ProjectMetricsService> logger,
            IRepository<Feature> featureRepository,
            IAppSettingService appSettingService,
            IServiceProvider serviceProvider)
            : base(appSettingService.GetFeaturRefreshSettings().Interval, serviceProvider)
        {
            this.logger = logger;
            this.featureRepository = featureRepository;
        }

        public RunChartData GetThroughputForProject(Project project, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput for Project {ProjectName} between {StartDate} and {EndDate}", project.Name, startDate.Date, endDate.Date);

            var projectFeatures = featureRepository.GetAllByPredicate(f =>
                f.Projects.Any(p => p.Id == project.Id) &&
                f.StateCategory == StateCategories.Done);

            var throughputByDay = GenerateThroughputRunChart(
                startDate,
                endDate,
                projectFeatures);

            logger.LogDebug("Finished calculating Throughput for Project {ProjectName}", project.Name);

            return new RunChartData(throughputByDay);
        }

        public RunChartData GetFeaturesInProgressOverTimeForProject(Project project, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Features In Progress Over Time for Project {ProjectName} between {StartDate} and {EndDate}", project.Name, startDate.Date, endDate.Date);

            var features = featureRepository.GetAllByPredicate(f =>
                    f.Projects.Any(p => p.Id == project.Id) &&
                    (f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done))
                .ToList();

            var wipOverTime = GenerateWorkInProgressByDay(
                startDate,
                endDate,
                features);

            logger.LogDebug("Finished calculating Features In Progress Over Time for Project {ProjectName}", project.Name);

            return new RunChartData(wipOverTime);
        }

        public RunChartData GetStartedItemsForProject(Project project, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Started Items for Project {ProjectName} between {StartDate} and {EndDate}", project.Name, startDate.Date, endDate.Date);

            var startedItems = featureRepository.GetAllByPredicate(f => f.Projects.Any(p => p.Id == project.Id) && (f.StateCategory == StateCategories.Done || f.StateCategory == StateCategories.Doing));
            var startedItemsByDay = GenerateStartedRunChart(startDate, endDate, startedItems);

            var throughput = new RunChartData(startedItemsByDay);

            return throughput;
        }

        public ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForProject(Project project, DateTime startDate, DateTime endDate)
        {
            var throughput = GetThroughputForProject(project, startDate, endDate);
            return GetMultiItemForecastPredictabilityScore(throughput, startDate, endDate);
        }

        public IEnumerable<Feature> GetInProgressFeaturesForProject(Project project)
        {
            logger.LogDebug("Getting In Progress Features for Project {ProjectName}", project.Name);

            return GetFromCacheIfExists<IEnumerable<Feature>, Project>(project, inProgressFeaturesMetricIdentifier, () =>
            {
                var features = featureRepository.GetAllByPredicate(f =>
                    f.Projects.Any(p => p.Id == project.Id) &&
                    f.StateCategory == StateCategories.Doing)
                    .ToList();

                logger.LogDebug("Found {FeatureCount} In Progress Features for Project {ProjectName}", features.Count, project.Name);
                return features;
            }, logger);
        }

        public IEnumerable<PercentileValue> GetCycleTimePercentilesForProject(Project project, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Percentiles for Project {ProjectName} between {StartDate} and {EndDate}", project.Name, startDate.Date, endDate.Date);

            var closedFeaturesInDateRange = GetFeaturesClosedInDateRange(project, startDate, endDate);
            var cycleTimes = closedFeaturesInDateRange.Select(f => f.CycleTime).Where(ct => ct > 0).ToList();

            if (cycleTimes.Count == 0)
            {
                logger.LogDebug("No closed features found in the specified date range for Project {ProjectName}", project.Name);
                return [];
            }

            return [
                new PercentileValue(50, PercentileCalculator.CalculatePercentile(cycleTimes, 50)),
                new PercentileValue(70, PercentileCalculator.CalculatePercentile(cycleTimes, 70)),
                new PercentileValue(85, PercentileCalculator.CalculatePercentile(cycleTimes, 85)),
                new PercentileValue(95, PercentileCalculator.CalculatePercentile(cycleTimes, 95))
            ];
        }

        public IEnumerable<Feature> GetCycleTimeDataForProject(Project project, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Data for Project {ProjectName} between {StartDate} and {EndDate}", project.Name, startDate.Date, endDate.Date);

            return GetFeaturesClosedInDateRange(project, startDate, endDate).ToList();
        }

        public IEnumerable<PercentileValue> GetSizePercentilesForProject(Project project, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Size Percentiles for Project {ProjectName} between {StartDate} and {EndDate}", project.Name, startDate.Date, endDate.Date);

            var closedFeaturesInDateRange = GetFeaturesClosedInDateRange(project, startDate, endDate);
            var sizes = closedFeaturesInDateRange.Select(f => f.Size).Where(s => s > 0).ToList();

            if (sizes.Count == 0)
            {
                logger.LogDebug("No closed features found in the specified date range for Project {ProjectName}", project.Name);
                return [];
            }

            return [
                new PercentileValue(50, PercentileCalculator.CalculatePercentile(sizes, 50)),
                new PercentileValue(70, PercentileCalculator.CalculatePercentile(sizes, 70)),
                new PercentileValue(85, PercentileCalculator.CalculatePercentile(sizes, 85)),
                new PercentileValue(95, PercentileCalculator.CalculatePercentile(sizes, 95))
            ];
        }

        public void InvalidateProjectMetrics(Project project)
        {
            InvalidateMetrics(project, logger);
        }

        private IEnumerable<Feature> GetFeaturesClosedInDateRange(Project project, DateTime startDate, DateTime endDate)
        {
            var closedFeaturesOfProject = featureRepository.GetAllByPredicate(f =>
                    f.Projects.Any(p => p.Id == project.Id) &&
                    f.StateCategory == StateCategories.Done)    
                .ToList();

            return closedFeaturesOfProject
                .Where(f => f.ClosedDate.HasValue &&
                           f.ClosedDate.Value.Date >= startDate.Date &&
                           f.ClosedDate.Value.Date <= endDate.Date);
        }
    }
}