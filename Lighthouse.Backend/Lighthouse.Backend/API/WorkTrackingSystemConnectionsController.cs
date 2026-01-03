using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkTrackingSystemConnectionsController : ControllerBase
    {
        private readonly IWorkTrackingSystemFactory workTrackingSystemFactory;
        private readonly IRepository<WorkTrackingSystemConnection> repository;
        private readonly IWorkTrackingConnectorFactory workTrackingConnectorFactory;
        private readonly ICryptoService cryptoService;

        private readonly bool isLinearIntegrationEnabled;

        public WorkTrackingSystemConnectionsController(
            IWorkTrackingSystemFactory workTrackingSystemFactory, IRepository<WorkTrackingSystemConnection> repository, IWorkTrackingConnectorFactory workTrackingConnectorFactory, ICryptoService cryptoService, IRepository<OptionalFeature> optionalFeatureRepository)
        {
            this.workTrackingSystemFactory = workTrackingSystemFactory;
            this.repository = repository;
            this.workTrackingConnectorFactory = workTrackingConnectorFactory;
            this.cryptoService = cryptoService;

            var linearPreviewFeature = optionalFeatureRepository.GetByPredicate(f => f.Key == OptionalFeatureKeys.LinearIntegrationKey);
            isLinearIntegrationEnabled = linearPreviewFeature?.Enabled ?? false;
        }

        [HttpGet("supported")]
        public ActionResult<IEnumerable<WorkTrackingSystemConnectionDto>> GetSupportedWorkTrackingSystemConnections()
        {
            var supportedSystems = new List<WorkTrackingSystemConnectionDto>();

            foreach (WorkTrackingSystems system in Enum.GetValues<WorkTrackingSystems>())
            {
                if (!isLinearIntegrationEnabled && system == WorkTrackingSystems.Linear)
                {
                    continue;
                }

                AddConnectionForWorkTrackingSystem(supportedSystems, system);
            }

            return Ok(supportedSystems);
        }

        [HttpGet]
        public ActionResult<IEnumerable<WorkTrackingSystemConnectionDto>> GetWorkTrackingSystemConnections()
        {
            var existingConnections = repository.GetAll();

            var connectionDtos = existingConnections.Select(c => new WorkTrackingSystemConnectionDto(c));
            return Ok(connectionDtos);
        }

        [HttpPost]
        public async Task<ActionResult<WorkTrackingSystemConnectionDto>> CreateNewWorkTrackingSystemConnectionAsync([FromBody] WorkTrackingSystemConnectionDto newConnection)
        {
            newConnection.Id = 0;
            var connection = CreateConnectionFromDto(newConnection);

            repository.Add(connection);
            await repository.Save();

            var createdConnectionDto = new WorkTrackingSystemConnectionDto(connection);
            return Ok(createdConnectionDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<WorkTrackingSystemConnectionDto>> UpdateWorkTrackingSystemConnectionAsync(int id, [FromBody] WorkTrackingSystemConnectionDto updatedConnection)
        {
            return await this.GetEntityByIdAnExecuteAction(repository, id, async existingConnection =>
            {
                existingConnection.Name = updatedConnection.Name;

                foreach (var option in updatedConnection.Options)
                {
                    var existingOption = existingConnection.Options.Single(o => o.Key == option.Key);
                    existingOption.Value = option.Value;
                }

                UpdateAdditionalFieldDefinitions(existingConnection, updatedConnection.AdditionalFieldDefinitions);

                repository.Update(existingConnection);
                await repository.Save();

                return new WorkTrackingSystemConnectionDto(existingConnection);
            });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWorkTrackingSystemConnectionAsync(int id)
        {
            if (!repository.Exists(id))
            {
                return NotFound();
            }

            repository.Remove(id);
            await repository.Save();

            return Ok();
        }

        [HttpPost("validate")]
        public async Task<ActionResult<bool>> ValidateConnection(WorkTrackingSystemConnectionDto connectionDto)
        {
            // Services expect an encrypted value, so we have to manually do that here
            foreach (var option in connectionDto.Options.Where(o => o.IsSecret))
            {
                option.Value = cryptoService.Encrypt(option.Value);
            }

            var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(connectionDto.WorkTrackingSystem);
            var connection = CreateConnectionFromDto(connectionDto);

            var isConnectionValid = await workItemService.ValidateConnection(connection);

            return Ok(isConnectionValid);
        }


        private void AddConnectionForWorkTrackingSystem(List<WorkTrackingSystemConnectionDto> supportedSystems, WorkTrackingSystems system)
        {
            var defaultConnection = workTrackingSystemFactory.CreateDefaultConnectionForWorkTrackingSystem(system);

            supportedSystems.Add(new WorkTrackingSystemConnectionDto(defaultConnection));
        }

        private WorkTrackingSystemConnection CreateConnectionFromDto(WorkTrackingSystemConnectionDto connectionDto)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = connectionDto.Id,
                Name = connectionDto.Name,
                WorkTrackingSystem = connectionDto.WorkTrackingSystem,
                AuthenticationMethodKey = connectionDto.AuthenticationMethodKey,
            };

            foreach (var option in connectionDto.Options)
            {
                connection.Options.Add(
                    new WorkTrackingSystemConnectionOption
                    {
                        Key = option.Key,
                        Value = option.Value,
                        IsSecret = option.IsSecret,
                        IsOptional = option.IsOptional,
                    }
                    );
            }

            foreach (var fieldDto in connectionDto.AdditionalFieldDefinitions)
            {
                var additionalField = fieldDto.ToModel();
                
                connection.AdditionalFieldDefinitions.Add(additionalField);
            }

            return connection;
        }

        private static void UpdateAdditionalFieldDefinitions(
            WorkTrackingSystemConnection existingConnection,
            List<AdditionalFieldDefinitionDto> updatedFields)
        {
            var existingById = existingConnection.AdditionalFieldDefinitions.ToDictionary(f => f.Id);
            var updatedIds = new HashSet<int>(updatedFields.Where(f => f.Id != 0).Select(f => f.Id));

            var toRemove = existingConnection.AdditionalFieldDefinitions
                .Where(f => !updatedIds.Contains(f.Id))
                .ToList();
            foreach (var field in toRemove)
            {
                existingConnection.AdditionalFieldDefinitions.Remove(field);
            }

            foreach (var fieldDto in updatedFields)
            {
                if (fieldDto.Id != 0 && existingById.TryGetValue(fieldDto.Id, out var existingField))
                {
                    // Update existing field (preserve Id)
                    existingField.DisplayName = fieldDto.DisplayName;
                    existingField.Reference = fieldDto.Reference;
                }
                else
                {
                    // Add new field (Id will be auto-generated)
                    existingConnection.AdditionalFieldDefinitions.Add(fieldDto.ToModel());
                }
            }
        }
    }
}
