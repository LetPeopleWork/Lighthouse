using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class WorkTrackingSystemConnection : IConcurrencyTokenEntity
    {
        public int Id { get; set; }

        public Guid ConcurrencyToken { get; set; }

        public string Name { get; set; }

        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public string AuthenticationMethodKey { get; set; } = string.Empty;

        public List<WorkTrackingSystemConnectionOption> Options { get; } = [];

        public List<AdditionalFieldDefinition> AdditionalFieldDefinitions { get; } = [];

        public List<WriteBackMappingDefinition> WriteBackMappingDefinitions { get; } = [];

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
            var workTrackingOption = Options.SingleOrDefault(x => x.Key == key);
            if (workTrackingOption == null)
            {
                return null;
            }

            return T.TryParse(workTrackingOption.Value, null, out var result) ? result : null;
        }

        /// <summary>
        /// The same lookup for callers that have something to say about a missing option. The throwing
        /// version treats absence as a programming error, which it is on a path that has already been
        /// through the connection screen — but a connection read back from the database has whatever
        /// options its row happens to carry, and a validator asked about one of those owes the
        /// administrator the name of the field rather than a stack trace.
        /// </summary>
        public string? FindWorkTrackingSystemConnectionOptionByKey(string key)
            => Options.SingleOrDefault(option => option.Key == key)?.Value;
    }
}
