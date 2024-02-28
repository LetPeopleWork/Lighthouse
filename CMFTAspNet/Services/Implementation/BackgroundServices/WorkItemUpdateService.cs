

using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation.BackgroundServices
{
    public class WorkItemUpdateService : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<WorkItemUpdateService> logger;

        public WorkItemUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<WorkItemUpdateService> logger)
        {
            this.configuration = configuration;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await TryUpdatingWorkItems(stoppingToken);
            }
        }

        private async Task TryUpdatingWorkItems(CancellationToken stoppingToken)
        {
            try
            {
                await UpdateWorkItemForAllProjects(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }

        private async Task UpdateWorkItemForAllProjects(CancellationToken stoppingToken)
        {
            var workItemUpdateInterval = configuration.GetValue<int>("PeriodicRefresh:WorkItems:Interval");

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var projectRepository = scope.ServiceProvider.GetRequiredService<IRepository<Project>>();

                foreach (var project in projectRepository.GetAll().ToList())
                {
                    await UpdateWorkItemsForProject(scope, projectRepository, project);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(workItemUpdateInterval), stoppingToken);
        }

        private async Task UpdateWorkItemsForProject(IServiceScope scope, IRepository<Project> projectRepository, Project project)
        {
            var workItemsRefreshAfter = configuration.GetValue<int>("PeriodicRefresh:WorkItems:RefreshAfter");

            var minutesSinceLastUpdate = (DateTime.Now - project.ProjectUpdateTime).TotalMinutes;

            if (minutesSinceLastUpdate >= workItemsRefreshAfter)
            {
                var workItemCollectorService = scope.ServiceProvider.GetRequiredService<IWorkItemCollectorService>();
                await workItemCollectorService.UpdateFeaturesForProject(project).ConfigureAwait(true);
                await projectRepository.Save();
            }
        }
    }
}
