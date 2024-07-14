using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IRepository<Project> repository;
        private readonly IWorkItemCollectorService workItemCollectorService;
        private readonly IMonteCarloService monteCarloService;

        public ProjectsController(IRepository<Project> repository, IWorkItemCollectorService workItemCollectorService, IMonteCarloService monteCarloService)
        {
            this.repository = repository;
            this.workItemCollectorService = workItemCollectorService;
            this.monteCarloService = monteCarloService;
        }

        [HttpGet]
        public IEnumerable<ProjectDto> GetProjects()
        {
            var projectDtos = new List<ProjectDto>();

            var allProjects = repository.GetAll();

            foreach (var project in allProjects)
            {
                var projectDto = new ProjectDto(project);
                projectDtos.Add(projectDto);
            }

            return projectDtos;
        }

        [HttpGet("{id}")]
        public ActionResult<ProjectDto> Get(int id)
        {
            var project = repository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            return Ok(new ProjectDto(project));
        }

        [HttpPost("refresh/{id}")]
        public async Task<ActionResult> UpdateFeaturesForProject(int id)
        {
            var project = repository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }

            await workItemCollectorService.UpdateFeaturesForProject(project);
            await repository.Save();
            await monteCarloService.UpdateForecastsForProject(project);

            return Ok(new ProjectDto(project));
        }

        [HttpDelete("{id}")]
        public void DeleteProject(int id)
        {
            repository.Remove(id);
            repository.Save();
        }

    }
}
