using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public enum DataSourceType
    {
        Query,
        File
    }

    public class WorkTrackingSystemConnection : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public WorkTrackingSystems WorkTrackingSystem { get; set; }


        public List<WorkTrackingSystemConnectionOption> Options { get; } = [];

        public DataSourceType DataSourceType { get; set; } = DataSourceType.Query;

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
