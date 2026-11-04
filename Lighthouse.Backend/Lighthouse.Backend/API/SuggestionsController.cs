using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    [CheckedInHandler("Each action offers suggestions only from the Teams or Portfolios the caller may read.")]
    public class SuggestionsController : ControllerBase
    {
        private readonly ILogger<SuggestionsController> logger;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IRbacAdministrationService rbacAdministrationService;

        public SuggestionsController(
            ILogger<SuggestionsController> logger,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IRbacAdministrationService rbacAdministrationService)
        {
            this.logger = logger;
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.rbacAdministrationService = rbacAdministrationService;
        }

        [HttpGet("workitemtypes/teams")]
        public async Task<ActionResult<List<string>>> GetWorkItemTypesForTeams()
        {
            logger.LogDebug("Getting Work Item Type Suggestions for Teams");

            var teams = await GetReadableTeams().ConfigureAwait(false);

            var workItemTypes = teams
                .SelectMany(x => x.WorkItemTypes)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return Ok(workItemTypes.ToList());
        }

        [HttpGet("workitemtypes/projects")]
        public async Task<ActionResult<List<string>>> GetWorkItemTypesForProjects()
        {
            logger.LogDebug("Getting Work Item Type Suggestions for Projects");

            var projects = await GetReadablePortfolios().ConfigureAwait(false);

            var workItemTypes = projects
                .SelectMany(x => x.WorkItemTypes)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return Ok(workItemTypes.ToList());
        }

        [HttpGet("states/teams")]
        public async Task<ActionResult<StatesCollectionDto>> GetStatesForTeams()
        {
            logger.LogDebug("Getting States Suggestions for Teams");

            var teams = await GetReadableTeams().ConfigureAwait(false);

            var statesCollection = new StatesCollectionDto
            {
                ToDoStates = teams.SelectMany(x => x.NormalisedStatesFor(x.ToDoStates)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                DoingStates = teams.SelectMany(x => x.NormalisedStatesFor(x.DoingStates)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                DoneStates = teams.SelectMany(x => x.NormalisedStatesFor(x.DoneStates)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };

            return Ok(statesCollection);
        }

        [HttpGet("states/projects")]
        public async Task<ActionResult<StatesCollectionDto>> GetStatesForProjects()
        {
            logger.LogDebug("Getting States Suggestions for Projects");

            var projects = await GetReadablePortfolios().ConfigureAwait(false);

            var statesCollection = new StatesCollectionDto
            {
                ToDoStates = projects.SelectMany(x => x.NormalisedStatesFor(x.ToDoStates)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                DoingStates = projects.SelectMany(x => x.NormalisedStatesFor(x.DoingStates)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                DoneStates = projects.SelectMany(x => x.NormalisedStatesFor(x.DoneStates)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };

            return Ok(statesCollection);
        }

        private async Task<IEnumerable<Team>> GetReadableTeams()
        {
            var teams = teamRepository.GetAll().ToList();
            var teamIds = teams.Select(t => t.Id).ToArray();
            var readableTeamIds = await rbacAdministrationService
                .GetReadableTeamIdsAsync(User, teamIds, HttpContext?.RequestAborted ?? default)
                .ConfigureAwait(false);

            var readableTeamIdSet = (readableTeamIds ?? teamIds)
                .ToHashSet();

            return teams.Where(t => readableTeamIdSet.Contains(t.Id)).OrderBy(t => t.Id);
        }

        private async Task<IEnumerable<Portfolio>> GetReadablePortfolios()
        {
            var portfolios = portfolioRepository.GetAll().ToList();
            var portfolioIds = portfolios.Select(p => p.Id).ToArray();
            var readablePortfolioIds = await rbacAdministrationService
                .GetReadablePortfolioIdsAsync(User, portfolioIds, HttpContext?.RequestAborted ?? default)
                .ConfigureAwait(false);
            var readablePortfolioIdSet = (readablePortfolioIds ?? portfolioIds)
                .ToHashSet();

            return portfolios.Where(p => readablePortfolioIdSet.Contains(p.Id)).OrderBy(p => p.Id);
        }
    }
}
