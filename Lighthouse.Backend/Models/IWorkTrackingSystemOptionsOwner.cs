using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Models
{
    public interface IWorkTrackingSystemOptionsOwner
    {
        WorkTrackingSystems WorkTrackingSystem { get; set; }

        string GetWorkTrackingSystemOptionByKey(string key);
    }
}