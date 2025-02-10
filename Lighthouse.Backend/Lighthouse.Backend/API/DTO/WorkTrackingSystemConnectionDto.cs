using Lighthouse.Backend.Models;
using Lighthouse.Backend.WorkTracking;
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

            Options.AddRange(workTrackingSystemConnection.Options.Select(o => new WorkTrackingSystemConnectionOptionDto(o)));
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; }

        [JsonRequired]
        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public List<WorkTrackingSystemConnectionOptionDto> Options { get; set; } = [];
    }
}
