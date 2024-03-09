using Lighthouse.WorkTracking;

namespace Lighthouse.Models
{
    public interface IWorkTrackingSystemOptionsOwner
    {
        WorkTrackingSystems WorkTrackingSystem { get; set; }

        string GetWorkTrackingSystemOptionByKey(string key);
    }
}