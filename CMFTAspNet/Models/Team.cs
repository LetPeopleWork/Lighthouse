using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Models
{
    public class Team : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ProjectName { get; set; }

        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public List<WorkTrackingSystemOption> WorkTrackingSystemOptions { get; set; } = new List<WorkTrackingSystemOption>();

        public List<string> AreaPaths { get; set; } = new List<string>();

        public List<string> WorkItemTypes { get; set; } = new List<string> { "User Story", "Bug" };

        public List<string> IgnoredTags { get; set; } = new List<string>();

        public List<string> AdditionalRelatedFields { get; set; } = new List<string>();

        public int FeatureWIP { get; set; } = 1;

        public int[] RawThroughput { get; set; } = [1];

        public int ThroughputHistory { get; set; } = 30;

        public Throughput Throughput => new Throughput(RawThroughput);

        public string GetWorkTrackingSystemOptionByKey(string key)
        {
            var workTrackingOption = WorkTrackingSystemOptions.SingleOrDefault(x => x.Key == key);

            if (workTrackingOption == null)
            {
                throw new ArgumentException($"Key {key} not found in Work Tracking Options");
            }

            return workTrackingOption.Value;
        }
    }
}
