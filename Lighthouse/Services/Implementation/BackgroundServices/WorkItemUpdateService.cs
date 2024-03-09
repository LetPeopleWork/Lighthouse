using Lighthouse.Models;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Services.Implementation.BackgroundServices
{
    public class WorkItemUpdateService : UpdateBackgroundServiceBase
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public WorkItemUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<WorkItemUpdateService> logger) : base(configuration, "WorkItems", logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var projectRepository = scope.ServiceProvider.GetRequiredService<IRepository<Project>>();

                foreach (var project in projectRepository.GetAll().ToList())
                {
                    await UpdateWorkItemsForProject(scope, projectRepository, project);
                }
            }
        }

        private async Task UpdateWorkItemsForProject(IServiceScope scope, IRepository<Project> projectRepository, Project project)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - project.ProjectUpdateTime).TotalMinutes;

            if (minutesSinceLastUpdate >= RefreshAfter)
            {
                var workItemCollectorService = scope.ServiceProvider.GetRequiredService<IWorkItemCollectorService>();
                await workItemCollectorService.UpdateFeaturesForProject(project).ConfigureAwait(true);
                await projectRepository.Save();
            }
        }
    }
}
