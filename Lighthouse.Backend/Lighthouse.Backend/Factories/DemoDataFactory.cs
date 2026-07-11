﻿using Lighthouse.Backend.Models;
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

        public Portfolio CreateDemoProject(string name)
        {
            var demoProject = new Portfolio
            {
                Name = name,
                BlockedTags = new List<string> { "Blocked" },
                BlockedStalenessThresholdDays = 5,
                ToDoStates = new List<string> { "Backlog" },
                DoingStates = new List<string> { "Next", "Analysing", "Implementation", "Waiting for Verification", "Verification" },
                DoneStates = new List<string> { "Done" },
                WorkItemTypes = new List<string> { "Epic" },
                CycleTimeDefinitions = CreateDemoCycleTimeDefinitions(),
                DataRetrievalValue = ParseCsv(name)
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
                BlockedStalenessThresholdDays = 5,
                ToDoStates = new List<string> { "Backlog" },
                DoingStates = new List<string> { "Next", "Analysing", "Implementation", "Waiting for Verification", "Verification" },
                DoneStates = new List<string> { "Done" },
                WorkItemTypes = new List<string> { "User Story", "Bug" },
                CycleTimeDefinitions = CreateDemoCycleTimeDefinitions(),
                DataRetrievalValue = ParseCsv(name)
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
            workTrackingSystemConnection.Options.Single(x => x.Key == CsvWorkTrackingOptionNames.StateEnteredDateHeader).Value = "StateEnteredDate";
            workTrackingSystemConnection.Options.Single(x => x.Key == CsvWorkTrackingOptionNames.SynthesizeStateJourneyForDemo).Value = bool.TrueString;

            return workTrackingSystemConnection;
        }

        private static List<CycleTimeDefinition> CreateDemoCycleTimeDefinitions()
        {
            return new List<CycleTimeDefinition>
            {
                new CycleTimeDefinition
                {
                    Id = 1,
                    Name = "Lead Time (End to End)",
                    StartState = "Backlog",
                    EndState = "Done",
                },
                new CycleTimeDefinition
                {
                    Id = 2,
                    Name = "Analysis to Done",
                    StartState = "Analysing",
                    EndState = "Done",
                },
            };
        }

        private static string ParseCsv(string csvName)
        {
            var csvContent = File.ReadAllText($"Factories/DemoData/{csvName}.csv");

            return ReplaceDatePlaceholders(csvContent);
        }

        private static string ReplaceDatePlaceholders(string csvContent)
        {
            var pattern = @"\{(w?)(-?\d+)\}";
            var today = DateTime.UtcNow.Date;

            return Regex.Replace(csvContent, pattern, match =>
            {
                int daysOffset = int.Parse(match.Groups[2].Value);
                var isBusinessDayOffset = match.Groups[1].Value == "w";

                var targetDate = isBusinessDayOffset
                    ? BusinessDaysBefore(today, Math.Abs(daysOffset))
                    : today.AddDays(daysOffset);

                return targetDate.ToString("yyyy-MM-dd");
            }, RegexOptions.None, TimeSpan.FromSeconds(1));
        }

        private static DateTime BusinessDaysBefore(DateTime today, int businessDays)
        {
            var date = today;

            while (IsWeekend(date))
            {
                date = date.AddDays(-1);
            }

            for (var remaining = businessDays; remaining > 0; remaining--)
            {
                do
                {
                    date = date.AddDays(-1);
                }
                while (IsWeekend(date));
            }

            return date;
        }

        private static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
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

        public static string ProjectWithDependencies => "Project Ocean Explorer";

        public static string QuarterlyPlanning => "Project NeuroLink City";

        public static string NewProductInitiative => "Project Altobelli";
    }
}
