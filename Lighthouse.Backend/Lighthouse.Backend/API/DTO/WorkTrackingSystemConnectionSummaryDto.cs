using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// What a Team or Portfolio Admin may read of a connection: the non-secret fields the settings forms
    /// need. Anything added here reaches every scoped admin, so options and usage stay on the full list.
    /// </summary>
    public class WorkTrackingSystemConnectionSummaryDto
    {
        public WorkTrackingSystemConnectionSummaryDto()
        {
        }

        public WorkTrackingSystemConnectionSummaryDto(WorkTrackingSystemConnection connection)
        {
            Id = connection.Id;
            Name = connection.Name;
            WorkTrackingSystem = connection.WorkTrackingSystem;
            AdditionalFieldDefinitions.AddRange(connection.AdditionalFieldDefinitions.Select(f => new AdditionalFieldDefinitionDto(f)));
            Defaults = new ConnectionDefaultsDto(connection);
            SupportedDefaults = [.. SupportedConnectionDefaults.For(connection.WorkTrackingSystem).Select(WorkTrackingSystemConnectionDto.NameOf)];
        }

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public List<AdditionalFieldDefinitionDto> AdditionalFieldDefinitions { get; set; } = [];

        public ConnectionDefaultsDto? Defaults { get; set; }

        public List<string> SupportedDefaults { get; set; } = [];
    }
}
