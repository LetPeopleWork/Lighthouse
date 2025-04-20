using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
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

        public WorkTrackingSystemConnectionsController(
            IWorkTrackingSystemFactory workTrackingSystemFactory, IRepository<WorkTrackingSystemConnection> repository, IWorkTrackingConnectorFactory workTrackingConnectorFactory, ICryptoService cryptoService)
        {
            this.workTrackingSystemFactory = workTrackingSystemFactory;
            this.repository = repository;
            this.workTrackingConnectorFactory = workTrackingConnectorFactory;
            this.cryptoService = cryptoService;
        }

        [HttpGet("supported")]
        public ActionResult<IEnumerable<WorkTrackingSystemConnectionDto>> GetSupportedWorkTrackingSystemConnections()
        {
            var supportedSystems = new List<WorkTrackingSystemConnectionDto>();

            foreach (WorkTrackingSystems system in Enum.GetValues<WorkTrackingSystems>())
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
            var connection = new WorkTrackingSystemConnection()
            {
                Id = connectionDto.Id,
                Name = connectionDto.Name,
                WorkTrackingSystem = connectionDto.WorkTrackingSystem,
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

            return connection;
        }
    }
}
