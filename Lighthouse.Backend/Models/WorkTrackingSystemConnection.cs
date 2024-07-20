using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Models
{
    public class WorkTrackingSystemConnection : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public List<WorkTrackingSystemConnectionOption> Options { get; } = [];

        public string GetWorkTrackingSystemConnectionOptionByKey(string key)
        {
            var workTrackingOption = Options.SingleOrDefault(x => x.Key == key);

            if (workTrackingOption == null)
            {
                throw new ArgumentException($"Key {key} not found in Work Tracking Options");
            }

            return workTrackingOption.Value;
        }
    }
}
