using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuggestionsController : ControllerBase
    {
        private readonly ILogger<SuggestionsController> logger;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Project> projectRepository;

        public SuggestionsController(ILogger<SuggestionsController> logger, IRepository<Team> teamRepository, IRepository<Project> projectRepository)
        {
            this.logger = logger;
            this.teamRepository = teamRepository;
            this.projectRepository = projectRepository;
        }

        [HttpGet("tags")]
        public ActionResult<List<string>> GetTags()
        {
            logger.LogDebug("Getting Tag Suggestions");

            var workItemQueryOwners = GetAllWorkItemQueryOwners();

            var tags = workItemQueryOwners
                .SelectMany(x => x.Tags)
                .Distinct();

            return Ok(tags.ToList());
        }

        [HttpGet("workitemtypes/teams")]
        public ActionResult<List<string>> GetWorkItemTypesForTeams()
        {
            logger.LogDebug("Getting Work Item Type Suggestions for Teams");

            var teams = teamRepository.GetAll();

            var workItemTypes = teams
                .SelectMany(x => x.WorkItemTypes)
                .Distinct();

            return Ok(workItemTypes.ToList());
        }

        [HttpGet("workitemtypes/projects")]
        public ActionResult<List<string>> GetWorkItemTypesForProjects()
        {
            logger.LogDebug("Getting Work Item Type Suggestions for Projects");

            var projects = projectRepository.GetAll();

            var workItemTypes = projects
                .SelectMany(x => x.WorkItemTypes)
                .Distinct();

            return Ok(workItemTypes.ToList());
        }

        [HttpGet("states/teams")]
        public ActionResult<StatesCollectionDto> GetStatesForTeams()
        {
            logger.LogDebug("Getting States Suggestions for Teams");

            var teams = teamRepository.GetAll();

            var statesCollection = new StatesCollectionDto
            {
                ToDoStates = teams.SelectMany(x => x.ToDoStates).Distinct().ToList(),
                DoingStates = teams.SelectMany(x => x.DoingStates).Distinct().ToList(),
                DoneStates = teams.SelectMany(x => x.DoneStates).Distinct().ToList()
            };

            return Ok(statesCollection);
        }

        [HttpGet("states/projects")]
        public ActionResult<StatesCollectionDto> GetStatesForProjects()
        {
            logger.LogDebug("Getting States Suggestions for Projects");

            var projects = projectRepository.GetAll();

            var statesCollection = new StatesCollectionDto
            {
                ToDoStates = projects.SelectMany(x => x.ToDoStates).Distinct().ToList(),
                DoingStates = projects.SelectMany(x => x.DoingStates).Distinct().ToList(),
                DoneStates = projects.SelectMany(x => x.DoneStates).Distinct().ToList()
            };

            return Ok(statesCollection);
        }

        private List<IWorkItemQueryOwner> GetAllWorkItemQueryOwners()
        {
            var workItemQueryOwners = new List<IWorkItemQueryOwner>();

            var teams = teamRepository.GetAll();
            var projects = projectRepository.GetAll();

            workItemQueryOwners.AddRange(teams);
            workItemQueryOwners.AddRange(projects);

            return workItemQueryOwners;
        }
    }
}
