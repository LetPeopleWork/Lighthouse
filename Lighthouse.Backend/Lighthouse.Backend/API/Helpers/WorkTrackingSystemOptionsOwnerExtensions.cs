using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;

namespace Lighthouse.Backend.API.Helpers
{
    public static class WorkTrackingSystemOptionsOwnerExtensions
    {
        /// <summary>
        /// Whether this edit means the entity has to throw away what it already stored and start from
        /// nothing. Driven by <see cref="FetchFingerprint.PropertiesThatAlsoCostAFreshStart"/>, and asked
        /// of the base a team and a portfolio share: it is one question, so it may only have one answer -
        /// two copies of it would be free to disagree about the same property.
        /// </summary>
        public static bool WorkItemRelatedSettingsChanged(this WorkTrackingSystemOptionsOwner queryOwner, SettingsOwnerDtoBase settings)
            => FetchFingerprint.PropertiesThatAlsoCostAFreshStart.Any(property => TheEditChanges(queryOwner, property, settings));

        /// <summary>A property nobody registered purges anyway: when the answer is unknown, take the expensive one.</summary>
        private static bool TheEditChanges(WorkTrackingSystemOptionsOwner queryOwner, string property, SettingsOwnerDtoBase settings) => property switch
        {
            nameof(WorkTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId)
                => queryOwner.WorkTrackingSystemConnectionId != settings.WorkTrackingSystemConnectionId,
            _ => true,
        };
    }
}
