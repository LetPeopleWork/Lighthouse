using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.API.Helpers
{
    /// <summary>
    /// The settings screen gives a field it has not saved yet a negative placeholder id, and a write-back
    /// mapping pointing at that field carries the same placeholder. The database knows nothing of it, so
    /// the mapping is linked to the new field object instead and picks up the real id when both are saved.
    /// </summary>
    public sealed class NewAdditionalFieldTargets
    {
        private readonly Dictionary<int, AdditionalFieldDefinition> fieldsByPlaceholderId = [];

        public AdditionalFieldDefinition CreateField(AdditionalFieldDefinitionDto fieldDto)
        {
            var placeholderId = fieldDto.Id;
            var field = fieldDto.ToModel();

            if (placeholderId < 0)
            {
                fieldsByPlaceholderId[placeholderId] = field;
            }

            return field;
        }

        public WriteBackMappingDefinition CreateMapping(WriteBackMappingDefinitionDto mappingDto)
        {
            var mapping = mappingDto.ToModel();
            AssignField(mapping, mappingDto.AdditionalFieldDefinitionId);
            return mapping;
        }

        public void AssignField(WriteBackMappingDefinition mapping, int? fieldId)
        {
            if (fieldId is < 0 && fieldsByPlaceholderId.TryGetValue(fieldId.Value, out var newField))
            {
                mapping.AdditionalFieldDefinition = newField;

                // A mapping that is new itself still holds the placeholder. An existing mapping keeps its stored
                // id, because clearing it as well would leave the database layer unsure which change to apply.
                if (mapping.AdditionalFieldDefinitionId < 0)
                {
                    mapping.AdditionalFieldDefinitionId = null;
                }

                return;
            }

            // An unmatched placeholder is kept so the validator can reject it before anything is saved.
            mapping.AdditionalFieldDefinitionId = fieldId;
        }
    }
}
