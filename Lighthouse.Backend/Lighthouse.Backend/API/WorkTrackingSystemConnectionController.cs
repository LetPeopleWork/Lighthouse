using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/worktrackingsystemconnections/{workTrackingSystemConnectionId:int}")]
    [Route("api/latest/worktrackingsystemconnections/{workTrackingSystemConnectionId:int}")]
    [ApiController]
    // S6960 sees the save use none of the services that reading and deleting use, because the save's own
    // service now reads the connection for it. It is still one job: reading, saving and deleting one
    // connection, behind one route. Splitting it would put that route in two controllers.
#pragma warning disable S6960
    public class WorkTrackingSystemConnectionController(
        IRepository<WorkTrackingSystemConnection> repository,
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        ILicenseService licenseService,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IConnectionSaveService connectionSaveService)
        : ControllerBase
#pragma warning restore S6960
    {
        [HttpGet]
        public async Task<ActionResult<WorkTrackingSystemConnectionDto>> GetWorkTrackingSystemConnection(int workTrackingSystemConnectionId)
        {
            return await this.GetEntityByIdAnExecuteAction(repository, workTrackingSystemConnectionId, async connection =>
            {
                await EnsurePredefinedAdditionalFieldsRegistered(connection);
                return new WorkTrackingSystemConnectionDto(connection);
            });
        }

        private async Task EnsurePredefinedAdditionalFieldsRegistered(WorkTrackingSystemConnection connection)
        {
            var connector = workTrackingConnectorFactory.GetWorkTrackingConnector(connection.WorkTrackingSystem);

            // Get-or-create keyed on (IsPredefined, DisplayName) — mirrors the connector sync-path
            // (JiraWorkTrackingConnector.EnsurePredefinedAdditionalFieldsRegistered) so the two writers use
            // ONE dedup key. Matching on the resolved Reference instead would be add-only: a drifted Reference
            // (default resolved before the flagged field key was known, real one after) would append a SECOND
            // predefined row rather than reconcile it. Update the Reference in place; never duplicate.
            var changed = false;
            foreach (var predefined in connector.GetPredefinedAdditionalFields(connection))
            {
                var existing = connection.AdditionalFieldDefinitions
                    .FirstOrDefault(field => field.IsPredefined && field.DisplayName == predefined.DisplayName);

                if (existing is null)
                {
                    connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
                    {
                        DisplayName = predefined.DisplayName,
                        Reference = predefined.Reference,
                        IsPredefined = true,
                    });
                    changed = true;
                }
                else if (existing.Reference != predefined.Reference)
                {
                    existing.Reference = predefined.Reference;
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            repository.Update(connection);
            await repository.Save();
        }

        [HttpPut]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<ActionResult<WorkTrackingSystemConnectionDto>> UpdateWorkTrackingSystemConnectionAsync(
            int workTrackingSystemConnectionId,
            [FromBody] WorkTrackingSystemConnectionDto updatedConnection,
            [FromQuery] bool startUpdates = true)
        {
            var save = await connectionSaveService.Open(workTrackingSystemConnectionId, CancellationToken.None);
            if (save is null)
            {
                return NotFound();
            }

            var existingConnection = save.Connection;
            existingConnection.Name = updatedConnection.Name;
            existingConnection.AuthenticationMethodKey = updatedConnection.AuthenticationMethodKey;

            MergeOptions(existingConnection, updatedConnection.Options);
            var newFields = new NewAdditionalFieldTargets();
            UpdateAdditionalFieldDefinitions(existingConnection, updatedConnection.AdditionalFieldDefinitions, newFields);
            UpdateWriteBackMappingDefinitions(existingConnection, updatedConnection.WriteBackMappingDefinitions, newFields);

            if (updatedConnection.Defaults is { } defaults)
            {
                existingConnection.ParentOverrideForTeams = defaults.ParentOverrideForTeams;
            }

            var mappingValidation = WriteBackMappingValidator.Validate(existingConnection.WriteBackMappingDefinitions);
            if (!mappingValidation.IsValid)
            {
                return BadRequest(mappingValidation.Errors);
            }

            if (!HasLicenseFor(existingConnection))
            {
                return StatusCode(StatusCodes.Status403Forbidden, null);
            }

            await connectionSaveService.Commit(save, updatedConnection.ConcurrencyToken, startUpdates, CancellationToken.None);

            return Ok(new WorkTrackingSystemConnectionDto(existingConnection));
        }

        [HttpDelete]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<ActionResult> DeleteWorkTrackingSystemConnectionAsync(int workTrackingSystemConnectionId)
        {
            if (!repository.Exists(workTrackingSystemConnectionId))
            {
                return NotFound();
            }

            var isReferencedByTeam = teamRepository.Exists(t => t.WorkTrackingSystemConnectionId == workTrackingSystemConnectionId);
            var isReferencedByPortfolio = portfolioRepository.Exists(p => p.WorkTrackingSystemConnectionId == workTrackingSystemConnectionId);

            if (isReferencedByTeam || isReferencedByPortfolio)
            {
                return Conflict("Cannot delete this connection because it is currently used by one or more teams or portfolios.");
            }

            repository.Remove(workTrackingSystemConnectionId);
            await repository.Save();

            return Ok();
        }

        private void MergeOptions(
            WorkTrackingSystemConnection existingConnection,
            IEnumerable<WorkTrackingSystemConnectionOptionDto> incomingOptions)
        {
            var incomingKeys = incomingOptions.Select(o => o.Key).ToHashSet();

            // Only remove non-secret options that are no longer needed (e.g. switching auth method).
            // Secret options are never removed here; they are preserved when the incoming value is empty.
            var toRemove = existingConnection.Options.Where(o => !incomingKeys.Contains(o.Key) && !o.IsSecret).ToList();
            foreach (var removed in toRemove)
            {
                existingConnection.Options.Remove(removed);
            }

            foreach (var option in incomingOptions)
            {
                var existingOption = existingConnection.Options.SingleOrDefault(o => o.Key == option.Key);

                if (existingOption == null)
                {
                    existingConnection.Options.Add(new WorkTrackingSystemConnectionOption
                    {
                        Key = option.Key,
                        Value = option.Value,
                        IsSecret = option.IsSecret,
                        IsOptional = option.IsOptional,
                    });
                    continue;
                }

                if (existingOption.IsSecret && string.IsNullOrEmpty(option.Value))
                {
                    continue;
                }

                existingOption.Value = option.Value;
            }
        }

        private bool HasLicenseFor(WorkTrackingSystemConnection connection)
            => connection.AdditionalFieldDefinitions.SupportsAdditionalFields(licenseService)
                && connection.WriteBackMappingDefinitions.SupportsWriteBackMappings(licenseService);

        private static void UpdateWriteBackMappingDefinitions(
            WorkTrackingSystemConnection existingConnection,
            List<WriteBackMappingDefinitionDto> updatedMappings,
            NewAdditionalFieldTargets newFields)
        {
            // Predefined (system-owned) fields are inbound-only: they must never be persisted as a
            // write-back mapping target. Drop any incoming mapping that targets a predefined field id.
            var predefinedFieldIds = existingConnection.AdditionalFieldDefinitions
                .Where(f => f.IsPredefined)
                .Select(f => f.Id)
                .ToHashSet();
            updatedMappings = updatedMappings
                .Where(m => !m.AdditionalFieldDefinitionId.HasValue
                    || !predefinedFieldIds.Contains(m.AdditionalFieldDefinitionId.Value))
                .ToList();

            var existingById = existingConnection.WriteBackMappingDefinitions.ToDictionary(m => m.Id);
            var updateIds = new HashSet<int>(updatedMappings.Where(m => m.Id != 0).Select(m => m.Id));

            var toRemove = existingConnection.WriteBackMappingDefinitions
                .Where(m => !updateIds.Contains(m.Id))
                .ToList();
            foreach (var mapping in toRemove)
            {
                existingConnection.WriteBackMappingDefinitions.Remove(mapping);
            }

            foreach (var mappingDto in updatedMappings)
            {
                if (mappingDto.Id != 0 && existingById.TryGetValue(mappingDto.Id, out var existingMapping))
                {
                    existingMapping.ValueSource = mappingDto.ValueSource;
                    existingMapping.AppliesTo = mappingDto.AppliesTo;
                    newFields.AssignField(existingMapping, mappingDto.AdditionalFieldDefinitionId);
                    existingMapping.TargetValueType = mappingDto.TargetValueType;
                    existingMapping.DateFormat = mappingDto.DateFormat;
                }
                else
                {
                    existingConnection.WriteBackMappingDefinitions.Add(newFields.CreateMapping(mappingDto));
                }
            }
        }

        private static void UpdateAdditionalFieldDefinitions(
            WorkTrackingSystemConnection existingConnection,
            List<AdditionalFieldDefinitionDto> updatedFields,
            NewAdditionalFieldTargets newFields)
        {
            var existingById = existingConnection.AdditionalFieldDefinitions.ToDictionary(f => f.Id);
            var updatedIds = new HashSet<int>(updatedFields.Where(f => f.Id != 0).Select(f => f.Id));

            // Predefined (system-owned) fields are auto-registered and inbound-only: they are never candidates
            // for removal and are never mutated by the incoming user payload (a settings save that omits them
            // must preserve them, and their DisplayName/Reference are owned by auto-registration).
            var toRemove = existingConnection.AdditionalFieldDefinitions
                .Where(f => !f.IsPredefined && !updatedIds.Contains(f.Id))
                .ToList();
            foreach (var field in toRemove)
            {
                existingConnection.AdditionalFieldDefinitions.Remove(field);
            }

            foreach (var fieldDto in updatedFields)
            {
                if (fieldDto.Id != 0 && existingById.TryGetValue(fieldDto.Id, out var existingField))
                {
                    if (existingField.IsPredefined)
                    {
                        continue;
                    }

                    // Update existing field (preserve Id)
                    existingField.DisplayName = fieldDto.DisplayName;
                    existingField.Reference = fieldDto.Reference;
                }
                else
                {
                    existingConnection.AdditionalFieldDefinitions.Add(newFields.CreateField(fieldDto));
                }
            }
        }
    }
}
