using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Lighthouse.Backend.MCP
{
    [McpServerToolType]
    public sealed class LighthouseFeatureTools : LighthouseToolsBase
    {
        public LighthouseFeatureTools(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
        {
        }

        [McpServerTool, Description("Get Feature Details by name")]
        public string GetFeatureDetails(string featureName)
        {
            using (var scope = CreateServiceScope())
            {
                var featureRepo = GetServiceFromServiceScope<IRepository<Feature>>(scope);

                var feature = GetFeatureByName(featureName, featureRepo);

                if (feature == null)
                {
                    return $"No feature found with name '{featureName}'";
                }

                return ToJson(new
                {
                    feature.Id,
                    feature.Name,
                    feature.ReferenceId,
                    feature.State,
                    feature.StateCategory,
                    feature.Type,
                    feature.OwningTeam,
                    feature.Url,
                    feature.IsUnparentedFeature,
                    feature.IsParentFeature,
                    feature.IsUsingDefaultFeatureSize,
                    feature.Size,
                    feature.EstimatedSize,
                    feature.IsBlocked,
                    FeatureWork = feature.FeatureWork.Select(fw => new
                    {
                        TeamId = fw.TeamId,
                        TeamName = fw.Team?.Name,
                        fw.RemainingWorkItems,
                        fw.TotalWorkItems
                    }),
                    TotalRemainingWork = feature.FeatureWork.Sum(fw => fw.RemainingWorkItems),
                    TotalWork = feature.FeatureWork.Sum(fw => fw.TotalWorkItems),
                    Projects = feature.Projects.Select(p => new
                    {
                        p.Id,
                        p.Name
                    }),
                    Teams = feature.Teams.Select(t => new
                    {
                        t.Id,
                        t.Name
                    }),
                    feature.Tags,
                    LastUpdated = feature.Forecast?.CreationTime
                });
            }
        }

        [McpServerTool, Description("Get when a feature will be done (forecast with 50/70/85/95 percentile)")]
        public string GetFeatureWhenForecast(string featureName)
        {
            using (var scope = CreateServiceScope())
            {
                var featureRepo = GetServiceFromServiceScope<IRepository<Feature>>(scope);

                var feature = GetFeatureByName(featureName, featureRepo);

                if (feature == null)
                {
                    return $"No feature found with name '{featureName}'";
                }

                if (feature.FeatureWork.Sum(fw => fw.RemainingWorkItems) == 0)
                {
                    return ToJson(new
                    {
                        FeatureName = feature.Name,
                        FeatureId = feature.Id,
                        Status = "Completed",
                        Message = "Feature has no remaining work - already completed",
                        CompletionDate = feature.ClosedDate,
                        TotalWork = feature.FeatureWork.Sum(fw => fw.TotalWorkItems)
                    });
                }

                if (feature.Forecast == null)
                {
                    return ToJson(new
                    {
                        FeatureName = feature.Name,
                        FeatureId = feature.Id,
                        ReferenceId = feature.ReferenceId,
                        Status = "No Forecast Available",
                        Message = "Feature does not have forecast data available",
                        RemainingWork = feature.FeatureWork.Sum(fw => fw.RemainingWorkItems),
                        TotalWork = feature.FeatureWork.Sum(fw => fw.TotalWorkItems)
                    });
                }

                var totalRemainingWork = feature.FeatureWork.Sum(fw => fw.RemainingWorkItems);

                return ToJson(new
                {
                    FeatureName = feature.Name,
                    FeatureId = feature.Id,
                    Status = feature.State,
                    RemainingWork = totalRemainingWork,
                    TotalWork = feature.FeatureWork.Sum(fw => fw.TotalWorkItems),
                    CompletionPercentage = Math.Round((double)(feature.FeatureWork.Sum(fw => fw.TotalWorkItems) - totalRemainingWork) / feature.FeatureWork.Sum(fw => fw.TotalWorkItems) * 100, 1),
                    Forecast = new
                    {
                        DaysToCompletion = new
                        {
                            Probability50 = feature.Forecast.GetProbability(50),
                            Probability70 = feature.Forecast.GetProbability(70),
                            Probability85 = feature.Forecast.GetProbability(85),
                            Probability95 = feature.Forecast.GetProbability(95)
                        },
                        EstimatedCompletionDates = new
                        {
                            Probability50 = DateTime.Today.AddDays(feature.Forecast.GetProbability(50)),
                            Probability70 = DateTime.Today.AddDays(feature.Forecast.GetProbability(70)),
                            Probability85 = DateTime.Today.AddDays(feature.Forecast.GetProbability(85)),
                            Probability95 = DateTime.Today.AddDays(feature.Forecast.GetProbability(95))
                        },
                        ConfidenceIntervals = new
                        {
                            High = $"{feature.Forecast.GetProbability(50)} days (50% confidence)",
                            Medium = $"{feature.Forecast.GetProbability(70)} days (70% confidence)",
                            Low = $"{feature.Forecast.GetProbability(85)} days (85% confidence)",
                            VeryLow = $"{feature.Forecast.GetProbability(95)} days (95% confidence)"
                        },
                        CreatedOn = feature.Forecast.CreationTime,
                    },
                    InvolvedTeams = feature.Teams.Select(t => new
                    {
                        t.Id,
                        t.Name,
                        RemainingWork = feature.FeatureWork.FirstOrDefault(fw => fw.TeamId == t.Id)?.RemainingWorkItems ?? 0
                    }).Where(t => t.RemainingWork > 0),
                    LastUpdated = feature.Forecast?.CreationTime
                });
            }
        }

        private static Feature? GetFeatureByName(string name, IRepository<Feature> featureRepo)
        {
            var feature = featureRepo.GetByPredicate(f => f.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase));

            if (feature == null)
            {
                return null;
            }

            return featureRepo.GetById(feature.Id);
        }
    }
}