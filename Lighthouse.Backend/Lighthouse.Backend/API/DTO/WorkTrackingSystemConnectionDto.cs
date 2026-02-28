using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkTrackingSystemConnectionDto
    {
        public WorkTrackingSystemConnectionDto()
        {
        }


        public WorkTrackingSystemConnectionDto(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            Id = workTrackingSystemConnection.Id;
            Name = workTrackingSystemConnection.Name;
            WorkTrackingSystem = workTrackingSystemConnection.WorkTrackingSystem;
            AuthenticationMethodKey = workTrackingSystemConnection.AuthenticationMethodKey;
            AuthenticationMethodDisplayName = AuthenticationMethodSchema.GetDisplayName(
                workTrackingSystemConnection.WorkTrackingSystem,
                workTrackingSystemConnection.AuthenticationMethodKey);
            AvailableAuthenticationMethods = AuthenticationMethodSchema
                .GetMethodsForSystem(workTrackingSystemConnection.WorkTrackingSystem)
                .Select(AuthenticationMethodDto.FromSchema)
                .ToList();
            Options.AddRange(workTrackingSystemConnection.Options.Select(o => new WorkTrackingSystemConnectionOptionDto(o)));
            AdditionalFieldDefinitions.AddRange(
                workTrackingSystemConnection.AdditionalFieldDefinitions.Select(f => new AdditionalFieldDefinitionDto(f)));
            WriteBackMappingDefinitions.AddRange(
                workTrackingSystemConnection.WriteBackMappingDefinitions.Select(m => new WriteBackMappingDefinitionDto(m)));
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        [JsonRequired]
        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        [JsonRequired]
        public string AuthenticationMethodKey { get; set; } = string.Empty;

        public string AuthenticationMethodDisplayName { get; set; } = string.Empty;

        public List<AuthenticationMethodDto> AvailableAuthenticationMethods { get; set; } = [];

        public List<WorkTrackingSystemConnectionOptionDto> Options { get; set; } = [];

        public List<AdditionalFieldDefinitionDto> AdditionalFieldDefinitions { get; set; } = [];

        public List<WriteBackMappingDefinitionDto> WriteBackMappingDefinitions { get; set; } = [];
    }
}
