using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkTrackingSystemOptionsOwnerDtoBase
    {
        public WorkTrackingSystemOptionsOwnerDtoBase(WorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            Name = workTrackingSystemOptionsOwner.Name;
            Id = workTrackingSystemOptionsOwner.Id;
            LastUpdated = DateTime.SpecifyKind(workTrackingSystemOptionsOwner.UpdateTime, DateTimeKind.Utc);
            Tags = workTrackingSystemOptionsOwner.Tags.ToList();
            ServiceLevelExpectationProbability = workTrackingSystemOptionsOwner.ServiceLevelExpectationProbability;
            ServiceLevelExpectationRange = workTrackingSystemOptionsOwner.ServiceLevelExpectationRange;
        }

        public string Name { get; }

        public int Id { get; }

        public List<string> Tags { get; }

        public DateTime LastUpdated { get; }

        public int ServiceLevelExpectationProbability { get; }

        public int ServiceLevelExpectationRange { get; }

    }
}
