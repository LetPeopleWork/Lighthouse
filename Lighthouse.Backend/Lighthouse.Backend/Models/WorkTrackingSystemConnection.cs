using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class WorkTrackingSystemConnection : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public string AuthenticationMethodKey { get; set; } = string.Empty;

        public List<WorkTrackingSystemConnectionOption> Options { get; } = [];

        public List<AdditionalFieldDefinition> AdditionalFieldDefinitions { get; } = [];

        public string GetWorkTrackingSystemConnectionOptionByKey(string key)
        {
            var workTrackingOption = Options.SingleOrDefault(x => x.Key == key);

            if (workTrackingOption == null)
            {
                throw new ArgumentException($"Key {key} not found in Work Tracking Options");
            }

            return workTrackingOption.Value;
        }

        public T? GetWorkTrackingSystemConnectionOptionByKey<T>(string key) where T : struct, IParsable<T>
        {
            var value = GetWorkTrackingSystemConnectionOptionByKey(key);
            return T.TryParse(value, null, out var result) ? result : null;
        }
    }
}
