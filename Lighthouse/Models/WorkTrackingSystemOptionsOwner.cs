using Lighthouse.WorkTracking;

namespace Lighthouse.Models
{
    public class WorkTrackingSystemOptionsOwner<T> : IWorkItemQueryOwner where T : class
    {
        public int Id { get; set; }

        public string WorkItemQuery { get; set; } = string.Empty;

        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public List<WorkTrackingSystemOption<T>> WorkTrackingSystemOptions { get; set; } = new List<WorkTrackingSystemOption<T>>();

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
