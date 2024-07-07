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

        [HttpGet("overview")]
        public IEnumerable<ProjectOverviewDto> GetOverview()
        {
            var projectOverviewDtos = new List<ProjectOverviewDto>();

            var allProjects = repository.GetAll();

            foreach (var project in allProjects)
            {
                var projectOverviewDto = new ProjectOverviewDto();

                projectOverviewDto.Id = project.Id;
                projectOverviewDto.Name = project.Name;
                projectOverviewDto.RemainingWork = project.Features.SelectMany(f => f.RemainingWork).Sum(rw => rw.RemainingWorkItems);
                projectOverviewDto.LastUpdated = project.ProjectUpdateTime;

                foreach (var team in project.InvolvedTeams)
                {
                    projectOverviewDto.InvolvedTeams.Add(new TeamDto { Id = team.Id, Name = team.Name });
                }

                var highestForecastFeature = project.Features
                        .Where(f => f.Forecast != null)
                        .OrderByDescending(f => f.Forecast.GetProbability(85))
                        .FirstOrDefault();

                if (highestForecastFeature != null)
                {
                    projectOverviewDto.Forecasts.AddRange(GetForecastDtosForFeature(highestForecastFeature));
                }

                projectOverviewDtos.Add(projectOverviewDto);
            }

            return projectOverviewDtos;
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return NotFound();
        }

        private IEnumerable<ForecastDto> GetForecastDtosForFeature(Feature feature)
        {
            var forecasts = new List<ForecastDto>
            {
                new ForecastDto { Probability = 50, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(50)) },
                new ForecastDto { Probability = 70, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(70)) },
                new ForecastDto { Probability = 85, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(85)) },
                new ForecastDto { Probability = 95, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(95)) },
            };

            return forecasts;
        }

        private DateTime GetFutureDate(int daysInFuture)
        {
            return DateTime.Today.AddDays(daysInFuture);
        }
    }
}
