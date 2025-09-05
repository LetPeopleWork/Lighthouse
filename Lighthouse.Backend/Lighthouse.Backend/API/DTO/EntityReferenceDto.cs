using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class EntityReferenceDto
    {
        public EntityReferenceDto()
        {
        }

        public EntityReferenceDto(int id, string name)
        {
            Id = id;
            Name = name;
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
