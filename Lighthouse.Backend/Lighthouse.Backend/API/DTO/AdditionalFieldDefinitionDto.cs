using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class AdditionalFieldDefinitionDto
    {
        public AdditionalFieldDefinitionDto()
        {
        }

        public AdditionalFieldDefinitionDto(AdditionalFieldDefinition field)
        {
            Id = field.Id;
            DisplayName = field.DisplayName;
            Reference = field.Reference;
        }

        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public AdditionalFieldDefinition ToModel()
        {
            if (Id < 0)
            {
                Id = 0;
            }
            
            return new AdditionalFieldDefinition
            {
                Id = Id,
                DisplayName = DisplayName,
                Reference = Reference
            };
        }
    }
}
