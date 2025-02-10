namespace Lighthouse.Backend.Models
{
    public interface IWorkTrackingSystemOptionsOwner
    {
        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }
    }
}