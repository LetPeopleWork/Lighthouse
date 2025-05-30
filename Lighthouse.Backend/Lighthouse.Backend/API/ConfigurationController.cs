using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly ILogger<ConfigurationController> logger;
        private readonly IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepo;
        private readonly IRepository<Team> teamRepo;
        private readonly IRepository<Project> projectRepo;

        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public ConfigurationController(
            ILogger<ConfigurationController> logger, IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepo, IRepository<Team> teamRepo, IRepository<Project> projectRepo)
        {
            this.logger = logger;
            this.workTrackingSystemConnectionRepo = workTrackingSystemConnectionRepo;
            this.teamRepo = teamRepo;
            this.projectRepo = projectRepo;
        }

        [HttpGet("export")]
        public IActionResult ExportConfiguration()
        {
            logger.LogInformation("Starting configuration export.");

            var configurationExport = new ConfigurationExport
            {
                WorkTrackingSystems = GetWorkTrackingSystems(),
                Teams = GetTeams(),
                Projects = GetProjects(),
            };

            var file = SerializeConfigurationToFile(configurationExport);

            logger.LogInformation("Configuration export completed successfully.");

            return file;
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearConfigurationAsync()
        {
            await RemoveAllProjects();
            await RemoveAllTeamsAsync();
            await RemoveAllWorkTrackingSystems();

            return Ok();
        }

        private async Task RemoveAllWorkTrackingSystems()
        {
            var workTrackingSystems = workTrackingSystemConnectionRepo.GetAll().ToList();
            foreach (var workTrackingSystem in workTrackingSystems)
            {
                workTrackingSystemConnectionRepo.Remove(workTrackingSystem.Id);
            }

            await workTrackingSystemConnectionRepo.Save();
        }

        private async Task RemoveAllTeamsAsync()
        {
            var teams = teamRepo.GetAll().ToList();
            foreach (var team in teams)
            {
                teamRepo.Remove(team.Id);
            }

            await teamRepo.Save();
        }

        private async Task RemoveAllProjects()
        {
            foreach (var project in projectRepo.GetAll().ToList())
            {
                projectRepo.Remove(project.Id);
            }

            await projectRepo.Save();
        }

        private FileContentResult SerializeConfigurationToFile(ConfigurationExport configurationExport)
        {
            var json = JsonSerializer.Serialize(configurationExport, CachedJsonSerializerOptions);
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
            var fileName = $"Lighthouse_Configuration_{DateTime.Now:yyyy.MM.dd}.json";

            return File(fileBytes, "application/json", fileName);
        }

        private List<WorkTrackingSystemConnectionDto> GetWorkTrackingSystems()
        {
            var workTrackingSystemDtos = new List<WorkTrackingSystemConnectionDto>();

            var workTrackingSystems = workTrackingSystemConnectionRepo.GetAll();

            return workTrackingSystems.Select(wts => new WorkTrackingSystemConnectionDto(wts)).ToList();
        }

        private List<TeamSettingDto> GetTeams()
        {
            var teams = teamRepo.GetAll();

            return teams
                .Select(t => new TeamSettingDto(t))
                .ToList();
        }

        private List<ProjectSettingDto> GetProjects()
        {
            var projects = projectRepo.GetAll();
            return projects
                .Select(p => new ProjectSettingDto(p))
                .ToList();
        }
    }
}