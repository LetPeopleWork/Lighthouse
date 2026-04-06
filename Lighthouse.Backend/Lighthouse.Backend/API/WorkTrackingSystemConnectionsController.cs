using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Models.Validation;
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
        private readonly ILicenseService licenseService;

        public WorkTrackingSystemConnectionsController(
            IWorkTrackingSystemFactory workTrackingSystemFactory, IRepository<WorkTrackingSystemConnection> repository, IWorkTrackingConnectorFactory workTrackingConnectorFactory, ICryptoService cryptoService, ILicenseService licenseService)
        {
            this.workTrackingSystemFactory = workTrackingSystemFactory;
            this.repository = repository;
            this.workTrackingConnectorFactory = workTrackingConnectorFactory;
            this.cryptoService = cryptoService;
            this.licenseService = licenseService;
        }

        [HttpGet("supported")]
        public ActionResult<IEnumerable<WorkTrackingSystemConnectionDto>> GetSupportedWorkTrackingSystemConnections()
        {
            var supportedSystems = new List<WorkTrackingSystemConnectionDto>();

            foreach (var system in Enum.GetValues<WorkTrackingSystems>())
            {
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

            var mappingValidation = WriteBackMappingValidator.Validate(connection.WriteBackMappingDefinitions);
            if (!mappingValidation.IsValid)
            {
                return BadRequest(mappingValidation.Errors);
            }
            
            if (!connection.AdditionalFieldDefinitions.SupportsAdditionalFields(licenseService))
            {
                return StatusCode(StatusCodes.Status403Forbidden, null);
            }

            if (!connection.WriteBackMappingDefinitions.SupportsWriteBackMappings(licenseService))
            {
                return StatusCode(StatusCodes.Status403Forbidden, null);
            }

            repository.Add(connection);
            await repository.Save();

            var createdConnectionDto = new WorkTrackingSystemConnectionDto(connection);
            return Ok(createdConnectionDto);
        }

        [HttpPost("validate")]
        public async Task<ActionResult<object>> ValidateConnection(WorkTrackingSystemConnectionDto connectionDto)
        {
            PatchWorkTrackingSystemConnectionSecretsIfNeeded(connectionDto);
            EncryptSecretValuesIfNeeded(connectionDto);

            var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(connectionDto.WorkTrackingSystem);
            var connection = CreateConnectionFromDto(connectionDto);

            if (!connection.AdditionalFieldDefinitions.SupportsAdditionalFields(licenseService))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ConnectionValidationResult.Failure(
                    "license_limit_exceeded",
                    "You've exceeded the number of additional fields allowed on your plan."));
            }

            if (!connection.WriteBackMappingDefinitions.SupportsWriteBackMappings(licenseService))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ConnectionValidationResult.Failure(
                    "license_limit_exceeded",
                    "Write-back mappings are not available on your current plan."));
            }

            var validationResult = await workItemService.ValidateConnection(connection);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult);
            }

            return Ok(validationResult);
        }

        private void EncryptSecretValuesIfNeeded(WorkTrackingSystemConnectionDto connectionDto)
        {
            // Only encrypt if not already encrypted (new values from user input)
            if (connectionDto.Id != 0)
            {
                return;
            }
            
            foreach (var option in connectionDto.Options.Where(o => o.IsSecret && !string.IsNullOrEmpty(o.Value)))
            {
                option.Value = cryptoService.Encrypt(option.Value);
            }
        }

        private void PatchWorkTrackingSystemConnectionSecretsIfNeeded(WorkTrackingSystemConnectionDto connectionDto)
        {
            if (connectionDto.Id == 0)
            {
                return;
            }

            var existingConnection = repository.GetById(connectionDto.Id);
            if (existingConnection == null)
            {
                return;
            }

            foreach (var option in connectionDto.Options.Where(o => o.IsSecret && string.IsNullOrEmpty(o.Value)))
            {
                var existingOption = existingConnection.Options.SingleOrDefault(o => o.Key == option.Key);
                if (existingOption != null)
                {
                    option.Value = existingOption.Value;
                }
            }
        }


        private void AddConnectionForWorkTrackingSystem(List<WorkTrackingSystemConnectionDto> supportedSystems, WorkTrackingSystems system)
        {
            var defaultConnection = workTrackingSystemFactory.CreateDefaultConnectionForWorkTrackingSystem(system);

            supportedSystems.Add(new WorkTrackingSystemConnectionDto(defaultConnection));
        }

        private static WorkTrackingSystemConnection CreateConnectionFromDto(WorkTrackingSystemConnectionDto connectionDto)
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

            foreach (var mappingDto in connectionDto.WriteBackMappingDefinitions)
            {
                var mapping = mappingDto.ToModel();

                connection.WriteBackMappingDefinitions.Add(mapping);
            }

            return connection;
        }
    }
}
