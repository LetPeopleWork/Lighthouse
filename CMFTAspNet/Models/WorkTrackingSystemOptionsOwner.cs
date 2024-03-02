using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Models
{
    public class WorkTrackingSystemOptionsOwner<T> : IWorkTrackingSystemOptionsOwner where T : class
    {
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
