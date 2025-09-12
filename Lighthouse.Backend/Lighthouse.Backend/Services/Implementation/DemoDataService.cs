using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class DemoDataService : IDemoDataService
    {
        private static readonly List<DemoDataScenario> scenarios = new List<DemoDataScenario>();

        public DemoDataService()
        {
            scenarios.Clear();
            scenarios.AddRange(GetFreeScenarios());
        }

        public IEnumerable<DemoDataScenario> GetAllScenarios()
        {
            return scenarios;
        }

        public Task LoadScenarios(params DemoDataScenario[] scenarios)
        {
            return Task.CompletedTask;
        }

        private List<DemoDataScenario> GetFreeScenarios()
        {
            var freeScenarios = new List<DemoDataScenario>();

            var whenWillItBeDone = CreatesScenario(0, "When Will This Be Done?", "One Team, a set of Epics, and the question: When can we get it?");
            whenWillItBeDone.Teams.Add(DemoTeamNames.GoodThroughput);
            whenWillItBeDone.Projects.Add(DemoProjectNames.EpicForecast);
            freeScenarios.Add(whenWillItBeDone);

            var overloadedTeams = CreatesScenario(1, "Crash Override", "We're super busy, but somehow everything is slow...");
            overloadedTeams.Teams.Add(DemoTeamNames.ConstantlyIncreasingWip);
            overloadedTeams.Projects.Add(DemoProjectNames.OverloadedWip);
            freeScenarios.Add(overloadedTeams);

            var waterMelon = CreatesScenario(2, "Watermelon", "Show the flaw over averages in forecasting");
            waterMelon.Teams.Add(DemoTeamNames.GoodThroughput);
            waterMelon.Projects.Add(DemoProjectNames.HiddenRisk);
            freeScenarios.Add(waterMelon);

            var productLaunch = CreatesScenario(3, "Product Launch", "Shows how we can deal with fixed dates and use the power of forecasting to show what's possible");
            productLaunch.Teams.Add(DemoTeamNames.GoodThroughput);
            productLaunch.Teams.Add(DemoTeamNames.ImprovedOverTime);
            productLaunch.Projects.Add(DemoProjectNames.LaunchAlignment);
            freeScenarios.Add(productLaunch);

            return freeScenarios;
        }

        private DemoDataScenario CreatesScenario(int id, string title, string description)
        {
            return new DemoDataScenario
            {
                Id = id,
                Title = title,
                Description = description,
                IsPremium = false,
            };
        }

        private static class DemoTeamNames
        {
            public static string GoodThroughput => "Team Zenith";

            public static string ConstantlyIncreasingWip => "Team Voyager";

            public static string SpikedThroughput => "Team Pulsar";

            public static string CleanupHappened => "Team Comet";

            public static string OldItems => "Team Gravity";

            public static string MoreClosedThanStarted => "Team Eclipse";

            public static string ImprovedOverTime => "Team Ascent";
        }

        private static class DemoProjectNames
        {
            public static string EpicForecast => "Project Apollo";

            public static string OverloadedWip => "Project Saturn";

            public static string HiddenRisk => "Project Orion";

            public static string LaunchAlignment => "Project Aurora";
        }
    }
}
