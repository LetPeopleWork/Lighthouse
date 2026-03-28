using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class StateMappingDto
    {
        public StateMappingDto()
        {
        }

        public StateMappingDto(StateMapping stateMapping)
        {
            Name = stateMapping.Name;
            States = stateMapping.States;
        }

        public string Name { get; set; } = string.Empty;

        public List<string> States { get; set; } = [];
    }
}
