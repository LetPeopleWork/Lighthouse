using System.Globalization;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    public class DemoDataFactoryTest
    {
        private static readonly TimeZoneInfo Zurich = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        private static readonly DateTimeOffset LateEveningInUtc = new(2026, 3, 10, 23, 30, 0, TimeSpan.Zero);

        private static readonly string[] TheCommunicationsEpic = ["AP-002"];

        private static readonly string[] NavigationAndMining = ["AP-004", "AP-006"];

        private static readonly string[] TheWhaleStudy = ["OE-010"];

        private static readonly string[] TheEcosystemDatabase = ["OE-009"];

        private static readonly string[] TheMineralSurvey = ["OE-008"];

        private static readonly string[] TheMappingInitiative = ["OE-001"];

        [Test]
        public void CreateDemoWorkTrackingSystemConnection_CreatesWorkTrackingSystemConnectionWithCorrectDetails()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = subject.CreateDemoWorkTrackingSystemConnection();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workTrackingSystemConnection.Name, Is.EqualTo("Demo Data CSV Connector"));
                Assert.That(workTrackingSystemConnection.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Csv));

                var options = workTrackingSystemConnection.Options;
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.Delimiter, options), Is.EqualTo(","));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.DateTimeFormat, options), Is.EqualTo("yyyy-MM-dd"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.TagSeparator, options), Is.EqualTo("|"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.IdHeader, options), Is.EqualTo("ID"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.NameHeader, options), Is.EqualTo("Name"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.StateHeader, options), Is.EqualTo("State"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.TypeHeader, options), Is.EqualTo("Type"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.ParentReferenceIdHeader, options), Is.EqualTo("Parent"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.StartedDateHeader, options), Is.EqualTo("StartedDate"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.ClosedDateHeader, options), Is.EqualTo("ClosedDate"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.TagsHeader, options), Is.EqualTo("Tags"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.StateEnteredDateHeader, options), Is.EqualTo("StateEnteredDate"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.DependsOnHeader, options), Is.EqualTo("DependsOn"));
            }
        }

        [Test]
        [TestCase("Team Equinox")]
        [TestCase("Team Gravity")]
        [TestCase("Team Lightspeed")]
        [TestCase("Team Meridian")]
        [TestCase("Team Pulsar")]
        [TestCase("Team Voyager")]
        [TestCase("Team Zenith")]
        public void CreateDemoTeam_CreatesTeamWithCorrectSettings(string teamName)
        {
            var subject = CreateSubject();

            var demoTeam = subject.CreateDemoTeam(teamName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(demoTeam.Name, Is.EqualTo(teamName));

                Assert.That(demoTeam.ToDoStates, Has.Count.EqualTo(1));
                Assert.That(demoTeam.ToDoStates, Contains.Item("Backlog"));

                Assert.That(demoTeam.DoingStates, Has.Count.EqualTo(5));
                Assert.That(demoTeam.DoingStates, Contains.Item("Next"));
                Assert.That(demoTeam.DoingStates, Contains.Item("Analysing"));
                Assert.That(demoTeam.DoingStates, Contains.Item("Implementation"));
                Assert.That(demoTeam.DoingStates, Contains.Item("Waiting for Verification"));
                Assert.That(demoTeam.DoingStates, Contains.Item("Verification"));

                Assert.That(demoTeam.DoneStates, Has.Count.EqualTo(1));
                Assert.That(demoTeam.DoneStates, Contains.Item("Done"));

                Assert.That(demoTeam.WorkItemTypes, Has.Count.EqualTo(2));
                Assert.That(demoTeam.WorkItemTypes, Contains.Item("User Story"));
                Assert.That(demoTeam.WorkItemTypes, Contains.Item("Bug"));

                Assert.That(demoTeam.BlockedRuleSetJson, Does.Contain("workitem.tags"));
                Assert.That(demoTeam.BlockedRuleSetJson, Does.Contain("Blocked"));
                Assert.That(demoTeam.BlockedStalenessThresholdDays, Is.EqualTo(5));

                Assert.That(demoTeam.DataRetrievalValue, Is.Not.Empty);
                Assert.That(demoTeam.DataRetrievalValue, Does.Not.Contain("{"));
                Assert.That(demoTeam.DataRetrievalValue, Does.Not.Contain("}"));
            }
        }

        [Test]
        [TestCase("Project Apollo")]
        [TestCase("Project NeuroLink City")]
        [TestCase("Project Ocean Explorer")]
        [TestCase("Project Orion")]
        public void CreateDemoProject_CreatesProjectWithCorrectSettings(string projectName)
        {
            var subject = CreateSubject();

            var demoProject = subject.CreateDemoProject(projectName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(demoProject.Name, Is.EqualTo(projectName));

                Assert.That(demoProject.ToDoStates, Has.Count.EqualTo(1));
                Assert.That(demoProject.ToDoStates, Contains.Item("Backlog"));

                Assert.That(demoProject.DoingStates, Has.Count.EqualTo(5));
                Assert.That(demoProject.DoingStates, Contains.Item("Next"));
                Assert.That(demoProject.DoingStates, Contains.Item("Analysing"));
                Assert.That(demoProject.DoingStates, Contains.Item("Implementation"));
                Assert.That(demoProject.DoingStates, Contains.Item("Waiting for Verification"));
                Assert.That(demoProject.DoingStates, Contains.Item("Verification"));

                Assert.That(demoProject.DoneStates, Has.Count.EqualTo(1));
                Assert.That(demoProject.DoneStates, Contains.Item("Done"));

                Assert.That(demoProject.WorkItemTypes, Has.Count.EqualTo(1));
                Assert.That(demoProject.WorkItemTypes, Contains.Item("Epic"));

                Assert.That(demoProject.BlockedRuleSetJson, Does.Contain("feature.tags"));
                Assert.That(demoProject.BlockedRuleSetJson, Does.Contain("Blocked"));
                Assert.That(demoProject.BlockedStalenessThresholdDays, Is.EqualTo(5));

                Assert.That(demoProject.DataRetrievalValue, Is.Not.Empty);
                Assert.That(demoProject.DataRetrievalValue, Does.Not.Contain("{"));
                Assert.That(demoProject.DataRetrievalValue, Does.Not.Contain("}"));
            }
        }

        [Test]
        public async Task CreateDemoProject_EpicForecast_WaitsOnWhatTheScenarioIsMeantToShow()
        {
            var dependencies = await DependenciesOfDemoPortfolio(DemoProjectNames.EpicForecast);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependencies["AP-004"], Is.EqualTo(TheCommunicationsEpic));
                Assert.That(dependencies["AP-005"], Is.EqualTo(NavigationAndMining));
                Assert.That(dependencies["AP-006"], Is.Empty);
            }
        }

        /// <summary>
        /// The scenario named after dependencies is the one that has to carry the awkward ones, so this
        /// pins the three a reader is meant to meet there: a circle, a blocker ranked below the Feature
        /// waiting on it, and one shared with a team that has closed nothing.
        /// </summary>
        [Test]
        public async Task CreateDemoProject_ProjectWithDependencies_CarriesOneOfEachAwkwardKind()
        {
            var dependencies = await DependenciesOfDemoPortfolio(DemoProjectNames.ProjectWithDependencies);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependencies["OE-009"], Is.EqualTo(TheWhaleStudy));
                Assert.That(dependencies["OE-010"], Is.EqualTo(TheEcosystemDatabase));
                Assert.That(dependencies["OE-005"], Is.EqualTo(TheMineralSurvey));
                Assert.That(dependencies["OE-004"], Is.EqualTo(TheMappingInitiative));
            }
        }

        private static async Task<Dictionary<string, string[]>> DependenciesOfDemoPortfolio(string portfolioName)
        {
            var subject = CreateSubject();

            var portfolio = subject.CreateDemoProject(portfolioName);
            portfolio.WorkTrackingSystemConnection = subject.CreateDemoWorkTrackingSystemConnection();

            var connector = new CsvWorkTrackingConnector(Mock.Of<ILogger<CsvWorkTrackingConnector>>());
            var features = await connector.GetFeaturesForProject(portfolio);

            return features.ToDictionary(
                feature => feature.ReferenceId,
                feature => feature.DependsOnReferences.Select(reference => reference.ReferenceId).ToArray());
        }

        [Test]
        public void CreateDemoTeam_GoodThroughputTeam_NeverCompletesWorkOnWeekends()
        {
            var subject = CreateSubject();

            var demoTeam = subject.CreateDemoTeam(DemoTeamNames.GoodThroughput);

            var completedDates = ParseColumnDates(demoTeam.DataRetrievalValue, columnIndex: 6);
            Assert.That(completedDates, Is.Not.Empty);
            Assert.That(completedDates, Has.None.Matches<DateTime>(IsWeekend));
        }

        /// <summary>
        /// Bug #5567: 23:30 UTC is already the next day in Zurich, so the UTC day and the instance
        /// day disagree - and every migrated read path reports the instance day.
        /// </summary>
        [Test]
        [TestCase("Team Zenith")]
        [TestCase("Team Gravity")]
        public void CreateDemoTeam_PastInstanceMidnightButBeforeUtcMidnight_AnchorsRelativeDatesOnTheInstanceDay(string teamName)
        {
            var clock = new FakeLighthouseClock(LateEveningInUtc, Zurich);

            var demoTeam = CreateSubject(clock).CreateDemoTeam(teamName);

            Assert.That(LatestDate(demoTeam.DataRetrievalValue), Is.EqualTo(clock.Today));
        }

        [Test]
        public void CreateDemoProject_PastInstanceMidnightButBeforeUtcMidnight_AnchorsRelativeDatesOnTheInstanceDay()
        {
            var clock = new FakeLighthouseClock(LateEveningInUtc, Zurich);

            var demoProject = CreateSubject(clock).CreateDemoProject(DemoProjectNames.NewProductInitiative);

            Assert.That(LatestDate(demoProject.DataRetrievalValue), Is.EqualTo(clock.Today));
        }

        [Test]
        public void CreateDemoTeam_InstanceZoneAheadOfUtc_AnchorsADayLaterThanAUtcInstance()
        {
            var zurichTeam = CreateSubject(new FakeLighthouseClock(LateEveningInUtc, Zurich)).CreateDemoTeam(DemoTeamNames.GoodThroughput);
            var utcTeam = CreateSubject(new FakeLighthouseClock(LateEveningInUtc)).CreateDemoTeam(DemoTeamNames.GoodThroughput);

            Assert.That(LatestDate(zurichTeam.DataRetrievalValue), Is.EqualTo(LatestDate(utcTeam.DataRetrievalValue).AddDays(1)));
        }

        private static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        }

        private static DateOnly LatestDate(string csvContent)
        {
            var dates = csvContent
                .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(TryParseDate)
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .ToList();

            Assert.That(dates, Is.Not.Empty);

            return dates.Max();
        }

        private static DateOnly? TryParseDate(string cell)
        {
            return DateOnly.TryParseExact(cell, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;
        }

        private static List<DateTime> ParseColumnDates(string csvContent, int columnIndex)
        {
            return csvContent
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Skip(1)
                .Select(line => line.Split(','))
                .Where(cells => cells.Length > columnIndex && !string.IsNullOrWhiteSpace(cells[columnIndex]))
                .Select(cells => DateTime.ParseExact(cells[columnIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToList();
        }

        private string GetWorkTrackingSystemOptionValue(string optionName, IEnumerable<WorkTrackingSystemConnectionOption> options)
        {
            return options.Single(o => o.Key == optionName).Value;
        }

        private static DemoDataFactory CreateSubject(ILighthouseClock? clock = null)
        {
            return new DemoDataFactory(
                new WorkTrackingSystemFactory(Mock.Of<ILogger<WorkTrackingSystemFactory>>()),
                clock ?? new FakeLighthouseClock(DateTimeOffset.UtcNow));
        }
    }
}
