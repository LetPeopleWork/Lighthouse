using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Factories
{
    public interface IDemoDataFactory
    {
        WorkTrackingSystemConnection CreateDemoWorkTrackingSystemConnection();

        Team CreateDemoTeam(string name);

        Project CreateDemoProject(string name);
    }
}
