using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class AppSetting : IEntity
    {
        public int Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }
    }
}
