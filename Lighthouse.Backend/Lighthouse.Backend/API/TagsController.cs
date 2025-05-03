using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly ILogger<TagsController> logger;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Project> projectRepository;

        public TagsController(ILogger<TagsController> logger, IRepository<Team> teamRepository, IRepository<Project> projectRepository)
        {
            this.logger = logger;
            this.teamRepository = teamRepository;
            this.projectRepository = projectRepository;
        }

        [HttpGet]
        public ActionResult<List<string>> GetAllTags()
        {
            logger.LogDebug("Getting All Tags");

            var teams = teamRepository.GetAll();

            var teamTags = teams
                .SelectMany(team => team.Tags)
                .Distinct();

            var projects = projectRepository.GetAll();

            var projectTags = projects
                .SelectMany(project => project.Tags)
                .Distinct();

            var allTags = teamTags.Union(projectTags).Distinct().ToList();

            return Ok(allTags);
        }
    }
}
