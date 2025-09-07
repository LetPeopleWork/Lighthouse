using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.DemoData
{
    public class DemoDataScenario : IEntity
    {
        public int Id { get; set; }

        public string Title {  get; set; }

        public string Description { get; set; }

        public int NumberOfTeams {  get; set; }

        public int NumberOfProjects { get; set; }

        public bool IsPremium {  get; set; }
    }
}
