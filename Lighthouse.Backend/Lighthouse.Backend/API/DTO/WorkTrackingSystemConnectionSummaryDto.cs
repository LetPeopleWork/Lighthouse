using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.API.DTO
{
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
        }

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public WorkTrackingSystems WorkTrackingSystem { get; set; }
    }
}
