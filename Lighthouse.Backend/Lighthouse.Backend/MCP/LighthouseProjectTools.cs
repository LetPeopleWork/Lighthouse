using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using ModelContextProtocol.Server;
using NuGet.Protocol;
using System.ComponentModel;

namespace Lighthouse.Backend.MCP
{
    [McpServerToolType]
    public sealed class LighthouseProjectTools : LighthouseToolsBase
    {
        public LighthouseProjectTools(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
        {
        }

        [McpServerTool, Description("Get a list of all projects configured in Lighthouse")]
        public string GetAllProjects()
        {
            using (var scope = CreateServiceScope())
            {
                var projectRepo = GetServiceFromServiceScope<IRepository<Project>>(scope);

                return projectRepo.GetAll()
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        TeamCount = p.Teams.Count,
                        FeatureCount = p.Features.Count,
                        MilestoneCount = p.Milestones.Count
                    })
                    .ToJson();
            }
        }

        [McpServerTool, Description("Get information for a specific project by name.")]
        public string GetProjectByName(string name)
        {
            using (var scope = CreateServiceScope())
            {
                var projectRepo = GetServiceFromServiceScope<IRepository<Project>>(scope);

                var project = GetProjectByName(name, projectRepo);

                if (project == null)
                {
                    return $"No project found with name {name}";
                }

                return ToJson(new
                {
                    project.Id,
                    project.Name,
                    project.DefaultAmountOfWorkItemsPerFeature,
                    project.OwningTeamId,
                    OwningTeamName = project.OwningTeam?.Name,
                    project.FeatureOwnerField,
                    project.UnparentedItemsQuery,
                    project.SizeEstimateField,
                    project.UsePercentileToCalculateDefaultAmountOfWorkItems,
                    project.PercentileHistoryInDays,
                    project.DefaultWorkItemPercentile,
                    TeamCount = project.Teams.Count,
                    FeatureCount = project.Features.Count,
                    MilestoneCount = project.Milestones.Count,
                    project.UpdateTime
                });
            }
        }

        [McpServerTool, Description("Get features within a specific project")]
        public string GetProjectFeatures(string projectName)
        {
            using (var scope = CreateServiceScope())
            {
                var projectRepo = GetServiceFromServiceScope<IRepository<Project>>(scope);

                var project = GetProjectByName(projectName, projectRepo);
                if (project == null)
                {
                    return $"No project found with name {projectName}";
                }

                return project.Features
                    .Select(f => new
                    {
                        f.Id,
                        f.Name,
                        f.ReferenceId,
                        f.State,
                        f.StateCategory,
                        f.OwningTeam,
                        f.Url,
                        TotalWorkItems = f.FeatureWork.Sum(fw => fw.TotalWorkItems),
                        RemainingWorkItems = f.FeatureWork.Sum(fw => fw.RemainingWorkItems),
                        f.IsUnparentedFeature
                    })
                    .ToJson();
            }
        }

