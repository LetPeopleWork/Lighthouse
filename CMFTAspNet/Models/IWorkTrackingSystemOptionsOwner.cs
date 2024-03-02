using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Models
{
    public interface IWorkTrackingSystemOptionsOwner
    {
        WorkTrackingSystems WorkTrackingSystem { get; set; }

        string GetWorkTrackingSystemOptionByKey(string key);
    }
}