using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkTrackingSystemOptionsOwnerDtoBase
    {
        public WorkTrackingSystemOptionsOwnerDtoBase()
        {
        }

        public WorkTrackingSystemOptionsOwnerDtoBase(WorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            Name = workTrackingSystemOptionsOwner.Name;
            Id = workTrackingSystemOptionsOwner.Id;
            LastUpdated = DateTime.SpecifyKind(workTrackingSystemOptionsOwner.UpdateTime, DateTimeKind.Utc);
            Tags = workTrackingSystemOptionsOwner.Tags.ToList();
            ServiceLevelExpectationProbability = workTrackingSystemOptionsOwner.ServiceLevelExpectationProbability;
            ServiceLevelExpectationRange = workTrackingSystemOptionsOwner.ServiceLevelExpectationRange;
        }

        public string Name { get; set; }

        [JsonRequired]
        public int Id { get; set; }

        public List<string> Tags { get; set; }

        [JsonRequired]
        public DateTime LastUpdated { get; set; }

        public int ServiceLevelExpectationProbability { get; }

        public int ServiceLevelExpectationRange { get; }

    }
}
