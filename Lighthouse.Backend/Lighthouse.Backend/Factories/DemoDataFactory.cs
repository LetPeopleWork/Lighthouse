using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Factories
{
    public class DemoDataFactory : IDemoDataFactory
    {
        public Project CreateDemoProject(string name)
        {
            throw new NotImplementedException();
        }

        public Team CreateDemoTeam(string name)
        {
            throw new NotImplementedException();
        }

        public WorkTrackingSystemConnection CreateDemoWorkTrackingSystemConnection()
        {
            throw new NotImplementedException();
        }
    }

    public static class DemoTeamNames
    {
        public static string GoodThroughput => "Team Zenith";

        public static string ConstantlyIncreasingWip => "Team Voyager";

        public static string SpikedThroughput => "Team Pulsar";

        public static string CleanupHappened => "Team Comet";

        public static string OldItems => "Team Gravity";

        public static string MoreClosedThanStarted => "Team Eclipse";

        public static string ImprovedOverTime => "Team Ascent";
    }

    public static class DemoProjectNames
    {
        public static string EpicForecast => "Project Apollo";

        public static string OverloadedWip => "Project Saturn";

        public static string HiddenRisk => "Project Orion";

        public static string LaunchAlignment => "Project Aurora";
    }
}
