using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/worktrackingsystemconnections/{workTrackingSystemConnectionId:int}")]
    [ApiController]
    public class WorkTrackingSystemConnectionController(IRepository<WorkTrackingSystemConnection> repository, ILicenseService licenseService)
        : ControllerBase
    {
        [HttpPut]
        public async Task<ActionResult<WorkTrackingSystemConnectionDto>> UpdateWorkTrackingSystemConnectionAsync(int workTrackingSystemConnectionId, [FromBody] WorkTrackingSystemConnectionDto updatedConnection)
        {
            var isForbiddenAction = false;
            
            var result = await this.GetEntityByIdAnExecuteAction(repository, workTrackingSystemConnectionId, async existingConnection =>
            {
                existingConnection.Name = updatedConnection.Name;

                foreach (var option in updatedConnection.Options)
                {
                    var existingOption = existingConnection.Options.Single(o => o.Key == option.Key);
                    
                    // Preserve existing secret values when empty string is provided
                    if (existingOption.IsSecret && string.IsNullOrEmpty(option.Value))
                    {
                        continue; // Skip update - keep existing encrypted value
                    }
                    
                    existingOption.Value = option.Value;
                }

                UpdateAdditionalFieldDefinitions(existingConnection, updatedConnection.AdditionalFieldDefinitions);

                if (existingConnection.AdditionalFieldDefinitions.SupportsAdditionalFields(licenseService))
                {
                    repository.Update(existingConnection);
                    await repository.Save();
                }
                else 
                {
                    isForbiddenAction = true;
                }
                
                return new WorkTrackingSystemConnectionDto(existingConnection);
            });

            return isForbiddenAction ? StatusCode(StatusCodes.Status403Forbidden, null) : result;
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteWorkTrackingSystemConnectionAsync(int workTrackingSystemConnectionId)
        {
            if (!repository.Exists(workTrackingSystemConnectionId))
            {
                return NotFound();
            }

            repository.Remove(workTrackingSystemConnectionId);
            await repository.Save();

            return Ok();
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
