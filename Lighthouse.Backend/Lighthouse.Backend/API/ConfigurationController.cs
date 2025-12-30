using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [LicenseGuard(RequirePremium = true)]
    [ApiController]
    public class ConfigurationController(
        ILogger<ConfigurationController> logger,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepo,
        IRepository<Team> teamRepo,
        IRepository<Portfolio> projectRepo)
        : ControllerBase
    {
        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        [HttpGet("export")]
        public IActionResult ExportConfiguration()
        {
            logger.LogInformation("Starting configuration export.");

            var workTrackingSystems = GetWorkTrackingSystems();
            var teams = GetTeams(workTrackingSystems.Select(w => w.Id));
            var projects = GetProjects(workTrackingSystems.Select(w => w.Id));

            var configurationExport = new ConfigurationExport
            {
                WorkTrackingSystems = workTrackingSystems,
                Teams = teams,
                Projects = projects,
            };

            var file = SerializeConfigurationToFile(configurationExport);

            logger.LogInformation("Configuration export completed successfully.");

            return file;
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> DeleteConfiguration()
        {
            logger.LogInformation("Clearing configuration");

            await RemoveAllProjects();
            await RemoveAllTeamsAsync();
            await RemoveAllWorkTrackingSystems();

            return Ok();
        }

        [HttpPost("validate")]
        public ActionResult<ConfigurationValidationDto> ValidateConfiguration([FromBody] ConfigurationExport configurationExport)
        {
            logger.LogInformation("Validating Configuration.");

            var validationResult = new ConfigurationValidationDto(configurationExport);
            ValidateWorkTrackingSystems(validationResult);
            ValidateTeams(configurationExport, validationResult);
            ValidateProjects(configurationExport, validationResult);

            logger.LogInformation("Configuration validation completed successfully.");
            return Ok(validationResult);
        }

        private void ValidateProjects(ConfigurationExport configurationExport, ConfigurationValidationDto validationResult)
        {
            foreach (var project in configurationExport.Projects)
            {
                var (status, id) = GetStatusAndIdForItem(projectRepo, project);

                var errorMessage = string.Empty;
                var workTrackingSystemExists = IsUsingValidWorkTrackingSystem(configurationExport, project.WorkTrackingSystemConnectionId);

                if (!workTrackingSystemExists)
                {
                    errorMessage = "Work Tracking System Not Found";
                    status = ValidationStatus.Error;
                }
                else
                {
                    var linkedTeams = project.InvolvedTeams.Select(t => t.Id).ToList();
                    if (project.OwningTeam != null && !linkedTeams.Contains(project.OwningTeam.Id))
                    {
                        errorMessage = "Owning Team must be involved in the project";
                        status = ValidationStatus.Error;
                    }
                    else
                    {
                        var isValid = ValidateLinkedTeams(configurationExport.Teams, linkedTeams);
                        if (!isValid)
                        {
                            errorMessage = "Involved Team Not Found";
                            status = ValidationStatus.Error;
                        }
                    }
                }

                validationResult.UpdateProject(project.Id, status, errorMessage, id);
            }
        }

        private static bool ValidateLinkedTeams(IEnumerable<TeamSettingDto> teams, List<int> linkedTeams)
        {
            return teams.Any(t => linkedTeams.Contains(t.Id));
        }

        private void ValidateTeams(ConfigurationExport configurationExport, ConfigurationValidationDto validationResult)
        {
            foreach (var team in configurationExport.Teams)
            {
                var (status, id) = GetStatusAndIdForItem(teamRepo, team);
                var errorMessage = string.Empty;

                var workTrackingSystemExists = IsUsingValidWorkTrackingSystem(configurationExport, team.WorkTrackingSystemConnectionId);
                if (!workTrackingSystemExists)
                {
                    errorMessage = "Work Tracking System Not Found";
                    status = ValidationStatus.Error;
                }

                validationResult.UpdateTeam(team.Id, status, errorMessage, id);
            }
        }

        private static bool IsUsingValidWorkTrackingSystem(ConfigurationExport configurationExport, int workTrackingSystemConnectionId)
        {
            return configurationExport.WorkTrackingSystems.Any(wts => wts.Id == workTrackingSystemConnectionId);
        }

        private (ValidationStatus status, int id) GetStatusAndIdForItem<TEntity>(IRepository<TEntity> repository, SettingsOwnerDtoBase item) where TEntity : WorkTrackingSystemOptionsOwner
        {
            var itemCount = repository.GetAllByPredicate(i => i.Name == item.Name).ToList();

            logger.LogDebug("Checking item: {Name}, Existing Count: {Count}", item.Name, itemCount.Count);

            var exists = itemCount.Count == 1;
            var status = exists ? ValidationStatus.Update : ValidationStatus.New;
            var id = exists ? itemCount.Single().Id : item.Id;

            return (status, id);
        }

        private void ValidateWorkTrackingSystems(ConfigurationValidationDto validationResult)
        {
            foreach (var workTrackingSystem in validationResult.WorkTrackingSystems)
            {
                var existingWorkTrackingSystem = workTrackingSystemConnectionRepo.GetAllByPredicate(wts => wts.Name == workTrackingSystem.Name).ToList();

                logger.LogDebug("Checking Work Tracking System: {Name}, Existing Count: {Count}", workTrackingSystem.Name, existingWorkTrackingSystem.Count);

                var exists = existingWorkTrackingSystem.Count == 1;
                workTrackingSystem.Status = exists ? ValidationStatus.Update : ValidationStatus.New;
                workTrackingSystem.Id = exists ? existingWorkTrackingSystem.Single().Id : workTrackingSystem.Id;
            }
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
            var workTrackingSystems = workTrackingSystemConnectionRepo.GetAll();

            return workTrackingSystems.Select(wts => new WorkTrackingSystemConnectionDto(wts)).ToList();
        }

        private List<TeamSettingDto> GetTeams(IEnumerable<int> exportedWorkTrackingSystemIds)
        {
            var teams = teamRepo.GetAll()
                .Where(t => exportedWorkTrackingSystemIds.Contains(t.WorkTrackingSystemConnectionId));

            return teams
                .Select(t => new TeamSettingDto(t))
                .ToList();
        }

        private List<PortfolioSettingDto> GetProjects(IEnumerable<int> exportedWorkTrackingSystemIds)
        {
            var projects = projectRepo.GetAll()
                .Where(p => exportedWorkTrackingSystemIds.Contains(p.WorkTrackingSystemConnectionId));


            return projects
                .Select(p => new PortfolioSettingDto(p))
                .ToList();
        }
    }
}