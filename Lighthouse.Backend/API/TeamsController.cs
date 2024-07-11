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

            var allTeams = teamRepository.GetAll().ToList();
            var allProjects = projectRepository.GetAll().ToList();
            var allFeatures = featureRepository.GetAll().ToList();

            foreach (var team in allTeams)
            {
                var teamDto = new TeamDto(team);

                var teamProjects = allProjects.Where(p => p.InvolvedTeams.Any(t => t.Id == team.Id)).ToList();

                var features = new List<Feature>();

                foreach (var feature in allFeatures)
                {
                    if (feature.RemainingWork.Any(rw => rw.TeamId == team.Id))
                    {
                        features.Add(feature);
                    }
                }

                teamDto.Projects.AddRange(teamProjects.Select(p => new ProjectDto(p)));
                teamDto.Features.AddRange(features.Select(f => new FeatureDto(f)));

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
