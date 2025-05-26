using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class EntityReferenceDto
    {
        public EntityReferenceDto()
        {
        }

        public EntityReferenceDto(Team team)
        {
            Id = team.Id;
            Name = team.Name;
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
