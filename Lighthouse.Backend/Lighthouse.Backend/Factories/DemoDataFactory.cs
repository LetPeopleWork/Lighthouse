using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Factories
{
    public class DemoDataFactory : IDemoDataFactory
    {
        private readonly IWorkTrackingSystemFactory workTrackingSystemFactory;

        public DemoDataFactory(IWorkTrackingSystemFactory workTrackingSystemFactory)
        {
            this.workTrackingSystemFactory = workTrackingSystemFactory;
        }

        public Project CreateDemoProject(string name)
        {
            var demoProject = new Project
            {
                Name = name,
                BlockedTags = new List<string> { "Blocked" },
                ToDoStates = new List<string> { "Backlog" },
                DoingStates = new List<string> { "Next", "Analysing", "Implementation", "Verification" },
                DoneStates = new List<string> { "Done" },
                WorkItemTypes = new List<string> { "Epic" },
                WorkItemQuery = ParseCsv(name)
            };

            return demoProject;
        }

        public Team CreateDemoTeam(string name)
        {
            var demoTeam = new Team
            {
                Name = name,
                AutomaticallyAdjustFeatureWIP = false,
                BlockedTags = new List<string> { "Blocked" },
                ToDoStates = new List<string> { "Backlog" },
                DoingStates = new List<string> { "Next", "Analysing", "Implementation", "Verification" },
                DoneStates = new List<string> { "Done" },
                WorkItemTypes = new List<string> { "User Story", "Bug" },
                WorkItemQuery = ParseCsv(name)
            };

            return demoTeam;
        }

        public WorkTrackingSystemConnection CreateDemoWorkTrackingSystemConnection()
        {
            var workTrackingSystemConnection = workTrackingSystemFactory.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);

            workTrackingSystemConnection.Name = "Demo Data CSV Connector";
            workTrackingSystemConnection.Id = 1886;

            workTrackingSystemConnection.Options.Single(x => x.Key == CsvWorkTrackingOptionNames.DateTimeFormat).Value = "yyyy-MM-dd";
            workTrackingSystemConnection.Options.Single(x => x.Key == CsvWorkTrackingOptionNames.TagSeparator).Value = "|";
            workTrackingSystemConnection.Options.Single(x => x.Key == CsvWorkTrackingOptionNames.ParentReferenceIdHeader).Value = "Parent";
            workTrackingSystemConnection.Options.Single(x => x.Key == CsvWorkTrackingOptionNames.StartedDateHeader).Value = "StartedDate";
            workTrackingSystemConnection.Options.Single(x => x.Key == CsvWorkTrackingOptionNames.ClosedDateHeader).Value = "ClosedDate";

            return workTrackingSystemConnection;
        }

        private string ParseCsv(string csvName)
        {
            var csvContent = File.ReadAllText($"Factories/DemoData/{csvName}.csv");

            return ReplaceDatePlaceholders(csvContent);
        }

        private string ReplaceDatePlaceholders(string csvContent)
        {
            var pattern = @"\{(-?\d+)\}";
            var today = DateTime.Today;

            return Regex.Replace(csvContent, pattern, match =>
            {
                int daysOffset = int.Parse(match.Groups[1].Value);

                var targetDate = today.AddDays(daysOffset);

                return targetDate.ToString("yyyy-MM-dd");
            }, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
    }

    public static class DemoTeamNames
    {
        public static string GoodThroughput => "Team Zenith";

        public static string ConstantlyIncreasingWip => "Team Voyager";

        public static string SpikedThroughput => "Team Pulsar";

        public static string OldItems => "Team Gravity";

        public static string PartTimeWork => "Team Lightspeed";
    }

    public static class DemoProjectNames
    {
        public static string EpicForecast => "Project Apollo";

        public static string LaunchAlignment => "Project Orion";
    }
}
