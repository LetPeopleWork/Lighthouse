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

        /// <summary>The settings every Team and Portfolio can follow a connection default for, taken from a save as sent.</summary>
        // Taken before the save moves the owner to another connection, so what the owner read before the request
        // is judged against the connection it was read from.
        public static void TakeFollowableSettingsAsSent(this WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase settings)
        {
            owner.TakeParentOverrideAsSent(settings.ParentOverrideSource, settings.ParentOverrideAdditionalFieldDefinitionId);
            owner.TakeEstimationAsSent(settings.EstimationSource, settings.EstimationAsSent());
        }

        /// <summary>A property nobody registered purges anyway: when the answer is unknown, take the expensive one.</summary>
        private static bool TheEditChanges(WorkTrackingSystemOptionsOwner queryOwner, string property, SettingsOwnerDtoBase settings) => property switch
        {
            nameof(WorkTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId)
                => queryOwner.WorkTrackingSystemConnectionId != settings.WorkTrackingSystemConnectionId,
            _ => true,
        };
    }
}
