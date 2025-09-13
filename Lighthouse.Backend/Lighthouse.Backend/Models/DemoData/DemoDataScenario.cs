using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.DemoData
{
    public class DemoDataScenario : IEntity
    {
        public int Id { get; set; }

        public string Title {  get; set; }

        public string Description { get; set; }

        public List<string> Teams { get; } = new List<string>();

        public List<string> Projects { get; } = new List<string>();

        public bool IsPremium {  get; set; }
    }
}
