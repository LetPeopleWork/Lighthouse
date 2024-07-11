using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IRepository<Project> repository;

        public ProjectsController(IRepository<Project> repository)
        {
            this.repository = repository;
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
        public IActionResult Get(int id)
        {
            var project = repository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }

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
