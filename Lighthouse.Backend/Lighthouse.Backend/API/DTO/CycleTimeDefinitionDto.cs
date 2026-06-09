using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class CycleTimeDefinitionDto
    {
        public CycleTimeDefinitionDto()
        {
        }

        public CycleTimeDefinitionDto(CycleTimeDefinition definition, bool isValid)
        {
            Id = definition.Id;
            Name = definition.Name;
            StartState = definition.StartState;
            EndState = definition.EndState;
            IsValid = isValid;
        }

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string StartState { get; set; } = string.Empty;

        public string EndState { get; set; } = string.Empty;

        public bool IsValid { get; set; }
    }
}
