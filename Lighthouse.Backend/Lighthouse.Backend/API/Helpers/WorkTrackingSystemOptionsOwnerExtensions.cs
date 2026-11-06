using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using System.Text.Json;

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
            TakeParentSourcesAsSent(owner, settings);
            owner.TakeEstimationAsSent(settings.EstimationSource, settings.EstimationAsSent());
        }

        /// <summary>
        /// The list this owner would hold as its own once the save is taken, or why it cannot hold it; null when
        /// the save sends no list, follows, edits the old field instead, or sends no source and only echoes the
        /// list the owner already reads.
        /// </summary>
        public static ParentSourcesOutcome? ParentSourcesOwnedOnceTaken(this WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase settings)
            => settings.ParentSources is { } sent && !TheOldFieldIsTheEdit(owner, settings) && TheSaveChoosesToOwn(owner, settings.ParentOverrideSource, sent)
                ? ParentSources.Of(sent.Select(entry => entry?.ToEntry()!))
                : null;

        /// <summary>The old single field this save means; a save that sends a list of its own has already said everything.</summary>
        public static int? ParentOverrideFieldAsSent(this WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase settings)
            => settings.ParentSources is null || TheOldFieldIsTheEdit(owner, settings) ? settings.ParentOverrideAdditionalFieldDefinitionId : null;

        // A client that knows only the old field may still send back everything it read, the list included.
        // The list is then an echo, and the old field it changed is what the save is about.
        private static bool TheOldFieldIsTheEdit(WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase settings)
        {
            var read = owner.TheEffectiveParentSources();

            return settings.ParentOverrideFieldWasSent()
                && settings.ParentOverrideAdditionalFieldDefinitionId != read.ToTheLegacyField()
                && TextOf(settings.ParentSources) == TextOf(ParentSourceEntryDto.ListOf(read));
        }

        private static bool TheSaveChoosesToOwn(WorkTrackingSystemOptionsOwner owner, SettingSource? sentSource, List<ParentSourceEntryDto?> sent)
            => IncomingSetting.SourceChosen(sentSource, TextOf(sent), sentIsSet: true, TextOf(ParentSourceEntryDto.ListOf(owner.TheEffectiveParentSources()))) == SettingSource.Own;

        private static string TextOf(List<ParentSourceEntryDto?>? list) => JsonSerializer.Serialize(list);

        private static void TakeParentSourcesAsSent(WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase settings)
        {
            if (owner.ParentSourcesOwnedOnceTaken(settings)?.List is { } list)
            {
                owner.OwnParentSources(list);
                return;
            }

            owner.TakeParentOverrideAsSent(settings.ParentOverrideSource, owner.ParentOverrideFieldAsSent(settings));
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