        [McpServerTool, Description("Get teams involved in a specific project")]
        public string GetProjectTeams(string projectName)
        {
            using (var scope = CreateServiceScope())
            {
                var projectRepo = GetServiceFromServiceScope<IRepository<Project>>(scope);

                var project = GetProjectByName(projectName, projectRepo);
                if (project == null)
                {
                    return $"No project found with name {projectName}";
                }

                return project.Teams
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.WorkTrackingSystemConnectionId,
                        IsOwningTeam = project.OwningTeamId == t.Id,
                        t.UpdateTime
                    })
                    .ToJson();
            }
        }

        [McpServerTool, Description("Run a When Forecast for a Project")]
        public string RunProjectWhenForecast(string projectName)
        {
            using (var scope = CreateServiceScope())
            {
                var projectRepo = GetServiceFromServiceScope<IRepository<Project>>(scope);

                var project = GetProjectByName(projectName, projectRepo);
                if (project == null)
                {
                    return $"No project found with name {projectName}";
                }

                if (project.Features.Count == 0)
                {
                    return $"Project {projectName} has no features to forecast";
                }

                // Get all features with remaining work
                var featuresWithWork = project.Features
                    .Where(f => f.FeatureWork.Sum(fw => fw.RemainingWorkItems) > 0)
                    .ToList();

                if (featuresWithWork.Count == 0)
                {
                    return ToJson(new
                    {
                        ProjectName = project.Name,
                        ProjectId = project.Id,
                        Message = "All features in the project are completed",
                        TotalFeatures = project.Features.Count,
                        CompletedFeatures = project.Features.Count,
                        ProjectCompletionStatus = "Complete"
                    });
                }

                // Find the feature that will complete latest by checking the 85th percentile forecast
                // (using 85th percentile as a reasonable confidence level for project planning)
                var latestFeature = featuresWithWork
                    .Where(f => f.Forecast != null)
                    .OrderByDescending(f => f.Forecast.GetProbability(85))
                    .FirstOrDefault();

                if (latestFeature == null)
                {
                    return $"Project {projectName} features do not have forecast data available";
                }

                var totalRemainingWork = featuresWithWork.Sum(f => f.FeatureWork.Sum(fw => fw.RemainingWorkItems));

                return ToJson(new
                {
                    ProjectName = project.Name,
                    ProjectId = project.Id,
                    CriticalPathFeature = new
                    {
                        latestFeature.Id,
                        latestFeature.Name,
                        latestFeature.ReferenceId,
                        RemainingWork = latestFeature.FeatureWork.Sum(fw => fw.RemainingWorkItems)
                    },
                    ProjectForecast = new
                    {
                        Probability50 = latestFeature.Forecast.GetProbability(50),
                        Probability70 = latestFeature.Forecast.GetProbability(70),
                        Probability85 = latestFeature.Forecast.GetProbability(85),
                        Probability95 = latestFeature.Forecast.GetProbability(95)
                    },
                    ConfidenceIntervals = new
                    {
                        High = $"{latestFeature.Forecast.GetProbability(50)} days (50% confidence)",
                        Medium = $"{latestFeature.Forecast.GetProbability(70)} days (70% confidence)",
                        Low = $"{latestFeature.Forecast.GetProbability(85)} days (85% confidence)",
                        VeryLow = $"{latestFeature.Forecast.GetProbability(95)} days (95% confidence)"
                    },
                    EstimatedCompletionDates = new
                    {
                        Probability50 = DateTime.Today.AddDays(latestFeature.Forecast.GetProbability(50)),
                        Probability70 = DateTime.Today.AddDays(latestFeature.Forecast.GetProbability(70)),
                        Probability85 = DateTime.Today.AddDays(latestFeature.Forecast.GetProbability(85)),
                        Probability95 = DateTime.Today.AddDays(latestFeature.Forecast.GetProbability(95))
                    },
                    ProjectSummary = new
                    {
                        TotalFeatures = project.Features.Count,
                        FeaturesWithRemainingWork = featuresWithWork.Count,
                        TotalRemainingWork = totalRemainingWork,
                        InvolvedTeams = project.Teams.Select(t => new { t.Id, t.Name }).ToList()
                    }
                });
            }
        }

        [McpServerTool, Description("Get project milestones with likelihood analysis")]
        public string GetProjectMilestones(string projectName)
        {
            using (var scope = CreateServiceScope())
            {
                var projectRepo = GetServiceFromServiceScope<IRepository<Project>>(scope);

                var project = GetProjectByName(projectName, projectRepo);
                if (project == null)
                {
                    return $"No project found with name {projectName}";
                }

                if (project.Milestones.Count == 0)
                {
                    return $"Project {projectName} has no milestones defined";
                }

                var milestoneAnalysis = project.Milestones
                    .Where(m => m.Date >= DateTime.Today) // Only future milestones
                    .OrderBy(m => m.Date)
                    .Select(milestone =>
                    {
                        // Calculate overall project likelihood for this milestone
                        var featureLikelihoods = project.Features
                            .Where(f => f.FeatureWork.Sum(fw => fw.RemainingWorkItems) > 0)
                            .Select(f => f.GetLikelhoodForDate(milestone.Date))
                            .ToList();

                        var averageLikelihood = featureLikelihoods.Any() ? featureLikelihoods.Average() : 100.0;
                        var minLikelihood = featureLikelihoods.Any() ? featureLikelihoods.Min() : 100.0;
                        var maxLikelihood = featureLikelihoods.Any() ? featureLikelihoods.Max() : 100.0;

                        return new
                        {
                            milestone.Id,
                            milestone.Name,
                            milestone.Date,
                            DaysFromNow = (milestone.Date - DateTime.Today).Days,
                            OverallLikelihood = Math.Round(averageLikelihood, 1),
                            LikelihoodRange = new
                            {
                                Min = Math.Round(minLikelihood, 1),
                                Max = Math.Round(maxLikelihood, 1),
                                Average = Math.Round(averageLikelihood, 1)
                            },
                            RiskAssessment = averageLikelihood switch
                            {
                                >= 80 => "Low Risk - Very likely to be met",
                                >= 60 => "Medium Risk - Likely to be met with some uncertainty",
                                >= 40 => "High Risk - Uncertain, may require intervention",
                                _ => "Very High Risk - Unlikely to be met without significant changes"
                            },
                            FeaturesAnalyzed = featureLikelihoods.Count,
                            RemainingFeatures = project.Features.Count(f => f.FeatureWork.Sum(fw => fw.RemainingWorkItems) > 0)
                        };
                    })
                    .ToList();

                return ToJson(new
                {
                    ProjectName = project.Name,
                    ProjectId = project.Id,
                    TotalMilestones = project.Milestones.Count,
                    FutureMilestones = milestoneAnalysis.Count,
                    OverallProjectHealth = milestoneAnalysis.Any() ? 
                        milestoneAnalysis.Average(m => m.OverallLikelihood) switch
                        {
                            >= 80 => "Healthy - Most milestones likely to be met",
                            >= 60 => "Moderate - Some milestones at risk",
                            >= 40 => "At Risk - Many milestones uncertain",
                            _ => "Critical - Most milestones unlikely to be met"
                        } : "No future milestones to analyze",
                    Milestones = milestoneAnalysis,
                    LastUpdated = project.UpdateTime
                });
            }
        }

        [McpServerTool, Description("Get Flow Metrics of the specified Project in a given time range")]
        public string GetProjectFlowMetrics(string projectName, DateTime? startDate, DateTime? endDate)
        {
            var rangeStart = startDate ?? DateTime.Now.AddDays(-90);
            var rangeEnd = endDate ?? DateTime.Now;

            using (var scope = CreateServiceScope())
            {
                var projectRepo = GetServiceFromServiceScope<IRepository<Project>>(scope);
                var projectMetricsService = GetServiceFromServiceScope<IProjectMetricsService>(scope);

                var project = GetProjectByName(projectName, projectRepo);
                if (project == null)
                {
                    return $"No project found with name {projectName}";
                }

                var cycleTimePercentiles = projectMetricsService.GetCycleTimePercentilesForProject(project, rangeStart, rangeEnd);
                var cycleTimes = projectMetricsService.GetCycleTimeDataForProject(project, rangeStart, rangeEnd).Select(f => f.CycleTime);
                var wip = projectMetricsService.GetFeaturesInProgressOverTimeForProject(project, rangeStart, rangeEnd);
                var throughput = projectMetricsService.GetThroughputForProject(project, rangeStart, rangeEnd);

                return ToJson(new
                {
                    ProjectName = project.Name,
                    ProjectId = project.Id,
                    DateRange = new
                    {
                        StartDate = rangeStart,
                        EndDate = rangeEnd,
                        DaysInRange = (rangeEnd - rangeStart).Days
                    },
                    CycleTimePercentiles = cycleTimePercentiles,
                    CycleTimes = cycleTimes,
                    WorkInProgress = wip,
                    Throughput = throughput,
                    ProjectSummary = new
                    {
                        TotalFeatures = project.Features.Count,
                        ActiveFeatures = project.Features.Count(f => f.StateCategory == StateCategories.Doing),
                        CompletedFeatures = project.Features.Count(f => f.StateCategory == StateCategories.Done),
                        InvolvedTeams = project.Teams.Select(t => new { t.Id, t.Name }).ToList(),
                        LastUpdated = project.UpdateTime
                    }
                });
            }
        }

        private static Project? GetProjectByName(string name, IRepository<Project> projectRepo)
        {
            var project = projectRepo.GetByPredicate(p => p.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase));
            if (project == null)
            {
                return null;
            }

            return projectRepo.GetById(project.Id);
        }
    }
}