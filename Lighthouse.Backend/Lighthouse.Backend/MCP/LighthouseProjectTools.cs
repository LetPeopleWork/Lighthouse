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