using Lighthouse.Models;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Services.Implementation.BackgroundServices
{
    public class WorkItemUpdateService : UpdateBackgroundServiceBase
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<WorkItemUpdateService> logger;

        public WorkItemUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<WorkItemUpdateService> logger) : base(configuration, "WorkItems", logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Starting Update of Work Items for Projects");

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var projectRepository = scope.ServiceProvider.GetRequiredService<IRepository<Project>>();

                foreach (var project in projectRepository.GetAll().ToList())
                {
                    logger.LogInformation($"Checking Work Items for Project {project.Name}");
                    await UpdateWorkItemsForProject(scope, projectRepository, project);
                }
            }

            logger.LogInformation($"Done Updating of Work Items for Projects");
        }

        private async Task UpdateWorkItemsForProject(IServiceScope scope, IRepository<Project> projectRepository, Project project)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - project.ProjectUpdateTime).TotalMinutes;

            logger.LogInformation($"Last Refresh of Work Items for Project {project.Name} was {minutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes");

            if (minutesSinceLastUpdate >= RefreshAfter)
            {
                var workItemCollectorService = scope.ServiceProvider.GetRequiredService<IWorkItemCollectorService>();
                await workItemCollectorService.UpdateFeaturesForProject(project).ConfigureAwait(true);
                await projectRepository.Save();
            }
        }
    }
}
