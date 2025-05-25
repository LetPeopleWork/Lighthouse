using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

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

        private FileContentResult SerializeConfigurationToFile(ConfigurationExport configurationExport)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = System.Text.Json.JsonSerializer.Serialize(configurationExport, options);
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