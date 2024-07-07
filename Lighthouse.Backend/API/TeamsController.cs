using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<Feature> featureRepository;

        public TeamsController(IRepository<Team> teamRepository, IRepository<Project> projectRepository, IRepository<Feature> featureRepository)
        {
            this.teamRepository = teamRepository;
            this.projectRepository = projectRepository;
            this.featureRepository = featureRepository;
        }

        [HttpGet]
        public IEnumerable<TeamDto> GetTeams()
        {
            var teamDtos = new List<TeamDto>();

            var allTeams = teamRepository.GetAll();

            foreach (var team in allTeams)
            {
                var teamDto = new TeamDto();

                var teamProjects = projectRepository.GetAll().Where(p => p.InvolvedTeams.Any(t => t.Id == team.Id)).ToList();
                var remainingWorkPerFeature = featureRepository.GetAll().SelectMany(f => f.RemainingWork).Where(rw => rw.Team.Id == team.Id).ToList();

                teamDto.Name = team.Name;
                teamDto.Id = team.Id;
                teamDto.Projects = teamProjects.Count;
                teamDto.Features = remainingWorkPerFeature.Count;
                teamDto.RemainingWork = remainingWorkPerFeature.Sum(rw => rw.RemainingWorkItems);

                teamDtos.Add(teamDto);
            }

            return teamDtos;
        }

        [HttpDelete("{id}")]
        public void DeleteTeam(int id)
        {
            teamRepository.Remove(id);
            teamRepository.Save();
        }
    }
}
